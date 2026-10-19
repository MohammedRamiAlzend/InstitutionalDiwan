using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Lookups;

internal sealed class GetLookupEntryByIdQueryHandler
    : IQueryHandler<GetLookupEntryByIdQuery, LookupEntryResponse>
{
    private readonly ILookupRepository _lookupRepository;

    public GetLookupEntryByIdQueryHandler(ILookupRepository lookupRepository) =>
        _lookupRepository = lookupRepository;

    public async Task<Result<LookupEntryResponse>> Handle(
        GetLookupEntryByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!_lookupRepository.IsSupported(request.Lookup))
            return Result.Failure<LookupEntryResponse>(DomainErrors.Lookup.UnknownCatalog(request.Lookup));

        var entry = await _lookupRepository.GetByIdAsync(request.Lookup, request.Id, cancellationToken);
        if (entry is null)
            return Result.Failure<LookupEntryResponse>(
                DomainErrors.Lookup.EntryNotFound(request.Lookup, request.Id));

        return LookupEntryResponse.From(entry);
    }
}
