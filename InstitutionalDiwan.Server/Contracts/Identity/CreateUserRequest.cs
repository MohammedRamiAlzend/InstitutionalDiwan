using System.ComponentModel;

namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>
/// Payload submitted to <c>POST /api/users</c> to create a new user account.
/// <c>ScopeTypeId</c> defaults to 1 (<c>UserScopeType.Department</c>); roles are granted separately.
/// </summary>
/// <param name="Username">Unique username for login (string). Example: <c>ahmed.khalil</c></param>
/// <param name="Password">Account password (string). Must meet password complexity requirements. Example: <c>TempP@ss123!</c></param>
/// <param name="PrimaryDepartmentId">GUID of the user's primary department. Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c></param>
/// <param name="EmployeeId">Optional GUID linking the user to an employee record. Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c></param>
/// <param name="ScopeTypeId">User scope type identifier (int). Default is 1 (Department scope). Example: <c>1</c></param>
/// <param name="MustChangePassword">Whether the user must change password on next login (bool). Default is <c>true</c>. Example: <c>true</c></param>
public sealed record CreateUserRequest(
    [property: Description("Unique username for login. Example: ahmed.khalil")] string Username,
    [property: Description("Account password. Must meet password complexity requirements. Example: TempP@ss123!")] string Password,
    [property: Description("GUID of the user's primary department. Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42")] Guid PrimaryDepartmentId,
    [property: Description("Optional GUID linking the user to an employee record. Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921")] Guid? EmployeeId = null,
    [property: Description("User scope type identifier. Default is 1 (Department scope). Example: 1")] int ScopeTypeId = 1,
    [property: Description("Whether the user must change password on next login. Default is true. Example: true")] bool MustChangePassword = true);
