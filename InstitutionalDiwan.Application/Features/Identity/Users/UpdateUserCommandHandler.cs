using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

internal sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, UserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILookupRepository _lookupRepository;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ILookupRepository lookupRepository)
    {
        _lookupRepository = lookupRepository;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserResponse>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserResponse>(DomainErrors.Identity.UserNotFound);

        if (await _departmentRepository.GetByIdAsync(request.PrimaryDepartmentId, cancellationToken) is null)
            return Result.Failure<UserResponse>(DomainErrors.Department.NotFound);

        if (request.EmployeeId is { } employeeId &&
            await _employeeRepository.GetByIdAsync(employeeId, cancellationToken) is null)
            return Result.Failure<UserResponse>(DomainErrors.Identity.EmployeeNotFound);

        var scope = await _lookupRepository.GetByIdAsync("user-scope-types", request.ScopeTypeId, cancellationToken);
        if (request.ScopeTypeId is < 1 or > 4 || scope is not { IsActive: true })
            return Result.Failure<UserResponse>(Error.Validation("Identity.UserScopeTypeInvalid", "Select an active supported user scope type."));

        var updateResult = user.UpdateProfile(
            request.PrimaryDepartmentId,
            request.EmployeeId,
            request.ScopeTypeId,
            request.MustChangePassword,
            utcNow: _unitOfWork.UtcNow);

        if (updateResult.IsFailure)
            return Result.Failure<UserResponse>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await UserResponseFactory.CreateAsync(user, _roleRepository, cancellationToken);
    }
}
