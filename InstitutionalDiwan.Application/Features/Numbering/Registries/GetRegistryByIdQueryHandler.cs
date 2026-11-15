using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

internal sealed class GetRegistryByIdQueryHandler : IQueryHandler<GetRegistryByIdQuery, NumberingRegistryResponse>
{
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;

    public GetRegistryByIdQueryHandler(
        INumberingRegistryRepository numberingRegistryRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator)
    {
        _numberingRegistryRepository = numberingRegistryRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
    }

    public async Task<Result<NumberingRegistryResponse>> Handle(
        GetRegistryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var registry = await _numberingRegistryRepository.GetByIdAsync(request.RegistryId, cancellationToken);
        if (registry is null)
            return Result.Failure<NumberingRegistryResponse>(DomainErrors.Numbering.RegistryNotFound);

        var accessible = await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(cancellationToken);
        if (!RegistryScopeGuard.CanRead(registry.DepartmentId, accessible))
            return Result.Failure<NumberingRegistryResponse>(DomainErrors.Numbering.RegistryNotFound);

        return NumberingRegistryResponse.From(registry);
    }
}
