using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

internal sealed class AssignUserRoleCommandHandler : ICommandHandler<AssignUserRoleCommand, UserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignUserRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserResponse>> Handle(
        AssignUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserResponse>(DomainErrors.Identity.UserNotFound);

        if (await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken) is null)
            return Result.Failure<UserResponse>(DomainErrors.Identity.RoleNotFound);

        var assignResult = user.AssignRole(request.RoleId, request.AssignedByUserId);
        if (assignResult.IsFailure)
            return Result.Failure<UserResponse>(assignResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await UserResponseFactory.CreateAsync(user, _roleRepository, cancellationToken);
    }
}
