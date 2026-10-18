using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

/// <summary>
/// Soft-deletes a role by deactivating it. System-defined roles are rejected with
/// <c>Identity.SystemRoleProtected</c> (403).
/// </summary>
public sealed record DeleteRoleCommand(Guid RoleId) : ICommand;
