using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving.SavedSearches;

internal sealed class DeleteSavedSearchCommandHandler : ICommandHandler<DeleteSavedSearchCommand>
{
    private readonly ISavedSearchRepository _savedSearchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSavedSearchCommandHandler(
        ISavedSearchRepository savedSearchRepository,
        IUnitOfWork unitOfWork)
    {
        _savedSearchRepository = savedSearchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteSavedSearchCommand request, CancellationToken cancellationToken)
    {
        var savedSearch = await _savedSearchRepository.GetByIdAsync(request.SavedSearchId, cancellationToken);
        if (savedSearch is null || savedSearch.UserId != request.UserId)
            return Result.Failure(DomainErrors.Archiving.SearchNotFound);

        _savedSearchRepository.Remove(savedSearch);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
