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

    public LoginCommandHandler(
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

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);

        // Same error for "unknown user" and "wrong password" so the endpoint cannot be used to
        // enumerate accounts.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<LoginResponse>(DomainErrors.Identity.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<LoginResponse>(DomainErrors.Identity.UserInactive);

        user.RecordLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await IdentityTokenIssuer.IssueAsync(user, _roleRepository, _jwtProvider, cancellationToken);
    }
}
