using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Archiving;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class GetDocumentDetailsQueryHandler
    : IQueryHandler<GetDocumentDetailsQuery, DocumentDetailResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IInstitutionSettingRepository? _settingRepository;
    private readonly IDocumentMovementRepository? _movementRepository;

    public GetDocumentDetailsQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IInstitutionSettingRepository? settingRepository = null,
        IDocumentMovementRepository? movementRepository = null)
    {
        _settingRepository = settingRepository;
        _movementRepository = movementRepository;
        _documentRepository = documentRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<DocumentDetailResponse>> Handle(
        GetDocumentDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentDetailResponse>(
                DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<DocumentDetailResponse>(
                DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var details = DocumentDetailResponse.From(document);
        if (_settingRepository is null || _movementRepository is null)
            return details;

        var custody = await ArchiveDiwanCustody.ResolveAsync(
            document.DocumentId, _settingRepository, _movementRepository, cancellationToken);
        return details with
        {
            ArchiveDiwanDepartmentId = custody.ArchiveDiwanDepartmentId,
            IsHeldByArchiveDiwan = custody.IsHeld
        };
    }
}
