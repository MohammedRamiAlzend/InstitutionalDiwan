using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class CreateDraftDocumentCommandHandler
    : ICommandHandler<CreateDraftDocumentCommand, DocumentResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IDocumentTypeSchemaVersionRepository _schemaVersionRepository;
    private readonly IDocumentFieldOptionResolver _optionResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DocumentPersonLinker _personLinker;
    private readonly IInstitutionSettingRepository _settingRepository;

    public CreateDraftDocumentCommandHandler(
        IUserRepository userRepository,
        IDocumentRepository documentRepository,
        IDocumentTypeRepository documentTypeRepository,
        IDocumentTypeSchemaVersionRepository schemaVersionRepository,
        IDocumentFieldOptionResolver optionResolver,
        IUnitOfWork unitOfWork,
        DocumentPersonLinker personLinker,
        IInstitutionSettingRepository settingRepository)
    {
        _settingRepository = settingRepository;
        _personLinker = personLinker;
        _userRepository = userRepository;
        _documentRepository = documentRepository;
        _documentTypeRepository = documentTypeRepository;
        _schemaVersionRepository = schemaVersionRepository;
        _optionResolver = optionResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentResponse>> Handle(
        CreateDraftDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<DocumentResponse>(DomainErrors.Identity.UserInactive);

        var mayCreate = await DocumentCreationPolicy.EnsureMayCreateAsync(
            _settingRepository, user.PrimaryDepartmentId, cancellationToken);
        if (mayCreate.IsFailure)
            return Result.Failure<DocumentResponse>(mayCreate.Error);

        var documentType = await _documentTypeRepository.GetByIdAsync(
            request.DocumentTypeId, cancellationToken);
        if (documentType is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentTypeNotFound);

        if (!documentType.IsActive)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentTypeInactive);

        // The document is pinned to the schema published right now and keeps it for its whole life.
        var schema = await _schemaVersionRepository.GetPublishedAsync(request.DocumentTypeId, cancellationToken);

        var documentResult = Document.CreateDraft(
            request.Title,
            request.DocumentTypeId,
            request.DirectionId,
            user.PrimaryDepartmentId,
            user.UserId,
            request.SubjectSummary,
            request.ConfidentialityLevelId,
            request.PriorityLevelId,
            utcNow: _unitOfWork.UtcNow,
            schemaVersionId: schema?.SchemaVersionId,
            displayName: Document.BuildDisplayName(documentType.DescAr, null, _unitOfWork.UtcNow));

        if (documentResult.IsFailure)
            return Result.Failure<DocumentResponse>(documentResult.Error);

        var sourcedOptions = schema is null ? null : await _optionResolver.ResolveAsync(schema, cancellationToken);
        var valuesResult = DocumentSchemaPinning.ApplyValues(
            documentResult.Value, schema, request.FieldValues, sourcedOptions, _unitOfWork.UtcNow);
        if (valuesResult.IsFailure)
            return Result.Failure<DocumentResponse>(valuesResult.Error);

        var personsResult = await _personLinker.LinkAsync(
            documentResult.Value, request.Persons, user.UserId, _unitOfWork.UtcNow, cancellationToken);
        if (personsResult.IsFailure)
            return Result.Failure<DocumentResponse>(personsResult.Error);

        _documentRepository.Add(documentResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(documentResult.Value);
    }
}
