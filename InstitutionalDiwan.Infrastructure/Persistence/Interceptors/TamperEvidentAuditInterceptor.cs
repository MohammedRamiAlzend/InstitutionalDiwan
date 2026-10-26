using InstitutionalDiwan.Application.Abstractions.Context;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Entities.Relations;
using InstitutionalDiwan.Domain.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;

namespace InstitutionalDiwan.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Appends an immutable, SHA-256 hash-chained audit entry for every request-scoped write. Domain
/// events are the primary source (they carry the business meaning); a generic old/new snapshot is
/// used for aggregate types that do not raise events. Password hashes, tokens, secrets, and lookup
/// tables are never written.
///
/// Writes made outside an HTTP request (startup seeding, migrations) are skipped: they are not user
/// actions and would otherwise flood the chain before any account exists.
/// </summary>
internal sealed class TamperEvidentAuditInterceptor : SaveChangesInterceptor
{
    private const string DepartmentClaimType = "dept_id";

    /// <summary>Name of the SQL Server application lock that serializes audit-chain appends.</summary>
    private const string ChainLockResource = "InstitutionalDiwan.AuditChain";

    private const int ChainLockTimeoutMilliseconds = 60_000;

    private static readonly string[] SensitiveFragments =
    {
        "password", "secret", "token", "salt", "securitystamp", "concurrencystamp"
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IClock _clock;

    /// <summary>
    /// Transactions this interceptor opened itself so it could hold the chain lock. They are
    /// committed (or rolled back) when the surrounding save completes.
    /// </summary>
    private readonly ConditionalWeakTable<DbContext, ChainSaveState> _chainStates = new();

    public TamperEvidentAuditInterceptor(IHttpContextAccessor httpContextAccessor, IClock clock)
    {
        _httpContextAccessor = httpContextAccessor;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
            ApplyAsync(context, CancellationToken.None).GetAwaiter().GetResult();

        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
            await ApplyAsync(context, cancellationToken);

        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is { } context)
            CompleteChainTransactionAsync(context, CancellationToken.None).GetAwaiter().GetResult();

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
            await CompleteChainTransactionAsync(context, cancellationToken);

        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is { } context)
            AbandonChainTransactionAsync(context).GetAwaiter().GetResult();
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
            await AbandonChainTransactionAsync(context);
    }

    private async Task ApplyAsync(DbContext context, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var entries = context.ChangeTracker.Entries().ToList();
        var changedEntries = entries
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var pendingAuditEntries = context.ChangeTracker
            .Entries<AuditLog>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToList();

        if (changedEntries.Count == 0 && pendingAuditEntries.Count == 0)
        {
            ClearDomainEvents(context);
            return;
        }

        var actorDepartmentId = httpContext is null
            ? (Guid?)null
            : CurrentDepartmentId(httpContext.User);
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var correlationId = httpContext is null ? null : ResolveCorrelationId(httpContext);
        var candidates = new List<PendingAuditEntry>();

        // Without an HTTP request there is no actor, department, IP, or correlation id to
        // attribute, so startup seeding and ordinary background writes are not audited. Explicit
        // rows appended by AuditLogAppender are still chained and serialized below.
        if (httpContext is not null)
        {
            var actorUserId = CurrentUserId(httpContext.User);
            var handledEntities = new HashSet<object>(ReferenceEqualityComparer.Instance);

            // 1) Business events are the richest source of audit detail.
            foreach (var entry in entries)
            {
                if (ReadDomainEvents(entry.Entity) is not { Count: > 0 } events)
                    continue;

                handledEntities.Add(entry.Entity);

                foreach (var domainEvent in events)
                {
                    var candidate = MapDomainEvent(domainEvent);
                    if (candidate is null)
                        continue;

                    candidates.Add(new PendingAuditEntry(
                        candidate,
                        actorUserId ?? candidate.ActorUserId,
                        domainEvent.OccurredOnUtc));
                }
            }

            // 2) Aggregate types that do not publish events still get a generic before/after entry.
            foreach (var entry in changedEntries)
            {
                if (handledEntities.Contains(entry.Entity))
                    continue;

                var candidate = MapEntityChange(entry);
                if (candidate is null)
                    continue;

                candidates.Add(new PendingAuditEntry(candidate, actorUserId, _clock.UtcNow));
            }
        }

        if (candidates.Count == 0 && pendingAuditEntries.Count == 0)
        {
            ClearDomainEvents(context);
            return;
        }

        try
        {
            // The chain has one writer position. The database lock makes concurrent appends (two
            // requests, an explicit self-audit plus a business write, or a scheduled job) extend it
            // one at a time instead of racing for the same PreviousHash.
            await AcquireChainLockAsync(context, cancellationToken);

            var previousHash = await ReadLatestHashAsync(context, cancellationToken);

            // Rows appended by AuditLogAppender computed their hash before the lock was taken, so
            // re-seal them onto the predecessor that is current now.
            foreach (var pendingAuditEntry in pendingAuditEntries)
            {
                pendingAuditEntry.Reseal(previousHash);
                previousHash = pendingAuditEntry.EventHash;
            }

            foreach (var candidate in candidates)
            {
                previousHash = Append(
                    context,
                    candidate.Candidate,
                    candidate.ActorUserId,
                    actorDepartmentId,
                    previousHash,
                    ipAddress,
                    correlationId,
                    candidate.OccurredAtUtc);
            }
        }
        catch
        {
            await AbandonChainTransactionAsync(context);
            throw;
        }

        ClearDomainEvents(context);
    }

    private static string Append(
        DbContext context,
        AuditCandidate candidate,
        Guid? actorUserId,
        Guid? actorDepartmentId,
        string previousHash,
        string? ipAddress,
        string? correlationId,
        DateTime occurredAtUtc)
    {
        var result = AuditLog.Record(
            candidate.ModuleName,
            candidate.ActionTypeId,
            candidate.EntityTypeId,
            candidate.EntityId.ToString("D", CultureInfo.InvariantCulture),
            previousHash,
            actorUserId,
            actorDepartmentId,
            candidate.RelatedDocumentId,
            candidate.OldValuesJson,
            candidate.NewValuesJson,
            ipAddress,
            correlationId,
            occurredAtUtc);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Could not append an audit entry for {candidate.ModuleName}/{candidate.EntityId}: " +
                $"{result.Error.Code} - {result.Error.Description}");
        }

        context.Set<AuditLog>().Add(result.Value);
        return result.Value.EventHash;
    }

    /// <summary>
    /// Serializes appends on SQL Server with a transaction-scoped application lock. The lock is
    /// released automatically when the transaction commits or rolls back. Non-relational and
    /// single-writer providers (SQLite in tests) rely on the unique <c>PreviousHash</c> index alone.
    /// </summary>
    private async Task AcquireChainLockAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
            return;

        if (context.Database.CurrentTransaction is null)
        {
            var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            _chainStates.AddOrUpdate(context, new ChainSaveState(transaction));
        }

        var dbTransaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "The audit chain lock requires an active database transaction.");

        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "sp_getapplock";
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = dbTransaction.GetDbTransaction();

        AddParameter(command, "@Resource", DbType.String, ChainLockResource, 255);
        AddParameter(command, "@LockMode", DbType.String, "Exclusive", 32);
        AddParameter(command, "@LockOwner", DbType.String, "Transaction", 32);
        AddParameter(command, "@LockTimeout", DbType.Int32, ChainLockTimeoutMilliseconds);
        AddParameter(command, "@DbPrincipal", DbType.String, "public", 128);

        // sp_getapplock reports success through its RETURN value, not through an output parameter.
        var lockResult = command.CreateParameter();
        lockResult.ParameterName = "@ReturnValue";
        lockResult.DbType = DbType.Int32;
        lockResult.Direction = ParameterDirection.ReturnValue;
        command.Parameters.Add(lockResult);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var result = lockResult.Value is null or DBNull ? -999 : Convert.ToInt32(lockResult.Value);
        if (result < 0)
        {
            throw new InvalidOperationException(
                $"Could not acquire the audit chain lock (sp_getapplock result {result}).");
        }
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        DbType type,
        object value,
        int? size = null)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;

        if (size is { } length)
            parameter.Size = length;

        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static async Task<string> ReadLatestHashAsync(
        DbContext context,
        CancellationToken cancellationToken)
    {
        if (context.Database.IsSqlServer())
        {
            // This database runs READ_COMMITTED_SNAPSHOT, so an ordinary SELECT inside a command
            // transaction can still see the snapshot taken before the chain lock was acquired. An
            // UPDLOCK/HOLDLOCK read bypasses the snapshot and sees the row the lock holder committed.
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText =
                "SELECT TOP (1) [EventHash] FROM [AUDIT_LOGS] WITH (UPDLOCK, HOLDLOCK) " +
                "ORDER BY [AuditLogId] DESC";

            var latestHash = await command.ExecuteScalarAsync(cancellationToken) as string;
            return latestHash ?? AuditLog.GenesisHash;
        }

        var latest = await context.Set<AuditLog>()
            .AsNoTracking()
            .OrderByDescending(log => log.AuditLogId)
            .Select(log => log.EventHash)
            .FirstOrDefaultAsync(cancellationToken);

        return latest ?? AuditLog.GenesisHash;
    }

    private async Task CompleteChainTransactionAsync(
        DbContext context,
        CancellationToken cancellationToken)
    {
        if (!_chainStates.TryGetValue(context, out var state))
            return;

        _chainStates.Remove(context);

        if (state.OwnedTransaction is not { } transaction)
            return;

        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    private async Task AbandonChainTransactionAsync(DbContext context)
    {
        if (!_chainStates.TryGetValue(context, out var state))
            return;

        _chainStates.Remove(context);

        if (state.OwnedTransaction is not { } transaction)
            return;

        try
        {
            await transaction.RollbackAsync();
        }
        catch
        {
            // The original failure is what matters; the transaction is disposed either way.
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    private static AuditCandidate? MapDomainEvent(IDomainEvent domainEvent) => domainEvent switch
    {
        DepartmentCreatedEvent e => new(
            "ORGANIZATION", AuditActionType.CreateId, AuditEntityType.DepartmentId, e.DepartmentId,
            NewJson(new { e.Code, e.NameAr, e.ParentDepartmentId })),
        DepartmentMovedEvent e => new(
            "ORGANIZATION", AuditActionType.UpdateId, AuditEntityType.DepartmentId, e.DepartmentId,
            NewJson(new { ParentDepartmentId = e.NewParentId }),
            OldJson(new { ParentDepartmentId = e.OldParentId })),
        DepartmentDeactivatedEvent e => new(
            "ORGANIZATION", AuditActionType.DeleteId, AuditEntityType.DepartmentId, e.DepartmentId),

        UserCreatedEvent e => new(
            "IDENTITY", AuditActionType.CreateId, AuditEntityType.UserId, e.UserId,
            NewJson(new { e.Username, e.PrimaryDepartmentId })),
        UserLoggedInEvent e => new(
            "IDENTITY", AuditActionType.LoginId, AuditEntityType.UserId, e.UserId,
            NewJson(new { e.Username }), ActorUserId: e.UserId),
        UserDeactivatedEvent e => new(
            "IDENTITY", AuditActionType.DeleteId, AuditEntityType.UserId, e.UserId),
        UserRoleAssignedEvent e => new(
            "IDENTITY", AuditActionType.UpdateId, AuditEntityType.UserId, e.UserId,
            NewJson(new { e.RoleId }), ActorUserId: e.AssignedByUserId),
        UserRoleRevokedEvent e => new(
            "IDENTITY", AuditActionType.UpdateId, AuditEntityType.UserId, e.UserId,
            OldValuesJson: OldJson(new { e.RoleId })),
        DocumentAccessGrantedEvent e => new(
            "IDENTITY", AuditActionType.CreateId, AuditEntityType.DocumentId, e.DocumentId,
            NewJson(new { e.GrantId, e.UserId, e.AccessLevelId }), RelatedDocumentId: e.DocumentId),
        DocumentAccessRevokedEvent e => new(
            "IDENTITY", AuditActionType.DeleteId, AuditEntityType.DocumentId, e.DocumentId,
            OldValuesJson: OldJson(new { e.GrantId, e.UserId }), RelatedDocumentId: e.DocumentId),

        DocumentCreatedEvent e => new(
            "CORRESPONDENCE", AuditActionType.CreateId, AuditEntityType.DocumentId, e.DocumentId,
            NewJson(new { e.DocumentTypeId, e.DirectionId, e.OwningDepartmentId }),
            RelatedDocumentId: e.DocumentId, ActorUserId: e.CreatedByUserId),
        DocumentRegisteredEvent e => new(
            "CORRESPONDENCE", AuditActionType.UpdateId, AuditEntityType.DocumentId, e.DocumentId,
            NewJson(new { e.OfficialNumberId, e.OfficialNumberText, e.RegistrationDate }),
            RelatedDocumentId: e.DocumentId),
        DocumentArchivedEvent e => new(
            "ARCHIVING", AuditActionType.UpdateId, AuditEntityType.DocumentId, e.DocumentId,
            NewJson(new { e.Reason }),
            OldValuesJson: OldJson(new { StatusId = e.PreviousStatusId }),
            RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        DocumentUnarchivedEvent e => new(
            "ARCHIVING", AuditActionType.UpdateId, AuditEntityType.DocumentId, e.DocumentId,
            NewJson(new { e.Reason }),
            OldValuesJson: OldJson(new { StatusId = e.RestoredStatusId }),
            RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        DocumentCanceledEvent e => new(
            "CORRESPONDENCE", AuditActionType.UpdateId, AuditEntityType.DocumentId, e.DocumentId,
            NewJson(new { e.Reason }),
            OldValuesJson: OldJson(new { StatusId = e.PreviousStatusId }),
            RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        DocumentParticipantAddedEvent e => new(
            "CORRESPONDENCE", AuditActionType.UpdateId, AuditEntityType.DocumentId, e.DocumentId,
            NewJson(new { e.ParticipantId, e.RoleId }), RelatedDocumentId: e.DocumentId),

        OfficialNumberIssuedEvent e => new(
            "NUMBERING", AuditActionType.CreateId, AuditEntityType.DocumentNumberId, e.DocumentNumberId,
            NewJson(new { e.DocumentId, e.RegistryId, e.OfficialNumberText, e.NumberingYear, e.SequenceValue }),
            RelatedDocumentId: e.DocumentId),
        OfficialNumberCancelledEvent e => new(
            "NUMBERING", AuditActionType.DeleteId, AuditEntityType.DocumentNumberId, e.DocumentNumberId,
            NewJson(new { e.Reason }),
            OldValuesJson: OldJson(new { e.OfficialNumberText }),
            RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        OfficialNumberReplacedEvent e => new(
            "NUMBERING", AuditActionType.UpdateId, AuditEntityType.DocumentNumberId, e.OldDocumentNumberId,
            NewJson(new { e.NewDocumentNumberId, e.Reason }),
            RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),

        MovementCreatedEvent e => new(
            "MOVEMENTS", AuditActionType.CreateId, AuditEntityType.MovementId, e.MovementId,
            NewJson(new { e.SourceDepartmentId, e.DestinationDepartmentId }),
            RelatedDocumentId: e.DocumentId),
        MovementSentEvent e => new(
            "MOVEMENTS", AuditActionType.UpdateId, AuditEntityType.MovementId, e.MovementId,
            NewJson(new { e.StepNo }), RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        MovementReceivedEvent e => new(
            "MOVEMENTS", AuditActionType.UpdateId, AuditEntityType.MovementId, e.MovementId,
            NewJson(new { e.StepNo }), RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        MovementCompletedEvent e => new(
            "MOVEMENTS", AuditActionType.ApproveId, AuditEntityType.MovementId, e.MovementId,
            NewJson(new { e.StepNo }), RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        MovementRejectedEvent e => new(
            "MOVEMENTS", AuditActionType.UpdateId, AuditEntityType.MovementId, e.MovementId,
            NewJson(new { e.StepNo, e.Reason }), RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        MovementReturnedEvent e => new(
            "MOVEMENTS", AuditActionType.UpdateId, AuditEntityType.MovementId, e.MovementId,
            NewJson(new { e.StepNo, e.Reason }), RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        MovementRedirectedEvent e => new(
            "MOVEMENTS", AuditActionType.UpdateId, AuditEntityType.MovementId, e.MovementId,
            NewJson(new { e.NewStepNo, e.NewDestinationDepartmentId }),
            RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        MovementClosedEvent e => new(
            "MOVEMENTS", AuditActionType.UpdateId, AuditEntityType.MovementId, e.MovementId,
            RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),
        MovementCancelledEvent e => new(
            "MOVEMENTS", AuditActionType.DeleteId, AuditEntityType.MovementId, e.MovementId,
            NewJson(new { e.Reason }), RelatedDocumentId: e.DocumentId, ActorUserId: e.ActorUserId),

        DocumentFileCreatedEvent e => new(
            "ATTACHMENTS", AuditActionType.CreateId, AuditEntityType.FileId, e.FileId,
            NewJson(new { e.FileTypeId, e.Title }), RelatedDocumentId: e.DocumentId),
        DocumentFileVersionUploadedEvent e => new(
            "ATTACHMENTS", AuditActionType.UpdateId, AuditEntityType.FileId, e.FileId,
            NewJson(new { e.VersionId, e.VersionNumber, e.ChecksumSha256 }),
            RelatedDocumentId: e.DocumentId),
        DocumentFileArchivedEvent e => new(
            "ATTACHMENTS", AuditActionType.DeleteId, AuditEntityType.FileId, e.FileId,
            RelatedDocumentId: e.DocumentId),

        DocumentRelationCreatedEvent e => new(
            "RELATIONS", AuditActionType.CreateId, AuditEntityType.DocumentRelationId, e.RelationId,
            NewJson(new { e.SourceDocumentId, e.TargetDocumentId, e.RelationTypeId }),
            RelatedDocumentId: e.SourceDocumentId, ActorUserId: e.ActorUserId),
        DocumentRelationRemovedEvent e => new(
            "RELATIONS", AuditActionType.DeleteId, AuditEntityType.DocumentRelationId, e.RelationId,
            OldValuesJson: OldJson(new { e.SourceDocumentId, e.TargetDocumentId, e.RelationTypeId }),
            RelatedDocumentId: e.SourceDocumentId, ActorUserId: e.ActorUserId),

        _ => null
    };

    private static AuditCandidate? MapEntityChange(EntityEntry entry)
    {
        if (!TryMapEntityType(entry.Entity.GetType(), out var module, out var entityTypeId))
            return null;

        var entityId = ResolveEntityId(entry);
        if (entityId is null)
            return null;

        var actionTypeId = entry.State switch
        {
            EntityState.Added => AuditActionType.CreateId,
            EntityState.Modified => AuditActionType.UpdateId,
            EntityState.Deleted => AuditActionType.DeleteId,
            _ => 0
        };

        if (actionTypeId == 0)
            return null;

        var oldValues = actionTypeId is AuditActionType.UpdateId or AuditActionType.DeleteId
            ? SerializeValues(entry, original: true, changedOnly: actionTypeId == AuditActionType.UpdateId)
            : null;

        var newValues = actionTypeId is AuditActionType.CreateId or AuditActionType.UpdateId
            ? SerializeValues(entry, original: false, changedOnly: actionTypeId == AuditActionType.UpdateId)
            : null;

        return new AuditCandidate(
            module,
            actionTypeId,
            entityTypeId,
            entityId.Value,
            NewValuesJson: newValues,
            OldValuesJson: oldValues,
            RelatedDocumentId: ResolveRelatedDocumentId(entry));
    }

    private static bool TryMapEntityType(Type type, out string module, out int entityTypeId)
    {
        (module, entityTypeId) = type switch
        {
            _ when type == typeof(Department) => ("ORGANIZATION", AuditEntityType.DepartmentId),
            _ when type == typeof(InstitutionSetting) => ("ORGANIZATION", AuditEntityType.SettingId),
            _ when type == typeof(ExternalOrganization) => ("ORGANIZATION", AuditEntityType.ExternalOrganizationId),
            _ when type == typeof(User) => ("IDENTITY", AuditEntityType.UserId),
            _ when type == typeof(Role) => ("IDENTITY", AuditEntityType.RoleId),
            _ when type == typeof(Employee) => ("IDENTITY", AuditEntityType.UserId),
            _ when type == typeof(UserDepartmentAssignment) => ("IDENTITY", AuditEntityType.UserId),
            _ when type == typeof(DocumentAccessGrant) => ("IDENTITY", AuditEntityType.DocumentId),
            _ when type == typeof(Document) => ("CORRESPONDENCE", AuditEntityType.DocumentId),
            _ when type == typeof(DocumentNumber) => ("NUMBERING", AuditEntityType.DocumentNumberId),
            _ when type == typeof(NumberingRegistry) => ("NUMBERING", AuditEntityType.NumberingRegistryId),
            _ when type == typeof(NumberingCounter) => ("NUMBERING", AuditEntityType.NumberingRegistryId),
            _ when type == typeof(DocumentFile) => ("ATTACHMENTS", AuditEntityType.FileId),
            _ when type == typeof(DocumentMovement) => ("MOVEMENTS", AuditEntityType.MovementId),
            _ when type == typeof(DocumentRelation) => ("RELATIONS", AuditEntityType.DocumentRelationId),
            _ => (string.Empty, 0)
        };

        return entityTypeId > 0;
    }

    private static Guid? ResolveEntityId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is null || primaryKey.Properties.Count != 1)
            return null;

        var property = primaryKey.Properties[0];
        var value = entry.Property(property.Name).CurrentValue;

        return value switch
        {
            Guid guid => guid,
            string text when Guid.TryParse(text, out var parsed) => parsed,
            _ => null
        };
    }

    private static Guid? ResolveRelatedDocumentId(EntityEntry entry)
    {
        var property = entry.Metadata.FindProperty("DocumentId");
        if (property is null)
            return null;

        var value = entry.Property("DocumentId").CurrentValue;
        return value is Guid documentId ? documentId : null;
    }

    private static string? SerializeValues(
        EntityEntry entry,
        bool original,
        bool changedOnly)
    {
        var values = new SortedDictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in entry.Properties)
        {
            if (changedOnly && !property.IsModified)
                continue;

            var name = property.Metadata.Name;
            if (IsSensitive(name))
                continue;

            var value = original ? property.OriginalValue : property.CurrentValue;
            values[name] = value;
        }

        return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
    }

    private static bool IsSensitive(string propertyName) =>
        SensitiveFragments.Any(fragment =>
            propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyCollection<IDomainEvent>? ReadDomainEvents(object entity)
    {
        var property = entity.GetType().GetProperty(
            "DomainEvents", BindingFlags.Public | BindingFlags.Instance);

        return property?.GetValue(entity) as IReadOnlyCollection<IDomainEvent>;
    }

    private static void ClearDomainEvents(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            var method = entry.Entity.GetType().GetMethod(
                "ClearDomainEvents", BindingFlags.Public | BindingFlags.Instance);

            method?.Invoke(entry.Entity, null);
        }
    }

    private static Guid? CurrentUserId(ClaimsPrincipal? principal) =>
        TryParseGuid(principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value)
        ?? TryParseGuid(principal?.FindFirst("sub")?.Value);

    private static Guid? CurrentDepartmentId(ClaimsPrincipal? principal) =>
        TryParseGuid(principal?.FindFirst(DepartmentClaimType)?.Value);

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(RequestContextKeys.CorrelationId, out var item) &&
            item is string correlationId &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        var header = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(header) ? context.TraceIdentifier : header;
    }

    private static Guid? TryParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;

    private static string? NewJson(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string? OldJson(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private sealed record AuditCandidate(
        string ModuleName,
        int ActionTypeId,
        int EntityTypeId,
        Guid EntityId,
        string? NewValuesJson = null,
        string? OldValuesJson = null,
        Guid? RelatedDocumentId = null,
        Guid? ActorUserId = null);

    private sealed record PendingAuditEntry(
        AuditCandidate Candidate,
        Guid? ActorUserId,
        DateTime OccurredAtUtc);

    /// <summary>The transaction this interceptor opened to hold the audit chain lock.</summary>
    private sealed class ChainSaveState
    {
        public ChainSaveState(IDbContextTransaction ownedTransaction) =>
            OwnedTransaction = ownedTransaction;

        public IDbContextTransaction OwnedTransaction { get; }
    }
}
