using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

internal sealed class ListSavedSearchesQueryHandler
    : IQueryHandler<ListSavedSearchesQuery, IReadOnlyCollection<SavedSearchResponse>>
{
    private readonly ISavedSearchRepository _savedSearchRepository;

    public ListSavedSearchesQueryHandler(ISavedSearchRepository savedSearchRepository) =>
        _savedSearchRepository = savedSearchRepository;

    public async Task<Result<IReadOnlyCollection<SavedSearchResponse>>> Handle(
        ListSavedSearchesQuery request,
        CancellationToken cancellationToken)
    {
        var searches = await _savedSearchRepository.ListByUserAsync(request.UserId, cancellationToken);

        IReadOnlyCollection<SavedSearchResponse> responses =
            searches.Select(SavedSearchResponse.From).ToList();

        return Result.Success(responses);
    }
}
