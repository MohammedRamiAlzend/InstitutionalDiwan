using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class CancelDocumentCommandHandler
    : ICommandHandler<CancelDocumentCommand, DocumentResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentMovementRepository _documentMovementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentMovementRepository documentMovementRepository,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _documentMovementRepository = documentMovementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentResponse>> Handle(
        CancelDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentNotFound);

        var hasActiveMovements = await _documentMovementRepository.HasActiveMovementsAsync(
            document.DocumentId, cancellationToken);

        var cancelResult = document.Cancel(
            request.UserId, hasActiveMovements, request.Reason, utcNow: _unitOfWork.UtcNow);
        if (cancelResult.IsFailure)
            return Result.Failure<DocumentResponse>(cancelResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(document);
    }
}
