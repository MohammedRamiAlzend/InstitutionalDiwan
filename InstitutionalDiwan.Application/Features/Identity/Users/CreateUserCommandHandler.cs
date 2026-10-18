using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

internal sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        if (await _userRepository.GetByUsernameAsync(username, cancellationToken) is not null)
            return Result.Failure<UserResponse>(DomainErrors.Identity.UsernameTaken);

        if (await _departmentRepository.GetByIdAsync(request.PrimaryDepartmentId, cancellationToken) is null)
            return Result.Failure<UserResponse>(DomainErrors.Department.NotFound);

        if (request.EmployeeId is { } employeeId &&
            await _employeeRepository.GetByIdAsync(employeeId, cancellationToken) is null)
            return Result.Failure<UserResponse>(DomainErrors.Identity.EmployeeNotFound);

        var passwordHash = _passwordHasher.Hash(request.Password);
        var userResult = User.Create(
            username,
            passwordHash,
            request.PrimaryDepartmentId,
            request.EmployeeId,
            request.ScopeTypeId,
            request.MustChangePassword);

        if (userResult.IsFailure)
            return Result.Failure<UserResponse>(userResult.Error);

        var user = userResult.Value;
        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await UserResponseFactory.CreateAsync(user, _roleRepository, cancellationToken);
    }
}
