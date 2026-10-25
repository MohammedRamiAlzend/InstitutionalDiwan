using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the permission catalog referenced by role grants (Order 100, every environment) so that
/// authorization always has data to resolve permission codes against, in Development and
/// Production alike. Runs first so <see cref="RoleSeeder"/> grants against a complete catalog.
/// </summary>
public sealed class PermissionSeeder : ISeeder
{
    public int Order => 100;

    public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

    private static readonly (string Code, string NameAr, string NameEn, string Module, string Description)[] Catalog =
    {
        (Permissions.DocumentView, "عرض المستندات", "View documents", "CORRESPONDENCE", "View document details and content."),
        (Permissions.DocumentCreate, "إنشاء المستندات", "Create documents", "CORRESPONDENCE", "Create and register new documents."),
        (Permissions.DocumentEdit, "تعديل المستندات", "Edit documents", "CORRESPONDENCE", "Edit document metadata and content."),
        (Permissions.DocumentDelete, "حذف المستندات", "Delete documents", "CORRESPONDENCE", "Delete documents they own or manage."),
        (Permissions.DocumentApprove, "اعتماد المستندات", "Approve documents", "CORRESPONDENCE", "Approve outgoing documents in the workflow."),
        (Permissions.NumberingView, "عرض سجلات الترقيم", "View numbering registries", "NUMBERING", "View document numbering registries and their counters."),
        (Permissions.NumberingManage, "إدارة سجلات الترقيم", "Manage numbering registries", "NUMBERING", "Create, update and activate numbering registries and issue official numbers."),
        (Permissions.DepartmentManage, "إدارة الأقسام", "Manage departments", "ORGANIZATION", "Create, rename and reorganize departments."),
        (Permissions.RoleManage, "إدارة الأدوار", "Manage roles", "IDENTITY", "Create roles and grant or revoke permissions."),
        (Permissions.UserManage, "إدارة المستخدمين", "Manage users", "IDENTITY", "Create, deactivate and assign roles to users."),
        (Permissions.EmployeeManage, "إدارة الموظفين", "Manage employees", "IDENTITY", "Create, update and deactivate employee records."),
        (Permissions.AccessGrantManage, "إدارة صلاحيات الوصول", "Manage document access", "IDENTITY", "Grant and revoke per-document access for users."),
        (Permissions.ReportView, "عرض التقارير", "View reports", "REPORTING", "View institutional reports and statistics."),
        (Permissions.LookupManage, "إدارة القوائم المرجعية", "Manage reference lookups", "LOOKUPS", "Add, relabel, activate and deactivate reference lookup entries."),
        (Permissions.AuditView, "عرض سجل التدقيق", "View audit log", "AUDIT", "Browse and verify the tamper-evident audit log."),
        (Permissions.AuditManage, "إدارة سلامة سجل التدقيق", "Manage audit integrity", "AUDIT", "Create checkpoints and run integrity verification over the audit log."),
    };

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;

        var existingCodes = (await db.Permissions
                .Select(permission => permission.Code)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var (code, nameAr, nameEn, moduleName, description) in Catalog)
        {
            if (existingCodes.Contains(code))
                continue;

            var permissionResult = Permission.Create(code, nameAr, moduleName, nameEn, description);
            if (permissionResult.IsFailure)
                throw new InvalidOperationException(
                    $"Invalid permission seed data '{code}': {permissionResult.Error.Code} - {permissionResult.Error.Description}");

            db.Permissions.Add(permissionResult.Value);
            added++;
        }

        if (added > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
