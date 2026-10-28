using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

internal sealed class ResetUserPasswordCommandHandler
    : ICommandHandler<ResetUserPasswordCommand, UserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ResetUserPasswordCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserResponse>> Handle(
        ResetUserPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserResponse>(DomainErrors.Identity.UserNotFound);

        var resetResult = user.ResetPassword(
            _passwordHasher.Hash(request.NewPassword),
            request.ResetByUserId,
            _unitOfWork.UtcNow);

        if (resetResult.IsFailure)
            return Result.Failure<UserResponse>(resetResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await UserResponseFactory.CreateAsync(user, _roleRepository, cancellationToken);
    }
}
