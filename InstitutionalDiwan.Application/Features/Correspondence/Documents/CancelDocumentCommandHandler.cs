using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.DepartmentRecords;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class CancelDocumentCommandHandler
    : ICommandHandler<CancelDocumentCommand, DocumentResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentMovementRepository _documentMovementRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly DepartmentRecordService _recordService;

    public CancelDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentMovementRepository documentMovementRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUnitOfWork unitOfWork,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        DepartmentRecordService recordService)
    {
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _recordService = recordService;
        _documentAccessEvaluator = documentAccessEvaluator;
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

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ManageId, cancellationToken))
        {
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var owner = await OwningDepartmentGuard.EnsureOwningDepartmentAsync(document, _departmentScopeEvaluator, cancellationToken);
        if (owner.IsFailure)
            return Result.Failure<DocumentResponse>(owner.Error);

        var hasActiveMovements = await _documentMovementRepository.HasActiveMovementsAsync(
            document.DocumentId, cancellationToken);

        var cancelResult = document.Cancel(
            request.UserId, hasActiveMovements, request.Reason, utcNow: _unitOfWork.UtcNow);
        if (cancelResult.IsFailure)
            return Result.Failure<DocumentResponse>(cancelResult.Error);

        await _recordService.LockAllAsync(document.DocumentId, _unitOfWork.UtcNow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(document);
    }
}
