using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds two accounts for each diwan of the standard organization (Order 310,
/// <see cref="SeedingModes.Standard"/> only). The login name is the Arabic name itself, and each
/// account also gets an employee record with that name in its diwan so the name shows in the UI.
/// All accounts hold the <c>DIWAN_OPERATOR</c> role (the document permissions plus
/// <c>NUMBERING_VIEW</c> and <c>NUMBERING_MANAGE</c>): a diwan must write its own department
/// register number before it can receive a document, and that needs both numbering permissions.
/// The Central Diwan accounts see the whole institution and the other diwans' accounts see their
/// own department. Idempotent by username; an existing account only gains the operator role
/// (and loses the plain <c>USER</c> role the first version of this seeder gave it).
/// </summary>
public sealed class StandardAccountSeeder : ISeeder
{
    public const string OperatorRoleCode = "DIWAN_OPERATOR";
    private const string LegacyUserRoleCode = "USER";

    private static readonly string[] OperatorPermissions =
    {
        Permissions.DocumentView,
        Permissions.DocumentCreate,
        Permissions.DocumentEdit,
        Permissions.NumberingView,
        Permissions.NumberingManage,
        Permissions.DecisionManage,
    };

    public int Order => 310;

    public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

    public IReadOnlyCollection<string> Modes { get; } = new[] { SeedingModes.Standard };

    private sealed record AccountSeed(
        string Username,
        string EmployeeNumber,
        string DepartmentCode,
        int ScopeTypeId);

    private static readonly AccountSeed[] Catalog =
    {
        new("الديوان المركزي", "STD-0001", StandardDepartmentSeeder.CentralDiwanCode, UserScopeType.InstitutionId),
        new("احمد ديوان مركزي", "STD-0002", StandardDepartmentSeeder.CentralDiwanCode, UserScopeType.InstitutionId),
        new("ديوان الفرد", "STD-0003", StandardDepartmentSeeder.IndividualDiwanCode, UserScopeType.DepartmentId),
        new("احمد ديوان الفرد", "STD-0004", StandardDepartmentSeeder.IndividualDiwanCode, UserScopeType.DepartmentId),
        new("ديوان التحقيق", "STD-0005", StandardDepartmentSeeder.InvestigationDiwanCode, UserScopeType.DepartmentId),
        new("احمد ديوان التحقيق", "STD-0006", StandardDepartmentSeeder.InvestigationDiwanCode, UserScopeType.DepartmentId),
    };

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;

        var userRole = await EnsureOperatorRoleAsync(db, cancellationToken);
        var legacyRoleId = await db.Roles
            .Where(role => role.Code == LegacyUserRoleCode)
            .Select(role => (Guid?)role.RoleId)
            .FirstOrDefaultAsync(cancellationToken);

        foreach (var seed in Catalog)
        {
            var username = seed.Username.Trim().ToLowerInvariant();
            var existing = await db.Users
                .Include(user => user.UserRoles)
                .FirstOrDefaultAsync(user => user.Username == username, cancellationToken);
            if (existing is not null)
            {
                if (existing.UserRoles.All(r => r.RoleId != userRole.RoleId))
                    existing.AssignRole(userRole.RoleId);
                if (legacyRoleId is { } legacy && existing.UserRoles.Any(r => r.RoleId == legacy))
                    existing.RevokeRole(legacy);

                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            var departmentId = await db.Departments
                .Where(department => department.Code == seed.DepartmentCode)
                .Select(department => (Guid?)department.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Department '{seed.DepartmentCode}' not found — StandardDepartmentSeeder (Order 98) must run before StandardAccountSeeder.");

            var employee = await db.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumber == seed.EmployeeNumber, cancellationToken);
            if (employee is null)
            {
                var employeeResult = Employee.Create(
                    seed.Username, departmentId, employeeNumber: seed.EmployeeNumber);
                if (employeeResult.IsFailure)
                    throw new InvalidOperationException(
                        $"Invalid employee seed data '{seed.EmployeeNumber}': {employeeResult.Error.Code} - {employeeResult.Error.Description}");

                employee = employeeResult.Value;
                db.Employees.Add(employee);
            }

            var userResult = User.Create(
                username,
                context.PasswordHasher.Hash(context.Options.DefaultPassword),
                primaryDepartmentId: departmentId,
                employeeId: employee.EmployeeId,
                scopeTypeId: seed.ScopeTypeId,
                mustChangePassword: false);
            if (userResult.IsFailure)
                throw new InvalidOperationException(
                    $"Invalid user seed data '{username}': {userResult.Error.Code} - {userResult.Error.Description}");

            var assigned = userResult.Value.AssignRole(userRole.RoleId);
            if (assigned.IsFailure)
                throw new InvalidOperationException(
                    $"Could not assign role '{userRole.Code}' to '{username}': {assigned.Error.Code} - {assigned.Error.Description}");

            db.Users.Add(userResult.Value);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<Role> EnsureOperatorRoleAsync(DiwanDbContext db, CancellationToken cancellationToken)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Code == OperatorRoleCode, cancellationToken);
        if (role is null)
        {
            var created = Role.Create(
                OperatorRoleCode,
                "موظف ديوان",
                "Diwan Operator",
                "Documents plus department register numbering",
                isSystemRole: true);
            if (created.IsFailure)
                throw new InvalidOperationException(
                    $"Invalid role seed data '{OperatorRoleCode}': {created.Error.Code} - {created.Error.Description}");

            role = created.Value;
            db.Roles.Add(role);
        }

        var granted = (await db.RolePermissions
                .Where(rp => rp.RoleId == role.RoleId)
                .Select(rp => rp.PermissionCode)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var code in OperatorPermissions.Where(code => !granted.Contains(code)))
            role.GrantPermission(code);

        await db.SaveChangesAsync(cancellationToken);
        return role;
    }
}
