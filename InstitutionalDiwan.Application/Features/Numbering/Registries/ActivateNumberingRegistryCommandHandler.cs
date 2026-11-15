using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

internal sealed class ActivateNumberingRegistryCommandHandler
    : ICommandHandler<ActivateNumberingRegistryCommand, NumberingRegistryResponse>
{
    private readonly INumberingRegistryRepository _registryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;

    public ActivateNumberingRegistryCommandHandler(
        INumberingRegistryRepository registryRepository,
        IUnitOfWork unitOfWork,
        IDepartmentScopeEvaluator departmentScopeEvaluator)
    {
        _registryRepository = registryRepository;
        _unitOfWork = unitOfWork;
        _departmentScopeEvaluator = departmentScopeEvaluator;
    }

    public async Task<Result<NumberingRegistryResponse>> Handle(
        ActivateNumberingRegistryCommand request,
        CancellationToken cancellationToken)
    {
        var registry = await _registryRepository.GetByIdAsync(request.RegistryId, cancellationToken);
        if (registry is null)
            return Result.Failure<NumberingRegistryResponse>(DomainErrors.Numbering.RegistryNotFound);

        var accessible = await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(cancellationToken);
        var scopeError = RegistryScopeGuard.CheckModify(registry.DepartmentId, accessible);
        if (scopeError is not null)
            return Result.Failure<NumberingRegistryResponse>(scopeError);

        var result = registry.Activate();
        if (result.IsFailure)
            return Result.Failure<NumberingRegistryResponse>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NumberingRegistryResponse.From(registry);
    }
}
