using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Lookups;

internal sealed class ListLookupEntriesQueryHandler
    : IQueryHandler<ListLookupEntriesQuery, IReadOnlyCollection<LookupEntryResponse>>
{
    private readonly ILookupRepository _lookupRepository;

    public ListLookupEntriesQueryHandler(ILookupRepository lookupRepository) =>
        _lookupRepository = lookupRepository;

    public async Task<Result<IReadOnlyCollection<LookupEntryResponse>>> Handle(
        ListLookupEntriesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_lookupRepository.IsSupported(request.Lookup))
            return Result.Failure<IReadOnlyCollection<LookupEntryResponse>>(
                DomainErrors.Lookup.UnknownCatalog(request.Lookup));

        var entries = await _lookupRepository.ListAsync(
            request.Lookup, request.IncludeInactive, cancellationToken);

        IReadOnlyCollection<LookupEntryResponse> responses =
            entries.Select(LookupEntryResponse.From).ToList();

        return Result.Success(responses);
    }
}
