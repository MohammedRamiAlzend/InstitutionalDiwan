using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

internal sealed class GrantRolePermissionCommandHandler
    : ICommandHandler<GrantRolePermissionCommand, RoleResponse>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GrantRolePermissionCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RoleResponse>> Handle(
        GrantRolePermissionCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure<RoleResponse>(DomainErrors.Identity.RoleNotFound);

        var permission = await _permissionRepository.GetByCodeAsync(request.PermissionCode, cancellationToken);
        if (permission is null)
            return Result.Failure<RoleResponse>(DomainErrors.Identity.PermissionNotFound);

        var grantResult = role.GrantPermission(permission.Code);
        if (grantResult.IsFailure)
            return Result.Failure<RoleResponse>(grantResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RoleResponse.From(role);
    }
}
