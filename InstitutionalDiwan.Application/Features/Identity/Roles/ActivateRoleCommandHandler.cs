using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

internal sealed class ActivateRoleCommandHandler : ICommandHandler<ActivateRoleCommand, RoleResponse>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RoleResponse>> Handle(
        ActivateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure<RoleResponse>(DomainErrors.Identity.RoleNotFound);

        var activateResult = role.Activate();
        if (activateResult.IsFailure)
            return Result.Failure<RoleResponse>(activateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RoleResponse.From(role);
    }
}
