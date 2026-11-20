using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Lists the explicit grants on an item. Manage only.</summary>
public sealed record ListDriveItemPermissionsQuery(Guid ItemId) : IQuery<IReadOnlyList<DrivePermissionResponse>>;

/// <summary>The caller's effective role on an item (0 = no access).</summary>
public sealed record GetDriveEffectiveRoleQuery(Guid ItemId) : IQuery<DriveEffectiveRoleResponse>;

/// <summary>The caller's effective Drive role id on an item.</summary>
public sealed record DriveEffectiveRoleResponse(int RoleId);

internal sealed class ListDriveItemPermissionsQueryHandler : IQueryHandler<ListDriveItemPermissionsQuery, IReadOnlyList<DrivePermissionResponse>>
{
    private readonly IDriveItemRepository _items;
    private readonly IDrivePermissionRepository _permissions;
    private readonly IDriveAccessEvaluator _access;

    public ListDriveItemPermissionsQueryHandler(
        IDriveItemRepository items,
        IDrivePermissionRepository permissions,
        IDriveAccessEvaluator access)
    {
        _items = items;
        _permissions = permissions;
        _access = access;
    }

    public async Task<Result<IReadOnlyList<DrivePermissionResponse>>> Handle(ListDriveItemPermissionsQuery request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<IReadOnlyList<DrivePermissionResponse>>(DomainErrors.Drive.ItemNotFound);
        if (!await _access.CanAccessItemAsync(request.ItemId, DriveRole.ManagerId, cancellationToken))
            return Result.Failure<IReadOnlyList<DrivePermissionResponse>>(DomainErrors.Drive.AccessDenied);

        var grants = await _permissions.ListByItemAsync(request.ItemId, cancellationToken);
        return Result.Success((IReadOnlyList<DrivePermissionResponse>)grants
            .Select(DrivePermissionResponse.From).ToList());
    }
}

internal sealed class GetDriveEffectiveRoleQueryHandler : IQueryHandler<GetDriveEffectiveRoleQuery, DriveEffectiveRoleResponse>
{
    private readonly IDriveAccessEvaluator _access;

    public GetDriveEffectiveRoleQueryHandler(IDriveAccessEvaluator access) => _access = access;

    public async Task<Result<DriveEffectiveRoleResponse>> Handle(GetDriveEffectiveRoleQuery request, CancellationToken cancellationToken)
    {
        var roleId = await _access.GetEffectiveRoleAsync(request.ItemId, cancellationToken);
        return Result.Success(new DriveEffectiveRoleResponse(roleId));
    }
}