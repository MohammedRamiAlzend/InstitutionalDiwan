using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Application.Features.DepartmentRecords;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving;

internal sealed class ArchiveDocumentCommandHandler
    : ICommandHandler<ArchiveDocumentCommand, DocumentResponse>
{
    private const string ArchiveLocationsLookup = "archive-locations";

    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentMovementRepository _documentMovementRepository;
    private readonly IDocumentArchiveRepository _documentArchiveRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly DepartmentRecordService _recordService;
    private readonly IInstitutionSettingRepository _settingRepository;

    public ArchiveDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentMovementRepository documentMovementRepository,
        IDocumentArchiveRepository documentArchiveRepository,
        ILookupRepository lookupRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUnitOfWork unitOfWork,
        INotificationPublisher notificationPublisher,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        DepartmentRecordService recordService,
        IInstitutionSettingRepository settingRepository)
    {
        _settingRepository = settingRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _recordService = recordService;
        _documentRepository = documentRepository;
        _documentMovementRepository = documentMovementRepository;
        _documentArchiveRepository = documentArchiveRepository;
        _lookupRepository = lookupRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _unitOfWork = unitOfWork;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<DocumentResponse>> Handle(
        ArchiveDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentNotFound);

        // With an archive diwan designated, archiving happens there and only there: custody of the
        // document replaces the MANAGE rule and the owning-department rule.
        var custody = await ArchiveDiwanCustody.ResolveAsync(
            document.DocumentId, _settingRepository, _documentMovementRepository, cancellationToken);
        if (custody.ArchiveDiwanDepartmentId is { } archiveDiwanDepartmentId)
        {
            if (!custody.IsHeld)
                return Result.Failure<DocumentResponse>(DomainErrors.Archiving.DocumentNotAtArchiveDiwan);

            if (!await _departmentScopeEvaluator.CanAccessDepartmentAsync(archiveDiwanDepartmentId, cancellationToken))
                return Result.Failure<DocumentResponse>(DomainErrors.Archiving.OnlyArchiveDiwanCanArchive);
        }
        else
        {
            if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                    request.DocumentId, AccessLevel.ManageId, cancellationToken))
            {
                return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
            }

            var owner = await OwningDepartmentGuard.EnsureOwningDepartmentAsync(document, _departmentScopeEvaluator, cancellationToken);
            if (owner.IsFailure)
                return Result.Failure<DocumentResponse>(owner.Error);
        }

        var location = await _lookupRepository.GetByIdAsync(
            ArchiveLocationsLookup, request.ArchiveLocationId, cancellationToken);
        if (location is null || !location.IsActive)
            return Result.Failure<DocumentResponse>(DomainErrors.Archiving.LocationNotFound);

        var utcNow = _unitOfWork.UtcNow;
        bool hasActiveMovements;
        if (custody.IsHeld)
        {
            // The movement that brought the document ends here, in the same save as the archiving.
            var arrival = custody.LatestMovement!;
            if (arrival.StatusId != DocumentMovement.StatusClosed)
            {
                var close = arrival.Close(request.UserId, "Archived by the archive diwan", utcNow);
                if (close.IsFailure)
                    return Result.Failure<DocumentResponse>(close.Error);

                var released = document.MarkMovementEnded(request.UserId, "Archived by the archive diwan", utcNow);
                if (released.IsFailure)
                    return Result.Failure<DocumentResponse>(released.Error);
            }

            hasActiveMovements = false;
        }
        else
        {
            hasActiveMovements = await _documentMovementRepository.HasActiveMovementsAsync(
                document.DocumentId, cancellationToken);
        }

        var previousStatusId = document.CurrentStatusId;

        var archiveResult = document.Archive(
            request.UserId, hasActiveMovements, request.ArchiveLocationId, request.Reason, utcNow);
        if (archiveResult.IsFailure)
            return Result.Failure<DocumentResponse>(archiveResult.Error);

        var eventResult = DocumentArchiveEvent.Record(
            document.DocumentId,
            ArchiveEventType.ArchivedId,
            previousStatusId,
            document.CurrentStatusId,
            request.UserId,
            request.Reason,
            request.ArchiveLocationId,
            utcNow);
        if (eventResult.IsFailure)
            return Result.Failure<DocumentResponse>(eventResult.Error);

        _documentArchiveRepository.Add(eventResult.Value);
        await _recordService.LockAllAsync(document.DocumentId, utcNow, cancellationToken);
        await _notificationPublisher.PublishDocumentAsync(NotificationType.DocumentArchivedId, document, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(document);
    }
}
