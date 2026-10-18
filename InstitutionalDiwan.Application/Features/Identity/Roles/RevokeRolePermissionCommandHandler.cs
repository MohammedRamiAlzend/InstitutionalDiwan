using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

internal sealed class RevokeRolePermissionCommandHandler
    : ICommandHandler<RevokeRolePermissionCommand, RoleResponse>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeRolePermissionCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RoleResponse>> Handle(
        RevokeRolePermissionCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure<RoleResponse>(DomainErrors.Identity.RoleNotFound);

        var revokeResult = role.RevokePermission(request.PermissionCode);
        if (revokeResult.IsFailure)
            return Result.Failure<RoleResponse>(revokeResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RoleResponse.From(role);
    }
}
