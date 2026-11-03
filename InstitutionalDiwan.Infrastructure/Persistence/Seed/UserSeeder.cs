using System.Linq.Expressions;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the demo accounts used to exercise the login flow (Order 300). Runs after
/// <see cref="RoleSeeder"/> so the roles it assigns already exist, and after
/// <see cref="InstitutionSettingSeeder"/> and <see cref="DepartmentSeeder"/> so each account belongs to
/// a real department: <c>admin</c> to the Central Diwan and <c>user</c> to <c>DEP-HR</c>. Existing
/// accounts are never changed.
///
/// Gated to Development and Staging only: these accounts share a well-known password and must
/// never reach Production. To seed a production account, add <see cref="SeedingEnvironments.Production"/>
/// to <see cref="Environments"/> and supply credentials from configuration instead.
/// </summary>
public sealed class UserSeeder : ISeeder
{
    /// <summary>Username of the seeded administrator account.</summary>
    public const string DemoUsername = "admin";

    /// <summary>Username of the seeded standard account.</summary>
    public const string DemoStandardUsername = "user";

    /// <summary>Shared demo password (Development/Staging only).</summary>
    public const string DemoPassword = "12345678";

    /// <summary>Code of the demo department the standard account belongs to (seeded by <see cref="DepartmentSeeder"/>).</summary>
    private const string StandardUserDepartmentCode = "DEP-HR";

    public int Order => 300;

    public IReadOnlyCollection<string> Environments { get; } =
        new[] { SeedingEnvironments.Development, SeedingEnvironments.Staging };

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "ADMIN", cancellationToken)
            ?? throw new InvalidOperationException(
                "Role 'ADMIN' not found — RoleSeeder (Order 200) must run before UserSeeder.");

        var userRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "USER", cancellationToken)
            ?? throw new InvalidOperationException(
                "Role 'USER' not found — RoleSeeder (Order 200) must run before UserSeeder.");

        await EnsureUserAsync(
            context,
            DemoUsername,
            adminRole,
            UserScopeType.SystemId,
            department => department.DepartmentTypeId == DepartmentType.CentralDiwanId,
            "Central Diwan department not found — InstitutionSettingSeeder (Order 95) must run before UserSeeder.",
            cancellationToken);

        await EnsureUserAsync(
            context,
            DemoStandardUsername,
            userRole,
            UserScopeType.DepartmentId,
            department => department.Code == StandardUserDepartmentCode,
            $"Department '{StandardUserDepartmentCode}' not found — DepartmentSeeder (Order 96) must run before UserSeeder.",
            cancellationToken);
    }

    private static async Task EnsureUserAsync(
        SeederContext context,
        string username,
        Role role,
        int scopeTypeId,
        Expression<Func<Department, bool>> primaryDepartment,
        string missingDepartmentMessage,
        CancellationToken cancellationToken)
    {
        var db = context.Db;
        var normalized = username.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Username == normalized, cancellationToken))
            return;

        // Resolved only for a missing account, so an existing user never depends on the demo tree.
        var primaryDepartmentId = await db.Departments
            .Where(primaryDepartment)
            .Select(department => (Guid?)department.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(missingDepartmentMessage);

        var passwordHash = context.PasswordHasher.Hash(DemoPassword);
        var userResult = User.Create(
            normalized,
            passwordHash,
            primaryDepartmentId: primaryDepartmentId,
            scopeTypeId: scopeTypeId,
            mustChangePassword: false);

        if (userResult.IsFailure)
            throw new InvalidOperationException(
                $"Invalid user seed data '{normalized}': {userResult.Error.Code} - {userResult.Error.Description}");

        var user = userResult.Value;
        var assignResult = user.AssignRole(role.RoleId);
        if (assignResult.IsFailure)
            throw new InvalidOperationException(
                $"Could not assign role '{role.Code}' to '{normalized}': {assignResult.Error.Code} - {assignResult.Error.Description}");

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }
}
