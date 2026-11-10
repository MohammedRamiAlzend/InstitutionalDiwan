using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.DepartmentRecords;
using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class ReceiveDocumentCommandHandler
    : ICommandHandler<ReceiveDocumentCommand, MovementStepReceiptResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly DepartmentRegistrationService _registrationService;
    private readonly IClock _clock;
    private readonly DepartmentRecordService _recordService;

    public ReceiveDocumentCommandHandler(
        IDocumentMovementRepository movementRepository,
        IDocumentRepository documentRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUnitOfWork unitOfWork,
        INotificationPublisher notificationPublisher,
        DepartmentRegistrationService registrationService,
        IClock clock,
        DepartmentRecordService recordService)
    {
        _recordService = recordService;
        _movementRepository = movementRepository;
        _documentRepository = documentRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _unitOfWork = unitOfWork;
        _notificationPublisher = notificationPublisher;
        _registrationService = registrationService;
        _clock = clock;
    }

    public async Task<Result<MovementStepReceiptResponse>> Handle(
        ReceiveDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementStepReceiptResponse>(DomainErrors.Movements.MovementNotFound);

        var custody = await MovementCustody.EnsureReceiverAsync(
            movement, _departmentScopeEvaluator, cancellationToken);
        if (custody.IsFailure)
            return Result.Failure<MovementStepReceiptResponse>(custody.Error);

        var stepGuard = MovementStepGuard.EnsureCurrent(movement, request.StepId);
        if (stepGuard.IsFailure)
            return Result.Failure<MovementStepReceiptResponse>(stepGuard.Error);

        var document = await _documentRepository.GetWithNumbersAsync(movement.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<MovementStepReceiptResponse>(DomainErrors.Correspondence.DocumentNotFound);

        var step = movement.Steps.Single(current => current.StepNo == movement.CurrentStepNo);
        if (step.StepStatusId != MovementStepStatus.SentId)
            return Result.Failure<MovementStepReceiptResponse>(DomainErrors.Movements.InvalidStatusTransition);

        var registryDate = (request.RegistryDate ?? _clock.UtcNow).Date;
        var registered = await WriteRegisterVisitAsync(request, movement, step, document, registryDate, cancellationToken);
        if (registered.IsFailure)
            return Result.Failure<MovementStepReceiptResponse>(registered.Error);

        var (entry, visit) = registered.Value;
        var replaced = document.Numbers.FirstOrDefault(number => number.ReplacedByNumberId == entry.DocumentNumberId);
        var receive = movement.ReceiveStep(
            request.UserId, entry.DocumentNumberId, request.Notes, _unitOfWork.UtcNow, visit.VisitId);
        if (receive.IsFailure)
            return Result.Failure<MovementStepReceiptResponse>(receive.Error);

        // Nothing is saved yet, so a missing required field fails the whole receipt.
        var opened = await _recordService.OpenOnReceiptAsync(
            document, movement.DestinationDepartmentId, visit, request.FieldValues,
            request.UserId, _unitOfWork.UtcNow, cancellationToken);
        if (opened.IsFailure)
            return Result.Failure<MovementStepReceiptResponse>(opened.Error);

        await _notificationPublisher.PublishMovementAsync(NotificationType.MovementReceivedId, movement, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new MovementStepReceiptResponse(
            movement.MovementId,
            step.StepNo,
            movement.DestinationDepartmentId,
            entry.DocumentNumberId,
            entry.OfficialNumberText,
            visit.RegistryDate,
            step.ReceivedAt ?? _clock.UtcNow,
            visit.VisitId,
            entry.Visits.Count > 1 || replaced is not null,
            replaced?.DocumentNumberId,
            replaced?.OfficialNumberText));
    }

    /// <summary>
    /// Adds the visit on the department's line for the year of the date, or creates that line when
    /// the clerk supplied a number. A typed number must match an existing line, unless the clerk asked
    /// for a new number: then the existing line is replaced by a new one that links back to it.
    /// </summary>
    private async Task<Result<(DocumentNumber Entry, DocumentRegistryVisit Visit)>> WriteRegisterVisitAsync(
        ReceiveDocumentCommand request,
        DocumentMovement movement,
        DocumentMovementStep step,
        Document document,
        DateTime registryDate,
        CancellationToken cancellationToken)
    {
        var departmentId = movement.DestinationDepartmentId;
        var typedNumber = request.RegistryNumberText?.Trim();
        var entry = document.FindDepartmentNumber(departmentId, registryDate.Year);

        var renumbering = entry is not null
            && !string.IsNullOrEmpty(typedNumber)
            && !string.Equals(entry.OfficialNumberText, typedNumber, StringComparison.OrdinalIgnoreCase);

        // A different number is a typing mistake unless the clerk asked for a new number.
        if (renumbering && !request.IssueNewNumber)
            return Result.Failure<(DocumentNumber, DocumentRegistryVisit)>(DomainErrors.Numbering.DepartmentNumberLocked);

        if (entry is not null && !renumbering)
        {
            var visit = entry.RecordReceiptVisit(
                request.UserId, registryDate, step.StepId, step.SentAt, request.Notes, _unitOfWork.UtcNow);
            return visit.IsFailure
                ? Result.Failure<(DocumentNumber, DocumentRegistryVisit)>(visit.Error)
                : Result.Success((entry, visit.Value));
        }

        if (string.IsNullOrEmpty(typedNumber))
            return Result.Failure<(DocumentNumber, DocumentRegistryVisit)>(DomainErrors.Numbering.DepartmentRegistrationRequired);

        var canRegister = await _registrationService.EnsureCanRegisterAsync(document, departmentId, cancellationToken);
        if (canRegister.IsFailure)
            return Result.Failure<(DocumentNumber, DocumentRegistryVisit)>(canRegister.Error);

        var registry = await _registrationService.ResolveRegistryAsync(departmentId, request.RegistryId, cancellationToken);
        if (registry.IsFailure)
            return Result.Failure<(DocumentNumber, DocumentRegistryVisit)>(registry.Error);

        var created = await _registrationService.CreateEntryAsync(
            document, departmentId, registry.Value, typedNumber, registryDate, request.UserId, request.Notes, step.StepId,
            cancellationToken, replaceCurrent: renumbering);
        return created.IsFailure
            ? Result.Failure<(DocumentNumber, DocumentRegistryVisit)>(created.Error)
            : Result.Success((created.Value, created.Value.Visits.Single()));
    }
}
