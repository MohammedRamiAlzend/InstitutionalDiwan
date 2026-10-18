using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

internal sealed class GetRegistryByIdQueryHandler : IQueryHandler<GetRegistryByIdQuery, NumberingRegistryResponse>
{
    private readonly INumberingRegistryRepository _numberingRegistryRepository;

    public GetRegistryByIdQueryHandler(INumberingRegistryRepository numberingRegistryRepository) =>
        _numberingRegistryRepository = numberingRegistryRepository;

    public async Task<Result<NumberingRegistryResponse>> Handle(
        GetRegistryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var registry = await _numberingRegistryRepository.GetByIdAsync(request.RegistryId, cancellationToken);
        if (registry is null)
            return Result.Failure<NumberingRegistryResponse>(DomainErrors.Numbering.RegistryNotFound);

        return NumberingRegistryResponse.From(registry);
    }
}
