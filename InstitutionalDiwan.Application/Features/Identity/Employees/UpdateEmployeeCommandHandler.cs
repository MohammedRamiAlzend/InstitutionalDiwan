using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

internal sealed class UpdateEmployeeCommandHandler
    : ICommandHandler<UpdateEmployeeCommand, EmployeeResponse>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EmployeeResponse>> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<EmployeeResponse>(DomainErrors.Identity.EmployeeNotFound);

        if (await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken) is null)
            return Result.Failure<EmployeeResponse>(DomainErrors.Department.NotFound);

        var updateResult = employee.Update(
            request.FullNameAr,
            request.DepartmentId,
            request.EmployeeNumber,
            request.FullNameEn,
            request.JobTitleAr,
            request.Email,
            request.Phone,
            utcNow: _unitOfWork.UtcNow);

        if (updateResult.IsFailure)
            return Result.Failure<EmployeeResponse>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return EmployeeResponse.From(employee);
    }
}
