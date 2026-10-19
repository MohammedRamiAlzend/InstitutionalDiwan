namespace InstitutionalDiwan.Domain.Common;

/// <summary>
/// Canonical permission codes. Single source of truth shared by the seed data (catalog and role
/// grants), the JWT <c>permission</c> claims, and the server's authorization policies — the three
/// must use identical strings for enforcement to line up.
/// </summary>
public static class Permissions
{
    public const string DocumentView = "DOCUMENT_VIEW";
    public const string DocumentCreate = "DOCUMENT_CREATE";
    public const string DocumentEdit = "DOCUMENT_EDIT";
    public const string DocumentDelete = "DOCUMENT_DELETE";
    public const string DocumentApprove = "DOCUMENT_APPROVE";
    public const string NumberingView = "NUMBERING_VIEW";
    public const string DepartmentManage = "DEPARTMENT_MANAGE";
    public const string RoleManage = "ROLE_MANAGE";
    public const string UserManage = "USER_MANAGE";
    public const string EmployeeManage = "EMPLOYEE_MANAGE";
    public const string AccessGrantManage = "ACCESS_GRANT_MANAGE";
    public const string ReportView = "REPORT_VIEW";
    public const string LookupManage = "LOOKUP_MANAGE";

    /// <summary>Every known permission — used to register one authorization policy per permission.</summary>
    public static readonly IReadOnlyCollection<string> All = new[]
    {
        DocumentView, DocumentCreate, DocumentEdit, DocumentDelete, DocumentApprove,
        NumberingView, DepartmentManage, RoleManage, UserManage, EmployeeManage,
        AccessGrantManage, ReportView, LookupManage,
    };
}
