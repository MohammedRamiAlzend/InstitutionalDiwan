using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

/// <summary>Reactivates a previously deactivated role.</summary>
public sealed record ActivateRoleCommand(Guid RoleId) : ICommand<RoleResponse>;
