using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

internal sealed class ListRegistriesQueryHandler
    : IQueryHandler<ListRegistriesQuery, IReadOnlyCollection<NumberingRegistryResponse>>
{
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;

    public ListRegistriesQueryHandler(
        INumberingRegistryRepository numberingRegistryRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator)
    {
        _numberingRegistryRepository = numberingRegistryRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
    }

    public async Task<Result<IReadOnlyCollection<NumberingRegistryResponse>>> Handle(
        ListRegistriesQuery request,
        CancellationToken cancellationToken)
    {
        var registries = await _numberingRegistryRepository.ListAllAsync(cancellationToken);
        var accessible = await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(cancellationToken);
        IReadOnlyCollection<NumberingRegistryResponse> responses =
            registries.Where(registry => RegistryScopeGuard.CanRead(registry.DepartmentId, accessible))
                .Select(NumberingRegistryResponse.From)
                .ToList();
        return Result.Success(responses);
    }
}
