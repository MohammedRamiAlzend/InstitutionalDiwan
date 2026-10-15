using InstitutionalDiwan.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the demo accounts used to exercise the login flow (Order 300). Runs after
/// <see cref="RoleSeeder"/> so the roles it assigns already exist.
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

    /// <summary>Fixed department id for seeded accounts (no department rows are required).</summary>
    private static readonly Guid SeedDepartmentId = Guid.Parse("11111111-2222-3333-4444-555555555555");

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

        await EnsureUserAsync(context, DemoUsername, adminRole, scopeType: "SYSTEM", cancellationToken);
        await EnsureUserAsync(context, DemoStandardUsername, userRole, scopeType: "DEPARTMENT", cancellationToken);
    }

    private static async Task EnsureUserAsync(
        SeederContext context,
        string username,
        Role role,
        string scopeType,
        CancellationToken cancellationToken)
    {
        var db = context.Db;
        var normalized = username.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Username == normalized, cancellationToken))
            return;

        var passwordHash = context.PasswordHasher.Hash(DemoPassword);
        var userResult = User.Create(
            normalized,
            passwordHash,
            primaryDepartmentId: SeedDepartmentId,
            scopeType: scopeType,
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
