using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Permissions;

/// <summary>Lists the permission catalog so a client can pick codes to grant to a role.</summary>
public sealed record ListPermissionsQuery : IQuery<IReadOnlyCollection<PermissionResponse>>;
