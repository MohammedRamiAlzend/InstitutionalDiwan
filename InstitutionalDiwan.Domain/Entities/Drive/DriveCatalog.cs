namespace InstitutionalDiwan.Domain.Entities.Drive;

/// <summary>Structural constants for Drive (Module 11). These are code contracts, not editable lookups.</summary>
public static class DriveSpaceType
{
    public const int PersonalId = 1;
    public const int SharedId = 2;

    public static bool IsValid(int id) => id is PersonalId or SharedId;
}

public static class DriveItemKind
{
    public const int FolderId = 1;
    public const int FileId = 2;

    public static bool IsValid(int id) => id is FolderId or FileId;
}

/// <summary>Effective-access roles, ordered weakest to strongest.</summary>
public static class DriveRole
{
    public const int ViewerId = 1;
    public const int EditorId = 2;
    public const int ManagerId = 3;

    public static bool IsValid(int id) => id is ViewerId or EditorId or ManagerId;
}

public static class DrivePrincipalType
{
    public const int UserId = 1;
    public const int DepartmentId = 2;

    public static bool IsValid(int id) => id is UserId or DepartmentId;
}

public static class DriveVersionStatus
{
    public const int PendingId = 1;
    public const int CommittedId = 2;
    public const int AbandonedId = 3;

    public static bool IsValid(int id) => id is PendingId or CommittedId or AbandonedId;
}

public static class DriveVersionSource
{
    public const int UploadId = 1;
    public const int EditorSaveId = 2;
    public const int RestoreId = 3;

    public static bool IsValid(int id) => id is UploadId or EditorSaveId or RestoreId;
}

public static class DriveDeletionRequestStatus
{
    public const int PendingId = 1;
    public const int ApprovedId = 2;
    public const int RejectedId = 3;
    public const int CancelledId = 4;

    public static bool IsValid(int id) => id is PendingId or ApprovedId or RejectedId or CancelledId;
}

public static class DriveEditSessionStatus
{
    public const int OpenId = 1;
    public const int SavedId = 2;
    public const int ClosedId = 3;
    public const int ExpiredId = 4;

    public static bool IsValid(int id) => id is OpenId or SavedId or ClosedId or ExpiredId;
}