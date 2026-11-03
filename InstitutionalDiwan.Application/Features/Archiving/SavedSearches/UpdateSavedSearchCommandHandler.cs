using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

internal sealed class UpdateSavedSearchCommandHandler
    : ICommandHandler<UpdateSavedSearchCommand, SavedSearchResponse>
{
    private readonly ISavedSearchRepository _savedSearchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSavedSearchCommandHandler(
        ISavedSearchRepository savedSearchRepository,
        IUnitOfWork unitOfWork)
    {
        _savedSearchRepository = savedSearchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SavedSearchResponse>> Handle(
        UpdateSavedSearchCommand request,
        CancellationToken cancellationToken)
    {
        var savedSearch = await _savedSearchRepository.GetByIdAsync(request.SavedSearchId, cancellationToken);
        if (savedSearch is null || savedSearch.UserId != request.UserId)
            return Result.Failure<SavedSearchResponse>(DomainErrors.Archiving.SearchNotFound);

        if (!ArchiveSearchCriteria.IsValid(request.CriteriaJson))
            return Result.Failure<SavedSearchResponse>(DomainErrors.Archiving.CriteriaInvalid);

        if (await _savedSearchRepository.ExistsByNameAsync(
                request.UserId, request.Name, request.SavedSearchId, cancellationToken))
        {
            return Result.Failure<SavedSearchResponse>(DomainErrors.Archiving.SearchNameTaken);
        }

        if (request.IsDefault)
        {
            await SavedSearchDefaults.ClearCurrentAsync(
                _savedSearchRepository, _unitOfWork, request.UserId, request.SavedSearchId, cancellationToken);
        }

        var updateResult = savedSearch.UpdateCriteria(
            request.Name, request.CriteriaJson, request.IsDefault, _unitOfWork.UtcNow);
        if (updateResult.IsFailure)
            return Result.Failure<SavedSearchResponse>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SavedSearchResponse.From(savedSearch);
    }
}
