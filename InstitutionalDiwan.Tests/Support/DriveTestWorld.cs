using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Infrastructure.Authorization;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>
/// A small Drive organization on a private SQLite database: one department with a head, an
/// unrelated department, five users (owner, member, stranger, head, admin), three spaces and a
/// three-level tree in the owner's shared space. Repository and evaluator tests build on it.
/// </summary>
internal sealed class DriveTestWorld
{
    public DiwanDbContext Db { get; }
    public Department Dept { get; }
    public Department Other { get; }
    public User Owner { get; }
    public User Member { get; }
    public User Stranger { get; }
    public User Head { get; }
    public User Admin { get; }
    public DriveSpace Personal { get; }
    public DriveSpace Shared { get; }
    public DriveSpace Foreign { get; }
    public DriveItem RootFolder { get; }
    public DriveItem ChildFolder { get; }
    public DriveItem DeepFile { get; }
    public DriveItem MemberFile { get; }

    public DriveItemRepository Items => new(Db);
    public DriveSpaceRepository Spaces => new(Db);
    public DriveFileVersionRepository Versions => new(Db);
    public DrivePermissionRepository Permissions => new(Db);
    public DriveDeletionRequestRepository Requests => new(Db);
    public DriveEditSessionRepository Sessions => new(Db);
    public DriveStarRepository Stars => new(Db);

    private DriveTestWorld(
        DiwanDbContext db,
        Department dept,
        Department other,
        User owner,
        User member,
        User stranger,
        User head,
        User admin,
        DriveSpace personal,
        DriveSpace shared,
        DriveSpace foreign,
        DriveItem rootFolder,
        DriveItem childFolder,
        DriveItem deepFile,
        DriveItem memberFile)
    {
        Db = db;
        Dept = dept;
        Other = other;
        Owner = owner;
        Member = member;
        Stranger = stranger;
        Head = head;
        Admin = admin;
        Personal = personal;
        Shared = shared;
        Foreign = foreign;
        RootFolder = rootFolder;
        ChildFolder = childFolder;
        DeepFile = deepFile;
        MemberFile = memberFile;
    }

    public static async Task<DriveTestWorld> CreateAsync()
    {
        var db = TestContextFactory.Create();

        db.DepartmentTypes.Add(DepartmentType.Create(DepartmentType.SectionId, "Section", "شعبة", 0).Value);
        foreach (var id in new[] { UserScopeType.DepartmentId, UserScopeType.DepartmentTreeId, UserScopeType.InstitutionId, UserScopeType.SystemId })
            db.UserScopeTypes.Add(UserScopeType.Create(id, $"Scope {id}", $"نطاق {id}").Value);
        db.StorageProviderTypes.Add(StorageProviderType.Create(StorageProviderType.AwsS3Id, "S3", "إس 3").Value);
        db.FileStorageProviders.Add(FileStorageProvider.Create(
            FileStorageProvider.DriveSeaweedFsId, "SeaweedFS (Drive)", "SeaweedFS (نظام الملفات)",
            "drive", StorageProviderType.AwsS3Id).Value);

        var dept = Department.Create("DIWAN", "الديوان", DepartmentType.SectionId).Value;
        var other = Department.Create("OTHER", "قسم آخر", DepartmentType.SectionId).Value;
        var headEmployee = Employee.Create("رئيس الديوان", dept.DepartmentId, "EMP-HEAD").Value;
        dept.AssignManager(headEmployee.EmployeeId);
        db.Departments.AddRange(dept, other);
        db.Employees.Add(headEmployee);

        var owner = User.Create("owner", "hash", dept.DepartmentId).Value;
        var member = User.Create("member", "hash", dept.DepartmentId).Value;
        var stranger = User.Create("stranger", "hash", other.DepartmentId).Value;
        var head = User.Create("head", "hash", dept.DepartmentId, employeeId: headEmployee.EmployeeId).Value;
        var admin = User.Create("admin", "hash", dept.DepartmentId).Value;
        var adminRole = Role.Create("ADMIN", "مدير النظام").Value;
        admin.AssignRole(adminRole.RoleId);
        db.Roles.Add(adminRole);
        db.Users.AddRange(owner, member, stranger, head, admin);

        var personal = DriveSpace.CreatePersonal(owner.UserId).Value;
        var shared = DriveSpace.Create("مساحة الفريق", DriveSpaceType.SharedId, owner.UserId).Value;
        var foreign = DriveSpace.Create("مساحة غريبة", DriveSpaceType.SharedId, stranger.UserId).Value;
        db.DriveSpaces.AddRange(personal, shared, foreign);

        var rootFolder = DriveItem.CreateFolder(shared.SpaceId, null, "مشروع", owner.UserId).Value;
        var childFolder = DriveItem.CreateFolder(shared.SpaceId, rootFolder.ItemId, "فرعي", owner.UserId, depth: 2).Value;
        var deepFile = DriveItem.CreateFile(shared.SpaceId, childFolder.ItemId, "تقرير.docx", owner.UserId, depth: 3).Value;
        var memberFile = DriveItem.CreateFile(shared.SpaceId, null, "ملاحظة.txt", member.UserId).Value;
        db.DriveItems.AddRange(rootFolder, childFolder, deepFile, memberFile);

        await db.SaveChangesAsync();
        return new DriveTestWorld(
            db, dept, other, owner, member, stranger, head, admin,
            personal, shared, foreign, rootFolder, childFolder, deepFile, memberFile);
    }

    public DriveAccessEvaluator EvaluatorFor(User caller, bool isAdmin = false) =>
        new(
            new FakeUserContext
            {
                IsAuthenticated = true,
                UserId = caller.UserId,
                PrimaryDepartmentId = caller.PrimaryDepartmentId,
                Roles = isAdmin ? new HashSet<string>(StringComparer.Ordinal) { "ADMIN" } : new HashSet<string>(StringComparer.Ordinal)
            },
            Items, Spaces, Permissions, Db);

    public static IReadOnlyCollection<Guid> DepartmentIdsFor(User caller) => new[] { caller.PrimaryDepartmentId };

    public async Task<DriveItem> AddFolderAsync(string name, Guid? parentId = null, Guid? ownerUserId = null)
    {
        var folder = DriveItem.CreateFolder(Shared.SpaceId, parentId, name, ownerUserId ?? Owner.UserId).Value;
        Db.DriveItems.Add(folder);
        await Db.SaveChangesAsync();
        return folder;
    }

    public async Task<DrivePermission> AddGrantAsync(Guid itemId, int principalTypeId, Guid principalId, int roleId)
    {
        var grant = DrivePermission.Create(itemId, principalTypeId, principalId, roleId, Owner.UserId).Value;
        Db.DrivePermissions.Add(grant);
        await Db.SaveChangesAsync();
        return grant;
    }

    public async Task<DriveFileVersion> AddVersionAsync(
        DriveItem file, int number, DateTime? createdAt = null, bool commit = false)
    {
        var version = DriveFileVersion.CreatePending(
            file.ItemId, number, FileStorageProvider.DriveSeaweedFsId,
            $"{file.SpaceId}/{file.ItemId}/{number}", 1024, "text/plain",
            DriveVersionSource.UploadId, Owner.UserId, createdAt).Value;
        if (commit)
            version.Commit(new string('a', 64), 1024);
        Db.DriveFileVersions.Add(version);
        await Db.SaveChangesAsync();
        return version;
    }
}