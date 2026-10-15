using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
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

        var roles = new List<Role>();
        foreach (var userRole in user.UserRoles)
        {
            var role = await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
            if (role is not null)
                roles.Add(role);
        }

        var roleCodes = roles.Select(role => role.Code).Distinct().ToList();
        var permissions = roles
            .SelectMany(role => role.RolePermissions)
            .Select(permission => permission.PermissionCode)
            .Distinct()
            .ToList();

        user.RecordLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtProvider.GenerateAccessToken(user, roleCodes, permissions);

        return new LoginResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            user.UserId,
            user.Username,
            user.MustChangePassword,
            roleCodes,
            permissions);
    }
}
