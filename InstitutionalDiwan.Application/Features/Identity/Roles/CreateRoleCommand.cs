using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

/// <summary>Creates a new (non-system) role.</summary>
public sealed record CreateRoleCommand(
    string Code,
    string NameAr,
    string? NameEn = null,
    string? Description = null) : ICommand<RoleResponse>;
