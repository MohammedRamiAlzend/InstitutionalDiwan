using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

internal sealed class AssignUserDepartmentScopeCommandHandler
    : ICommandHandler<AssignUserDepartmentScopeCommand, UserResponse>
{
    private const string AssignmentTypesLookup = "assignment-types";

    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignUserDepartmentScopeCommandHandler(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        ILookupRepository lookupRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _lookupRepository = lookupRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserResponse>> Handle(
        AssignUserDepartmentScopeCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserResponse>(DomainErrors.Identity.UserNotFound);

        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
            return Result.Failure<UserResponse>(DomainErrors.Department.NotFound);

        if (!department.IsActive)
            return Result.Failure<UserResponse>(DomainErrors.Department.InactiveParent);

        if (request.AssignmentTypeId > 0)
        {
            var assignmentType = await _lookupRepository.GetByIdAsync(
                AssignmentTypesLookup, request.AssignmentTypeId, cancellationToken);

            if (assignmentType is null || !assignmentType.IsActive)
                return Result.Failure<UserResponse>(DomainErrors.Identity.AssignmentTypeInvalid);
        }

        var assignResult = user.AssignDepartmentScope(
            request.DepartmentId,
            request.AssignmentTypeId > 0 ? request.AssignmentTypeId : AssignmentType.AdditionalId,
            request.IncludeSubtree,
            request.StartDate,
            request.EndDate,
            _unitOfWork.UtcNow);

        if (assignResult.IsFailure)
            return Result.Failure<UserResponse>(assignResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await UserResponseFactory.CreateAsync(user, _roleRepository, cancellationToken);
    }
}
