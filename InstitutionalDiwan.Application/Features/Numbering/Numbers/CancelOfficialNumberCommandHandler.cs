using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

internal sealed class CancelOfficialNumberCommandHandler
    : ICommandHandler<CancelOfficialNumberCommand, DocumentNumberResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOfficialNumberCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentNumberResponse>> Handle(
        CancelOfficialNumberCommand request,
        CancellationToken cancellationToken)
    {
        var number = await _documentNumberRepository.GetByIdAsync(
            request.DocumentNumberId, cancellationToken);

        if (number is null || number.DocumentId != request.DocumentId)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.NumberNotFound);

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.DownloadId, cancellationToken))
        {
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var notArchived = document.EnsureNotArchived();
        if (notArchived.IsFailure)
            return Result.Failure<DocumentNumberResponse>(notArchived.Error);

        if (number.StatusId != DocumentNumberStatus.ActiveId)
            return Result.Failure<DocumentNumberResponse>(DomainErrors.Numbering.NumberNotActive);

        var cancelResult = number.Cancel(request.UserId, request.Reason, _unitOfWork.UtcNow);
        if (cancelResult.IsFailure)
            return Result.Failure<DocumentNumberResponse>(cancelResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentNumberResponse.From(number);
    }
}
