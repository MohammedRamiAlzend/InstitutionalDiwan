using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Register;

internal sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, RegisterResponse>
{
    /// <summary>Role assigned to every self-registered account (never a privileged role).</summary>
    private const string DefaultRoleCode = "USER";

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
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

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        if (await _userRepository.GetByUsernameAsync(username, cancellationToken) is not null)
            return Result.Failure<RegisterResponse>(DomainErrors.Identity.UsernameTaken);

        var role = await _roleRepository.GetByCodeAsync(DefaultRoleCode, cancellationToken);
        if (role is null)
            return Result.Failure<RegisterResponse>(DomainErrors.Identity.RoleNotFound);

        var passwordHash = _passwordHasher.Hash(request.Password);
        var userResult = User.Create(
            username,
            passwordHash,
            primaryDepartmentId: Guid.Empty, // self-registered accounts have no department yet
            scopeTypeId: UserScopeType.DepartmentId,
            mustChangePassword: false);

        if (userResult.IsFailure)
            return Result.Failure<RegisterResponse>(userResult.Error);

        var user = userResult.Value;

        var assignResult = user.AssignRole(role.RoleId);
        if (assignResult.IsFailure)
            return Result.Failure<RegisterResponse>(assignResult.Error);

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(
            user.UserId,
            user.Username,
            user.MustChangePassword,
            new[] { role.Code });
    }
}
