using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving;

internal sealed class UnarchiveDocumentCommandHandler
    : ICommandHandler<UnarchiveDocumentCommand, DocumentResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentArchiveRepository _documentArchiveRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;

    public UnarchiveDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentArchiveRepository documentArchiveRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUnitOfWork unitOfWork,
        IDepartmentScopeEvaluator departmentScopeEvaluator)
    {
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _documentRepository = documentRepository;
        _documentArchiveRepository = documentArchiveRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentResponse>> Handle(
        UnarchiveDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ManageId, cancellationToken))
        {
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var owner = await OwningDepartmentGuard.EnsureOwningDepartmentAsync(document, _departmentScopeEvaluator, cancellationToken);
        if (owner.IsFailure)
            return Result.Failure<DocumentResponse>(owner.Error);

        var previousStatusId = document.CurrentStatusId;
        var utcNow = _unitOfWork.UtcNow;

        var unarchiveResult = document.Unarchive(request.UserId, request.Reason, utcNow);
        if (unarchiveResult.IsFailure)
            return Result.Failure<DocumentResponse>(unarchiveResult.Error);

        var eventResult = DocumentArchiveEvent.Record(
            document.DocumentId,
            ArchiveEventType.UnarchivedId,
            previousStatusId,
            document.CurrentStatusId,
            request.UserId,
            request.Reason,
            utcNow: utcNow);
        if (eventResult.IsFailure)
            return Result.Failure<DocumentResponse>(eventResult.Error);

        _documentArchiveRepository.Add(eventResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(document);
    }
}
