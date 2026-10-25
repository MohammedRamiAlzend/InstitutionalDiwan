using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

internal sealed class MoveDepartmentCommandHandler
    : ICommandHandler<MoveDepartmentCommand, DepartmentResponse>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MoveDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DepartmentResponse>> Handle(
        MoveDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
            return Result.Failure<DepartmentResponse>(DomainErrors.Department.NotFound);

        if (department.ParentDepartmentId == request.NewParentDepartmentId)
            return DepartmentResponse.From(department);

        var newParentIsActive = true;
        IReadOnlySet<Guid>? descendantIds = null;

        if (request.NewParentDepartmentId is { } newParentId)
        {
            var parent = await _departmentRepository.GetByIdAsync(newParentId, cancellationToken);
            if (parent is null)
                return Result.Failure<DepartmentResponse>(DomainErrors.Department.ParentNotFound);

            newParentIsActive = parent.IsActive;

            // The whole subtree below this department is off-limits as a new parent; passing it lets
            // MoveToParent reject a move that would introduce a hierarchy cycle.
            descendantIds = await _departmentRepository.GetDescendantIdsAsync(
                department.DepartmentId, cancellationToken);
        }

        var moveResult = department.MoveToParent(
            request.NewParentDepartmentId, descendantIds, newParentIsActive, _unitOfWork.UtcNow);
        if (moveResult.IsFailure)
            return Result.Failure<DepartmentResponse>(moveResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DepartmentResponse.From(department);
    }
}
