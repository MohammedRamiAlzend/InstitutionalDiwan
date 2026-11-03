using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Numbering;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class RegisterDocumentCommandHandler
    : ICommandHandler<RegisterDocumentCommand, DocumentResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IDocumentTypeSchemaVersionRepository _schemaVersionRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IClock _clock;

    public RegisterDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        INumberingRegistryRepository numberingRegistryRepository,
        IDocumentTypeSchemaVersionRepository schemaVersionRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUnitOfWork unitOfWork,
        INotificationPublisher notificationPublisher,
        IClock clock)
    {
        _documentAccessEvaluator = documentAccessEvaluator;
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _numberingRegistryRepository = numberingRegistryRepository;
        _schemaVersionRepository = schemaVersionRepository;
        _unitOfWork = unitOfWork;
        _notificationPublisher = notificationPublisher;
        _clock = clock;
    }

    public async Task<Result<DocumentResponse>> Handle(
        RegisterDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.DownloadId, cancellationToken))
        {
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        // Checked before allocating a sequence so a rejected registration never burns a number.
        if (document.CurrentStatusId != DocumentStatusType.DraftId)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.OnlyDraftRegisterable);

        if (document.ActiveOfficialNumberId is not null)
            return Result.Failure<DocumentResponse>(DomainErrors.Numbering.DocumentAlreadyNumbered);

        // Also before allocating a sequence: a missing required dynamic field must not burn a number.
        var schema = document.SchemaVersionId is { } schemaVersionId
            ? await _schemaVersionRepository.GetByIdAsync(schemaVersionId, cancellationToken)
            : null;
        var requiredResult = document.EnsureRequiredFieldsComplete(schema);
        if (requiredResult.IsFailure)
            return Result.Failure<DocumentResponse>(requiredResult.Error);

        var numberingYear = request.NumberingYear
            ?? document.DocumentDate?.Year
            ?? _clock.UtcNow.Year;

        var registryResult = await NumberingRegistryResolver.ResolveAsync(
            _numberingRegistryRepository,
            document.OwningDepartmentId,
            document.DocumentTypeId,
            document.DirectionId,
            cancellationToken,
            _unitOfWork.UtcNow);

        if (registryResult.IsFailure)
            return Result.Failure<DocumentResponse>(registryResult.Error);

        var registry = registryResult.Value;

        var sequenceResult = registry.AllocateNextSequence(numberingYear, _unitOfWork.UtcNow);
        if (sequenceResult.IsFailure)
            return Result.Failure<DocumentResponse>(sequenceResult.Error);

        var numberText = OfficialNumberFormatter.Format(
            registry.FormatPattern, numberingYear, sequenceResult.Value);

        if (await _documentNumberRepository.ExistsByNumberAndYearAsync(
                numberText, numberingYear, cancellationToken))
        {
            return Result.Failure<DocumentResponse>(DomainErrors.Numbering.OfficialNumberTaken);
        }

        var numberResult = DocumentNumber.Issue(
            document.DocumentId,
            registry.RegistryId,
            numberingYear,
            sequenceResult.Value,
            numberText,
            request.UserId,
            utcNow: _unitOfWork.UtcNow);

        if (numberResult.IsFailure)
            return Result.Failure<DocumentResponse>(numberResult.Error);

        var officialNumberValue = OfficialNumberValue.Create(numberText);
        if (officialNumberValue.IsFailure)
            return Result.Failure<DocumentResponse>(officialNumberValue.Error);

        var registerResult = document.Register(
            numberResult.Value.DocumentNumberId,
            officialNumberValue.Value,
            request.UserId,
            utcNow: _unitOfWork.UtcNow);

        if (registerResult.IsFailure)
            return Result.Failure<DocumentResponse>(registerResult.Error);

        _documentNumberRepository.Add(numberResult.Value);
        await _notificationPublisher.PublishDocumentAsync(NotificationType.DocumentRegisteredId, document, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(document);
    }
}
