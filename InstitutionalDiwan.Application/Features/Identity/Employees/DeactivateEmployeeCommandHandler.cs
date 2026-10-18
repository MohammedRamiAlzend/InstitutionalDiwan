using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

internal sealed class DeactivateEmployeeCommandHandler
    : ICommandHandler<DeactivateEmployeeCommand, EmployeeResponse>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EmployeeResponse>> Handle(
        DeactivateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<EmployeeResponse>(DomainErrors.Identity.EmployeeNotFound);

        var result = employee.Deactivate();
        if (result.IsFailure)
            return Result.Failure<EmployeeResponse>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return EmployeeResponse.From(employee);
    }
}
