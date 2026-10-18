namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>POST /api/users/{userId}/roles</c>.</summary>
public sealed record AssignUserRoleRequest(Guid RoleId);
