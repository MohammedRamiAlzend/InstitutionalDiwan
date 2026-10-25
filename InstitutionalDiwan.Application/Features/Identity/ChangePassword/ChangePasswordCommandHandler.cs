using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Identity.Login;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.ChangePassword;

internal sealed class ChangePasswordCommandHandler
    : ICommandHandler<ChangePasswordCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponse>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<LoginResponse>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<LoginResponse>(DomainErrors.Identity.UserInactive);

        if (!_passwordHasher.Verify(request.OldPassword, user.PasswordHash))
            return Result.Failure<LoginResponse>(DomainErrors.Identity.CurrentPasswordIncorrect);

        var changeResult = user.ChangePasswordHash(
            _passwordHasher.Hash(request.NewPassword), _unitOfWork.UtcNow);
        if (changeResult.IsFailure)
            return Result.Failure<LoginResponse>(changeResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await IdentityTokenIssuer.IssueAsync(user, _roleRepository, _jwtProvider, cancellationToken);
    }
}
