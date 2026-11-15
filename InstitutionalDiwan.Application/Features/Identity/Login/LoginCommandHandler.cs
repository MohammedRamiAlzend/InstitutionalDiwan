using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Login;

internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoginAttemptTracker _loginAttemptTracker;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        IUnitOfWork unitOfWork,
        ILoginAttemptTracker loginAttemptTracker)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _unitOfWork = unitOfWork;
        _loginAttemptTracker = loginAttemptTracker;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        if (await _loginAttemptTracker.IsLockedOutAsync(username, cancellationToken))
            return Result.Failure<LoginResponse>(DomainErrors.Identity.LoginThrottled);

        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);

        // Same error for "unknown user" and "wrong password" so the endpoint cannot be used to
        // enumerate accounts; an unknown username still burns one PBKDF2 verification so the
        // response timing cannot tell the two cases apart either.
        if (user is null)
        {
            _passwordHasher.Verify(request.Password, _passwordHasher.DummyHash);
            if (await _loginAttemptTracker.RegisterFailureAsync(username, cancellationToken))
                return Result.Failure<LoginResponse>(DomainErrors.Identity.LoginThrottled);

            return Result.Failure<LoginResponse>(DomainErrors.Identity.InvalidCredentials);
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (await _loginAttemptTracker.RegisterFailureAsync(username, cancellationToken))
                return Result.Failure<LoginResponse>(DomainErrors.Identity.LoginThrottled);

            return Result.Failure<LoginResponse>(DomainErrors.Identity.InvalidCredentials);
        }

        if (!user.IsActive)
            return Result.Failure<LoginResponse>(DomainErrors.Identity.UserInactive);

        // Upgrade hashes stored under an older work factor. Forced-change accounts skip this —
        // they rehash at their own work factor when they change the password.
        if (!user.MustChangePassword && _passwordHasher.NeedsRehash(user.PasswordHash))
        {
            var upgrade = user.UpgradePasswordHash(_passwordHasher.Hash(request.Password));
            if (upgrade.IsFailure)
                return Result.Failure<LoginResponse>(upgrade.Error);
        }

        await _loginAttemptTracker.ResetAsync(username, cancellationToken);
        user.RecordLogin(_unitOfWork.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await IdentityTokenIssuer.IssueAsync(user, _roleRepository, _jwtProvider, cancellationToken);
    }
}
