using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Permissions;

internal sealed class ListPermissionsQueryHandler
    : IQueryHandler<ListPermissionsQuery, IReadOnlyCollection<PermissionResponse>>
{
    private readonly IPermissionRepository _permissionRepository;

    public ListPermissionsQueryHandler(IPermissionRepository permissionRepository) =>
        _permissionRepository = permissionRepository;

    public async Task<Result<IReadOnlyCollection<PermissionResponse>>> Handle(
        ListPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.ListAllAsync(cancellationToken);
        IReadOnlyCollection<PermissionResponse> responses =
            permissions.Select(PermissionResponse.From).ToList();
        return Result.Success(responses);
    }
}
