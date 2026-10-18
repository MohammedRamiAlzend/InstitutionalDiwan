using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

/// <summary>Updates a role's display names and description. The code is immutable.</summary>
public sealed record UpdateRoleCommand(
    Guid RoleId,
    string NameAr,
    string? NameEn = null,
    string? Description = null) : ICommand<RoleResponse>;
