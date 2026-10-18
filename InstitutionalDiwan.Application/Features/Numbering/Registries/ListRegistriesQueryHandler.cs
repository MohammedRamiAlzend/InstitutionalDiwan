using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

internal sealed class ListRegistriesQueryHandler
    : IQueryHandler<ListRegistriesQuery, IReadOnlyCollection<NumberingRegistryResponse>>
{
    private readonly INumberingRegistryRepository _numberingRegistryRepository;

    public ListRegistriesQueryHandler(INumberingRegistryRepository numberingRegistryRepository) =>
        _numberingRegistryRepository = numberingRegistryRepository;

    public async Task<Result<IReadOnlyCollection<NumberingRegistryResponse>>> Handle(
        ListRegistriesQuery request,
        CancellationToken cancellationToken)
    {
        var registries = await _numberingRegistryRepository.ListAllAsync(cancellationToken);
        IReadOnlyCollection<NumberingRegistryResponse> responses =
            registries.Select(NumberingRegistryResponse.From).ToList();
        return Result.Success(responses);
    }
}
