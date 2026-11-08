using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Movements;
using InstitutionalDiwan.Application.Features.Numbering;
using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using MediatR;
using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class CreateDocumentCommandHandler : ICommandHandler<CreateDocumentCommand, DocumentResponse>
{
    private const string DefaultRegistryCode = "DEFAULT";

    private readonly IUserRepository _userRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly INumberingRegistryRepository _numberingRegistryRepository;
    private readonly IDocumentTypeSchemaVersionRepository _schemaVersionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;

    public CreateDocumentCommandHandler(
        IUserRepository userRepository,
        IDocumentRepository documentRepository,
        IDocumentTypeRepository documentTypeRepository,
        IDocumentNumberRepository documentNumberRepository,
        INumberingRegistryRepository numberingRegistryRepository,
        IDocumentTypeSchemaVersionRepository schemaVersionRepository,
        IUnitOfWork unitOfWork,
        ISender sender)
    {
        _userRepository = userRepository;
        _documentRepository = documentRepository;
        _documentTypeRepository = documentTypeRepository;
        _documentNumberRepository = documentNumberRepository;
        _numberingRegistryRepository = numberingRegistryRepository;
        _schemaVersionRepository = schemaVersionRepository;
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<Result<DocumentResponse>> Handle(
        CreateDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<DocumentResponse>(DomainErrors.Identity.UserInactive);

        var documentType = await _documentTypeRepository.GetByIdAsync(request.DocumentTypeId, cancellationToken);
        if (documentType is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentTypeNotFound);

        if (!documentType.IsActive)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentTypeInactive);

        // The creator's own department must keep a register: the typed number and date go into it.
        var departmentRegistry = await NumberingRegistryResolver.RequireDepartmentRegistryAsync(
            _numberingRegistryRepository, user.PrimaryDepartmentId, cancellationToken);
        if (departmentRegistry.IsFailure)
            return Result.Failure<DocumentResponse>(departmentRegistry.Error);

        // The central number is system-owned: a GUID, issued in the current year.
        var numberText = Guid.NewGuid().ToString("D");
        var numberingYear = _unitOfWork.UtcNow.Year;

        // Pinned to the schema published right now; this flow registers immediately, so the values
        // are applied and the required fields checked before a number is consumed.
        var schema = await _schemaVersionRepository.GetPublishedAsync(request.DocumentTypeId, cancellationToken);

        var documentResult = Document.CreateDraft(
            title: request.Title,
            documentTypeId: request.DocumentTypeId,
            directionId: request.DirectionId,
            owningDepartmentId: user.PrimaryDepartmentId,
            createdByUserId: user.UserId,
            subjectSummary: request.SubjectSummary,
            confidentialityLevelId: request.ConfidentialityLevelId,
            priorityLevelId: request.PriorityLevelId,
            utcNow: _unitOfWork.UtcNow,
            schemaVersionId: schema?.SchemaVersionId);

        if (documentResult.IsFailure)
            return Result.Failure<DocumentResponse>(documentResult.Error);

        var document = documentResult.Value;

        var valuesResult = DocumentSchemaPinning.ApplyValues(document, schema, request.FieldValues, _unitOfWork.UtcNow);
        if (valuesResult.IsFailure)
            return Result.Failure<DocumentResponse>(valuesResult.Error);

        var requiredResult = document.EnsureRequiredFieldsComplete(schema);
        if (requiredResult.IsFailure)
            return Result.Failure<DocumentResponse>(requiredResult.Error);

        // The system number always comes from a central registry, never from a department register.
        var registryResult = await ResolveRegistryAsync(
            null, request.DocumentTypeId, request.DirectionId, cancellationToken);
        if (registryResult.IsFailure)
            return Result.Failure<DocumentResponse>(registryResult.Error);

        var registry = registryResult.Value;

        var sequenceResult = registry.AllocateNextSequence(
            numberingYear, _unitOfWork.UtcNow);
        if (sequenceResult.IsFailure)
            return Result.Failure<DocumentResponse>(sequenceResult.Error);

        var numberResult = DocumentNumber.Issue(
            document.DocumentId,
            registry.RegistryId,
            numberingYear,
            sequenceResult.Value,
            numberText,
            user.UserId,
            utcNow: _unitOfWork.UtcNow);
        if (numberResult.IsFailure)
            return Result.Failure<DocumentResponse>(numberResult.Error);

        var officialNumberValue = OfficialNumberValue.Create(numberText);
        if (officialNumberValue.IsFailure)
            return Result.Failure<DocumentResponse>(officialNumberValue.Error);

        var registerResult = document.Register(
            numberResult.Value.DocumentNumberId,
            officialNumberValue.Value,
            user.UserId,
            utcNow: _unitOfWork.UtcNow);
        if (registerResult.IsFailure)
            return Result.Failure<DocumentResponse>(registerResult.Error);

        _documentRepository.Add(document);
        _documentNumberRepository.Add(numberResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var registration = await _sender.Send(new RecordDepartmentRegistrationCommand(
            document.DocumentId,
            user.PrimaryDepartmentId,
            request.DepartmentRegistryNumberText.Trim(),
            request.DepartmentRegistryDate,
            user.UserId,
            RegistryId: request.DepartmentRegistryId ?? departmentRegistry.Value.RegistryId), cancellationToken);
        if (registration.IsFailure)
            return Result.Failure<DocumentResponse>(registration.Error);

        if (request.Movement is { } route)
        {
            var opened = await _sender.Send(new CreateMovementCommand(
                document.DocumentId,
                user.UserId,
                route.DestinationDepartmentId,
                route.MovementTypeId,
                route.AssignedToUserId,
                route.Instructions,
                route.DueDate), cancellationToken);
            if (opened.IsFailure)
                return Result.Failure<DocumentResponse>(opened.Error);

            var sent = await _sender.Send(
                new SendMovementCommand(opened.Value.MovementId, user.UserId), cancellationToken);
            if (sent.IsFailure)
                return Result.Failure<DocumentResponse>(sent.Error);
        }

        return DocumentResponse.From(document);
    }

    private async Task<Result<NumberingRegistry>> ResolveRegistryAsync(
        Guid? departmentId,
        int documentTypeId,
        int? directionId,
        CancellationToken cancellationToken)
    {
        var registry = await _numberingRegistryRepository.GetActiveForScopeAsync(
            departmentId, documentTypeId, directionId, cancellationToken);

        if (registry is not null)
            return Result.Success(registry);

        var created = NumberingRegistry.Create(
            DefaultRegistryCode,
            "سجل الترقيم الافتراضي",
            utcNow: _unitOfWork.UtcNow);
        if (created.IsFailure)
            return Result.Failure<NumberingRegistry>(created.Error);

        _numberingRegistryRepository.Add(created.Value);
        return Result.Success(created.Value);
    }
}
