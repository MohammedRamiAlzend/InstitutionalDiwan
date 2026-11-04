using InstitutionalDiwan.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds two accounts for each diwan of the standard organization (Order 310,
/// <see cref="SeedingModes.Standard"/> only). The login name is the Arabic name itself, and each
/// account also gets an employee record with that name in its diwan so the name shows in the UI.
/// All accounts hold the <c>USER</c> role; the Central Diwan accounts see the whole institution
/// and the other diwans' accounts see their own department. Idempotent by username: an existing
/// account is never changed.
/// </summary>
public sealed class StandardAccountSeeder : ISeeder
{
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

        var userRole = await db.Roles.FirstOrDefaultAsync(role => role.Code == "USER", cancellationToken)
            ?? throw new InvalidOperationException(
                "Role 'USER' not found — RoleSeeder (Order 200) must run before StandardAccountSeeder.");

        foreach (var seed in Catalog)
        {
            var username = seed.Username.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(user => user.Username == username, cancellationToken))
                continue;

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
}
