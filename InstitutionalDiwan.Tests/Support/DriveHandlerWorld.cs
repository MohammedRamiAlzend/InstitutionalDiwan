using InstitutionalDiwan.Application.Abstractions.Audit;
using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Drive;
using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Infrastructure.OnlyOffice;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>
/// Handler-test harness over <see cref="DriveTestWorld"/>: real SQLite repositories and the real
/// access evaluator, an in-memory S3 stand-in, a capturing notification publisher, and a unit of
/// work that commits to the same database. Handler factories bind a caller's identity and evaluator.
/// </summary>
internal sealed class DriveHandlerWorld
{
    public DriveTestWorld World { get; }
    public FakeInstitutionSettingRepository Settings { get; } = new();
    public InMemoryDriveStorage Storage { get; } = new();
    public CapturingDriveNotifications Notifications { get; } = new();
    public FakeFileSignatureValidator Signatures { get; } = new();
    public DbUnitOfWork UnitOfWork { get; }
    public DriveUploadGuard Guard { get; }
    public IDrivePurgeService Purge { get; }
    public DriveOnlyOfficeOptions OnlyOfficeOptions { get; } = new()
    {
        ServerUrl = "https://onlyoffice.local",
        CallbackBaseUrl = "https://api.local",
        JwtSecret = "unit-test-secret",
        Enabled = true
    };
    public OnlyOfficeTokenService Tokens { get; }
    public FakeDriveCallbackDownloader Downloader { get; } = new();

    private DriveHandlerWorld(DriveTestWorld world)
    {
        World = world;
        UnitOfWork = new DbUnitOfWork(world.Db);
        Guard = new DriveUploadGuard(Signatures);
        Settings.Add(InstitutionSetting.Create("مؤسسة", 1, 1).Value);
        Tokens = new OnlyOfficeTokenService(Options.Create(OnlyOfficeOptions));
        Purge = new DrivePurgeService(
            world.Db, UnitOfWork, world.Spaces, world.Versions, Storage, new NoopAuditRecorder());
    }

    public static async Task<DriveHandlerWorld> CreateAsync() => new(await DriveTestWorld.CreateAsync());

    public FakeUserContext ContextFor(User caller, bool isAdmin = false) => new()
    {
        IsAuthenticated = true,
        UserId = caller.UserId,
        Username = caller.Username,
        PrimaryDepartmentId = caller.PrimaryDepartmentId,
        Roles = isAdmin ? new HashSet<string>(StringComparer.Ordinal) { "ADMIN" } : new HashSet<string>(StringComparer.Ordinal),
        Permissions = isAdmin ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DRIVE_MANAGE" } : new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    };

    public IDriveAccessEvaluator AccessFor(User caller, bool isAdmin = false) => World.EvaluatorFor(caller, isAdmin);

    private FakeRecipientDirectory Directory => new(World);

    public CreateDriveSpaceCommandHandler CreateSpace(User caller, bool isAdmin = false) =>
        new(World.Spaces, Settings, AccessFor(caller, isAdmin), ContextFor(caller, isAdmin), UnitOfWork);

    public EnsurePersonalDriveSpaceCommandHandler EnsurePersonal(User caller) =>
        new(World.Spaces, Settings, ContextFor(caller), UnitOfWork);

    public SetDriveSpaceQuotaCommandHandler SetQuota(User caller, bool isAdmin = false) =>
        new(World.Spaces, Settings, AccessFor(caller, isAdmin), UnitOfWork);

    public CreateDriveFolderCommandHandler CreateFolder(User caller, bool isAdmin = false) =>
        new(World.Items, World.Spaces, AccessFor(caller, isAdmin), ContextFor(caller, isAdmin), UnitOfWork);

    public RenameDriveItemCommandHandler RenameItem(User caller, bool isAdmin = false) =>
        new(World.Items, AccessFor(caller, isAdmin), UnitOfWork);

    public MoveDriveItemCommandHandler MoveItem(User caller, bool isAdmin = false) =>
        new(World.Items, World.Spaces, AccessFor(caller, isAdmin), ContextFor(caller, isAdmin), UnitOfWork);

    public StarDriveItemCommandHandler Star(User caller) =>
        new(World.Items, World.Stars, AccessFor(caller), ContextFor(caller), UnitOfWork);

    public BrowseDriveItemsQueryHandler Browse(User caller, bool isAdmin = false) =>
        new(World.Items, World.Stars, AccessFor(caller, isAdmin), ContextFor(caller, isAdmin));

    public InitiateDriveUploadCommandHandler InitiateUpload(User caller, bool isAdmin = false) =>
        new(World.Items, World.Spaces, World.Versions, Settings, Storage, AccessFor(caller, isAdmin), Guard, ContextFor(caller, isAdmin), UnitOfWork);

    public CompleteDriveUploadCommandHandler CompleteUpload(User caller, bool isAdmin = false) =>
        new(World.Items, World.Spaces, World.Versions, Settings, Storage, AccessFor(caller, isAdmin), Guard, UnitOfWork);

    public RestoreDriveFileVersionCommandHandler RestoreVersion(User caller, bool isAdmin = false) =>
        new(World.Items, World.Spaces, World.Versions, Settings, Storage, AccessFor(caller, isAdmin), ContextFor(caller, isAdmin), UnitOfWork);

    public GetDriveFileContentQueryHandler FileContent(User caller, bool isAdmin = false) =>
        new(World.Items, World.Versions, Storage, AccessFor(caller, isAdmin));

    public GetDriveDownloadUrlQueryHandler DownloadUrl(User caller, bool isAdmin = false) =>
        new(World.Items, World.Versions, Storage, AccessFor(caller, isAdmin));

    public ListDriveFileVersionsQueryHandler FileVersions(User caller, bool isAdmin = false) =>
        new(World.Items, World.Versions, AccessFor(caller, isAdmin));

    public GrantDrivePermissionCommandHandler Grant(User caller, bool isAdmin = false) =>
        new(World.Items, World.Permissions, AccessFor(caller, isAdmin), Directory, Notifications, ContextFor(caller, isAdmin), UnitOfWork);

    public SetDriveItemInheritanceCommandHandler Inheritance(User caller, bool isAdmin = false) =>
        new(World.Items, AccessFor(caller, isAdmin), UnitOfWork);

    public RequestDriveItemDeletionCommandHandler RequestDeletion(User caller, bool isAdmin = false) =>
        new(World.Items, World.Requests, AccessFor(caller, isAdmin), new UserRepository(World.Db), new RoleRepository(World.Db), Notifications, ContextFor(caller, isAdmin), UnitOfWork);

    public ApproveDriveDeletionCommandHandler ApproveDeletion(User caller, bool isAdmin = false) =>
        new(World.Items, World.Requests, AccessFor(caller, isAdmin), Notifications, ContextFor(caller, isAdmin), UnitOfWork);

    public RejectDriveDeletionCommandHandler RejectDeletion(User caller, bool isAdmin = false) =>
        new(World.Items, World.Requests, AccessFor(caller, isAdmin), Notifications, ContextFor(caller, isAdmin), UnitOfWork);

    public CancelDriveDeletionCommandHandler CancelDeletion(User caller) =>
        new(World.Requests, ContextFor(caller), UnitOfWork);

    public DeleteDriveItemDirectlyCommandHandler DeleteDirectly(User caller, bool isAdmin = false) =>
        new(World.Items, AccessFor(caller, isAdmin), ContextFor(caller, isAdmin), UnitOfWork);

    public RestoreDriveItemCommandHandler RestoreItem(User caller, bool isAdmin = false) =>
        new(World.Items, AccessFor(caller, isAdmin), UnitOfWork);

    public PurgeDriveItemCommandHandler PurgeItem(User caller, bool isAdmin = false) =>
        new(World.Items, AccessFor(caller, isAdmin), Purge, ContextFor(caller, isAdmin));

    public UpdateDriveSettingsCommandHandler UpdateSettings(User caller, bool isAdmin = false) =>
        new(Settings, AccessFor(caller, isAdmin), UnitOfWork);

    public GetDriveEditConfigQueryHandler EditConfig(User caller, bool isAdmin = false) =>
        new(World.Items, World.Versions, World.Sessions, Storage, AccessFor(caller, isAdmin), OnlyOfficeOptions, Tokens, ContextFor(caller, isAdmin), UnitOfWork);

    public GetDriveReadinessQueryHandler Readiness() =>
        new(OnlyOfficeOptions, Tokens, Storage);

    public ProcessDriveOnlyOfficeCallbackCommandHandler Callback() =>
        new(World.Items, World.Spaces, World.Versions, World.Sessions, Settings, Storage, Downloader, Tokens, Guard, Notifications, UnitOfWork,
            NullLogger<ProcessDriveOnlyOfficeCallbackCommandHandler>.Instance);

    /// <summary>Commits handler writes to the shared SQLite database.</summary>
    public sealed class DbUnitOfWork : IUnitOfWork
    {
        private readonly DiwanDbContext _db;
        public DbUnitOfWork(DiwanDbContext db) => _db = db;
        public DateTime UtcNow { get; set; } = DateTime.UtcNow;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>An in-memory S3 stand-in: presigned URLs are opaque, bytes live in a dictionary.</summary>
    public sealed class InMemoryDriveStorage : IDriveStorageService
    {
        private readonly Dictionary<string, byte[]> _objects = new(StringComparer.Ordinal);

        public bool IsConfigured { get; set; } = true;

        public void PutObject(string key, byte[] bytes) => _objects[key] = bytes;
        public bool Exists(string key) => _objects.ContainsKey(key);
        public byte[]? Bytes(string key) => _objects.TryGetValue(key, out var bytes) ? bytes : null;

        public Task<DrivePresignedUpload> CreateUploadUrlAsync(string objectKey, string? mimeType, long maxBytes, TimeSpan lifetime, CancellationToken cancellationToken = default)
            => Task.FromResult(new DrivePresignedUpload(new Uri($"https://storage.local/{objectKey}?verb=PUT"), DateTime.UtcNow.Add(lifetime)));

        public Task<Uri> CreateDownloadUrlAsync(string objectKey, TimeSpan lifetime, CancellationToken cancellationToken = default)
            => Task.FromResult(new Uri($"https://storage.local/{objectKey}?verb=GET"));

        public Task<DriveStoredObject?> HeadAsync(string objectKey, CancellationToken cancellationToken = default)
            => Task.FromResult(_objects.TryGetValue(objectKey, out var bytes) ? new DriveStoredObject(bytes.Length, "application/octet-stream") : null);

        public Task<Stream?> OpenReadAsync(string objectKey, long? rangeStart = null, long? rangeEnd = null, CancellationToken cancellationToken = default)
        {
            if (!_objects.TryGetValue(objectKey, out var bytes))
                return Task.FromResult<Stream?>(null);
            var start = (int)(rangeStart ?? 0);
            var end = (int)Math.Min(rangeEnd ?? bytes.Length - 1, bytes.Length - 1);
            var slice = bytes[start..(end + 1)];
            return Task.FromResult<Stream?>(new MemoryStream(slice));
        }

        public Task<string> ComputeSha256Async(Stream content, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            return Task.FromResult(Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant());
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            _objects.Remove(objectKey);
            return Task.CompletedTask;
        }

        public Task CopyAsync(string sourceObjectKey, string destinationObjectKey, CancellationToken cancellationToken = default)
        {
            if (_objects.TryGetValue(sourceObjectKey, out var bytes))
                _objects[destinationObjectKey] = (byte[])bytes.Clone();
            return Task.CompletedTask;
        }

        public Task WriteAsync(string objectKey, Stream content, string? mimeType, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            _objects[objectKey] = buffer.ToArray();
            return Task.CompletedTask;
        }
    }

    /// <summary>An OnlyOffice save-callback downloader stand-in returning configurable bytes (or null).</summary>
    public sealed class FakeDriveCallbackDownloader : IDriveCallbackDownloader
    {
        public byte[]? Bytes { get; set; }
        public string? LastUrl { get; private set; }

        public Task<Stream?> DownloadAsync(string url, CancellationToken cancellationToken = default)
        {
            LastUrl = url;
            return Task.FromResult<Stream?>(Bytes is null ? null : new MemoryStream(Bytes));
        }
    }

    /// <summary>Records Drive notifications instead of staging inbox rows.</summary>
    public sealed class CapturingDriveNotifications : IDriveNotificationPublisher
    {
        public List<(int TypeId, IReadOnlyCollection<Guid> Recipients, string Title, Guid Actor)> Published { get; } = new();

        public Task PublishAsync(int notificationTypeId, IReadOnlyCollection<Guid> recipientUserIds, string itemTitle, Guid actorUserId, string? requesterName = null, string? actionUrl = null, CancellationToken cancellationToken = default)
        {
            Published.Add((notificationTypeId, recipientUserIds.ToList(), itemTitle, actorUserId));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRecipientDirectory : INotificationRecipientDirectory
    {
        private readonly DriveTestWorld _world;
        public FakeRecipientDirectory(DriveTestWorld world) => _world = world;

        public Task<IReadOnlyList<NotificationAudience>> GetDepartmentUsersAsync(Guid departmentId, DateTime asOfUtc, CancellationToken cancellationToken = default)
        {
            var users = new[] { _world.Owner, _world.Member, _world.Head, _world.Admin, _world.Stranger }
                .Where(user => user.PrimaryDepartmentId == departmentId && user.IsActive)
                .Select(user => new NotificationAudience(user.UserId, departmentId))
                .ToList();
            return Task.FromResult<IReadOnlyList<NotificationAudience>>(users);
        }

        public Task<IReadOnlyList<Guid>> FilterNotifiableAsync(Guid documentId, IReadOnlyCollection<Guid> userIds, DateTime asOfUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Guid>>(userIds.ToList());
    }

    private sealed class NoopAuditRecorder : IAuditActionRecorder
    {
        public Task RecordAsync(AuditActionRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}