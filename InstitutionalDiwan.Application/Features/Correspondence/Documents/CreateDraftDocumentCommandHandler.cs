using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
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
    private readonly IUnitOfWork _unitOfWork;

    public CreateDraftDocumentCommandHandler(
        IUserRepository userRepository,
        IDocumentRepository documentRepository,
        IDocumentTypeRepository documentTypeRepository,
        IDocumentTypeSchemaVersionRepository schemaVersionRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _documentRepository = documentRepository;
        _documentTypeRepository = documentTypeRepository;
        _schemaVersionRepository = schemaVersionRepository;
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
            request.ConfidentialityLevelId ?? DocumentConfidentialityLevel.NormalId,
            request.PriorityLevelId ?? DocumentPriorityLevel.NormalId,
            request.DocumentDate,
            request.ExternalReferenceNumber,
            request.ExternalDocumentDate,
            utcNow: _unitOfWork.UtcNow,
            schemaVersionId: schema?.SchemaVersionId);

        if (documentResult.IsFailure)
            return Result.Failure<DocumentResponse>(documentResult.Error);

        var valuesResult = DocumentSchemaPinning.ApplyValues(
            documentResult.Value, schema, request.FieldValues, _unitOfWork.UtcNow);
        if (valuesResult.IsFailure)
            return Result.Failure<DocumentResponse>(valuesResult.Error);

        _documentRepository.Add(documentResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentResponse.From(documentResult.Value);
    }
}
