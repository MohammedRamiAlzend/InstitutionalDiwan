using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

internal sealed class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand, RoleResponse>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RoleResponse>> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure<RoleResponse>(DomainErrors.Identity.RoleNotFound);

        var updateResult = role.UpdateDetails(request.NameAr, request.NameEn, request.Description);
        if (updateResult.IsFailure)
            return Result.Failure<RoleResponse>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RoleResponse.From(role);
    }
}
