using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

internal sealed class CreateSavedSearchCommandHandler
    : ICommandHandler<CreateSavedSearchCommand, SavedSearchResponse>
{
    private readonly ISavedSearchRepository _savedSearchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSavedSearchCommandHandler(
        ISavedSearchRepository savedSearchRepository,
        IUnitOfWork unitOfWork)
    {
        _savedSearchRepository = savedSearchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SavedSearchResponse>> Handle(
        CreateSavedSearchCommand request,
        CancellationToken cancellationToken)
    {
        if (!ArchiveSearchCriteria.IsValid(request.CriteriaJson))
            return Result.Failure<SavedSearchResponse>(DomainErrors.Archiving.CriteriaInvalid);

        var count = await _savedSearchRepository.CountByUserAsync(request.UserId, cancellationToken);
        if (count >= SavedSearch.MaxPerUser)
            return Result.Failure<SavedSearchResponse>(DomainErrors.Archiving.SearchLimitReached);

        if (await _savedSearchRepository.ExistsByNameAsync(
                request.UserId, request.Name, cancellationToken: cancellationToken))
        {
            return Result.Failure<SavedSearchResponse>(DomainErrors.Archiving.SearchNameTaken);
        }

        var savedSearchResult = SavedSearch.Create(
            request.UserId, request.Name, request.CriteriaJson, request.IsDefault, _unitOfWork.UtcNow);
        if (savedSearchResult.IsFailure)
            return Result.Failure<SavedSearchResponse>(savedSearchResult.Error);

        if (request.IsDefault)
        {
            await SavedSearchDefaults.ClearCurrentAsync(
                _savedSearchRepository, _unitOfWork, request.UserId, keepSavedSearchId: null, cancellationToken);
        }

        _savedSearchRepository.Add(savedSearchResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SavedSearchResponse.From(savedSearchResult.Value);
    }
}
