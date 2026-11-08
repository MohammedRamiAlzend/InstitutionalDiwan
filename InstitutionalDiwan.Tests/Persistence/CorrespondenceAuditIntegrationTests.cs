using InstitutionalDiwan.Application.Abstractions.Context;
using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Interceptors;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Exercises the real repositories, SQLite context, and audit interceptor end to end — the same
/// save path the API uses — to prove that document commands append rows to AUDIT_LOGS.
/// </summary>
public sealed class CorrespondenceAuditIntegrationTests
{
    private const string Number = "2026/000001";
    private const int Year = 2026;

    [Fact]
    public async Task CreateDocument_ThroughRealRepositories_AppendsAuditRows()
    {
        var fixture = await Fixture.CreateAsync();
        using var db = fixture.Db;
        fixture.Authenticate();

        var registries = new NumberingRegistryRepository(db);
        registries.Add(NumberingRegistry.Create(
            "DEPT-BOOK", "دفتر القسم", departmentId: fixture.User.PrimaryDepartmentId).Value);
        await db.SaveChangesAsync();
        var numbers = new DocumentNumberRepository(db);
        var handler = new CreateDocumentCommandHandler(
            new UserRepository(db),
            new DocumentRepository(db),
            new DocumentTypeRepository(db),
            numbers,
            registries,
            new DocumentTypeSchemaVersionRepository(db),
            new FakeDocumentFieldOptionResolver(),
            db,
            new FakeSender());

        var result = await handler.Handle(
            new CreateDocumentCommand(
                fixture.User.UserId,
                "طلب تجديد",
                DocumentType.OutgoingLetterId,
                "17/2026",
                new DateTime(2026, 3, 9),
                SubjectSummary: "تجديد رخصة",
                DirectionId: DocumentDirection.OutgoingId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var logs = await db.AuditLogs.OrderBy(log => log.AuditLogId).ToListAsync();

        var created = Assert.Single(logs, log =>
            log.EntityTypeId == AuditEntityType.DocumentId &&
            log.ActionTypeId == AuditActionType.CreateId);
        Assert.Equal(result.Value.DocumentId.ToString(), created.EntityId);
        Assert.Equal(fixture.User.UserId, created.ActorUserId);
        Assert.Equal(fixture.User.PrimaryDepartmentId, created.ActorDepartmentId);
        Assert.Equal("correlation-1", created.CorrelationId);
        Assert.Equal("10.0.0.15", created.IpAddress);

        Assert.Contains(logs, log =>
            log.EntityTypeId == AuditEntityType.DocumentId &&
            log.ActionTypeId == AuditActionType.UpdateId &&
            log.EntityId == result.Value.DocumentId.ToString());

        Assert.Contains(logs, log =>
            log.EntityTypeId == AuditEntityType.DocumentNumberId &&
            log.ActionTypeId == AuditActionType.CreateId);

        Assert.All(logs, log => Assert.Equal(
            log.PreviousHash,
            logs.FirstOrDefault(previous => previous.AuditLogId == log.AuditLogId - 1)?.EventHash
                ?? AuditLog.GenesisHash));
    }

    [Fact]
    public async Task UpdateDraftMetadata_ThroughRealRepositories_AppendsAnUpdateAuditRow()
    {
        var fixture = await Fixture.CreateAsync();
        using var db = fixture.Db;

        var draft = Document.CreateDraft(
            "عنوان قديم",
            DocumentType.OutgoingLetterId,
            DocumentDirection.OutgoingId,
            fixture.User.PrimaryDepartmentId,
            fixture.User.UserId,
            subjectSummary: "ملخص قديم").Value;
        db.Documents.Add(draft);
        await db.SaveChangesAsync();

        fixture.Authenticate();

        var handler = new UpdateDraftMetadataCommandHandler(
            new DocumentRepository(db), new FakeDocumentAccessEvaluator { AllowAll = true }, db);

        var result = await handler.Handle(
            new UpdateDraftMetadataCommand(draft.DocumentId, "عنوان جديد", "ملخص جديد"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var log = await db.AuditLogs.SingleAsync(entry =>
            entry.EntityTypeId == AuditEntityType.DocumentId &&
            entry.EntityId == draft.DocumentId.ToString() &&
            entry.ActionTypeId == AuditActionType.UpdateId);

        Assert.Equal(fixture.User.UserId, log.ActorUserId);
        Assert.Equal("عنوان قديم", ReadTitle(log.OldValuesJson));
        Assert.Equal("عنوان جديد", ReadTitle(log.NewValuesJson));
        Assert.True(log.VerifyIntegrity(AuditLog.GenesisHash).IsSuccess);
    }

    [Fact]
    public async Task PublishingASchemaVersion_ThroughRealRepositories_AppendsSchemaAuditRows()
    {
        var fixture = await Fixture.CreateAsync();
        using var db = fixture.Db;
        fixture.Authenticate();

        var fields = new DocumentFieldRepository(db);
        var versions = new DocumentTypeSchemaVersionRepository(db);
        var types = new DocumentTypeRepository(db);
        var userId = fixture.User.UserId;

        var field = await new CreateDocumentFieldCommandHandler(types, fields, db).Handle(
            new CreateDocumentFieldCommand(userId, DocumentType.OutgoingLetterId, "session_no", DocumentFieldDataType.Text),
            CancellationToken.None);
        var draft = await new CreateSchemaDraftCommandHandler(types, versions, db).Handle(
            new CreateSchemaDraftCommand(userId, DocumentType.OutgoingLetterId), CancellationToken.None);
        var added = await new AddFieldToDraftCommandHandler(versions, fields, new FakeDocumentFieldOptionResolver(), db).Handle(
            new AddFieldToDraftCommand(draft.Value.SchemaVersionId, field.Value.DocumentFieldId,
                new DocumentFieldSettingsInput("رقم الجلسة", IsRequired: true)),
            CancellationToken.None);
        var published = await new PublishSchemaVersionCommandHandler(versions, db).Handle(
            new PublishSchemaVersionCommand(draft.Value.SchemaVersionId, userId), CancellationToken.None);

        Assert.True(field.IsSuccess && draft.IsSuccess && added.IsSuccess && published.IsSuccess);

        var logs = await db.AuditLogs
            .Where(log => log.EntityTypeId == AuditEntityType.DocumentSchemaVersionId)
            .ToListAsync();

        Assert.Contains(logs, log => log.EntityId == field.Value.DocumentFieldId.ToString()
            && log.ActionTypeId == AuditActionType.CreateId);
        Assert.Contains(logs, log => log.EntityId == draft.Value.SchemaVersionId.ToString()
            && log.ActionTypeId == AuditActionType.CreateId);
        var publish = Assert.Single(logs, log => log.EntityId == draft.Value.SchemaVersionId.ToString()
            && log.ActionTypeId == AuditActionType.UpdateId && log.ActorUserId == userId
            && log.NewValuesJson!.Contains("Published"));
        Assert.Contains("Published", publish.NewValuesJson!);
        Assert.Contains("Draft", publish.OldValuesJson!);
    }

    private static string? ReadTitle(string? json)
    {
        using var document = JsonDocument.Parse(json!);
        return document.RootElement.GetProperty("Title").GetString();
    }

    private sealed class Fixture
    {
        private readonly HttpContextAccessor _accessor;

        private Fixture(DiwanDbContext db, HttpContextAccessor accessor, User user)
        {
            Db = db;
            _accessor = accessor;
            User = user;
        }

        public DiwanDbContext Db { get; }

        public User User { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var accessor = new HttpContextAccessor();
            var db = TestContextFactory.Create(
                new TamperEvidentAuditInterceptor(accessor, TestClock.Instance));

            TestContextFactory.SeedAuditLookups(db);

            db.DepartmentTypes.Add(DepartmentType.Create(
                DepartmentType.CentralDiwanId, "Central Diwan", "الديوان المركزي", 0).Value);
            db.DocumentDirections.Add(DocumentDirection.Create(
                DocumentDirection.OutgoingId, "Outgoing", "صادر").Value);
            db.DocumentTypes.Add(DocumentType.Create(
                DocumentType.OutgoingLetterId, "Outgoing letter", "خطاب صادر",
                DocumentDirection.OutgoingId).Value);
            db.DocumentStatusTypes.Add(DocumentStatusType.Create(
                DocumentStatusType.DraftId, "Draft", "مسودة").Value);
            db.DocumentStatusTypes.Add(DocumentStatusType.Create(
                DocumentStatusType.RegisteredId, "Registered", "مسجل").Value);
            db.DocumentConfidentialityLevels.Add(DocumentConfidentialityLevel.Create(
                DocumentConfidentialityLevel.NormalId, "Normal", "عادي").Value);
            db.DocumentPriorityLevels.Add(DocumentPriorityLevel.Create(
                DocumentPriorityLevel.NormalId, "Normal", "عادي").Value);
            db.DocumentNumberStatuses.Add(DocumentNumberStatus.Create(
                DocumentNumberStatus.ActiveId, "Active", "فعال").Value);
            db.DocumentNumberScopes.Add(DocumentNumberScope.Create(
                DocumentNumberScope.CentralId, "Central", "مركزي").Value);
            db.DocumentNumberEventTypes.Add(DocumentNumberEventType.Create(
                DocumentNumberEventType.IssuedId, "Issued", "صادر").Value);
            db.NumberingResetPolicies.Add(NumberingResetPolicy.Create(
                NumberingResetPolicy.YearlyId, "Yearly", "سنوي").Value);
            db.UserScopeTypes.Add(UserScopeType.Create(
                UserScopeType.DepartmentId, "Department", "دائرة").Value);

            var department = Department.Create(
                "DIWAN", "الديوان", DepartmentType.CentralDiwanId).Value;
            db.Departments.Add(department);

            var user = User.Create("author", "hash", department.DepartmentId).Value;
            db.Users.Add(user);

            // Seeding is not a user action, so no audit rows may be produced here.
            await db.SaveChangesAsync();
            Assert.Empty(db.AuditLogs);

            return new Fixture(db, accessor, user);
        }

        public void Authenticate()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.15");
            context.Items[RequestContextKeys.CorrelationId] = "correlation-1";
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, User.UserId.ToString()),
                new Claim("dept_id", User.PrimaryDepartmentId.ToString())
            }, authenticationType: "Test"));

            _accessor.HttpContext = context;
        }
    }
}
