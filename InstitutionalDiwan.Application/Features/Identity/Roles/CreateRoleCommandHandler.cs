using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

internal sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleResponse>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RoleResponse>> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        // Codes are stored upper-cased (see Role.Create), so normalize before the uniqueness check.
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _roleRepository.GetByCodeAsync(code, cancellationToken) is not null)
            return Result.Failure<RoleResponse>(DomainErrors.Identity.RoleCodeAlreadyExists);

        var roleResult = Role.Create(
            request.Code,
            request.NameAr,
            request.NameEn,
            request.Description,
            isSystemRole: false,
            utcNow: _unitOfWork.UtcNow);

        if (roleResult.IsFailure)
            return Result.Failure<RoleResponse>(roleResult.Error);

        var role = roleResult.Value;

        _roleRepository.Add(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RoleResponse.From(role);
    }
}
