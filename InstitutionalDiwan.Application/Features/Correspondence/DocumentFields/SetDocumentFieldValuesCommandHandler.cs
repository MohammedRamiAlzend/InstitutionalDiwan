using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

internal sealed class SetDocumentFieldValuesCommandHandler
    : ICommandHandler<SetDocumentFieldValuesCommand, DocumentFieldsResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;

    public SetDocumentFieldValuesCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _versionRepository = versionRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentFieldsResponse>> Handle(
        SetDocumentFieldValuesCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentFieldsResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.DownloadId, cancellationToken))
        {
            return Result.Failure<DocumentFieldsResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var schema = document.SchemaVersionId is { } schemaVersionId
            ? await _versionRepository.GetByIdAsync(schemaVersionId, cancellationToken)
            : null;

        // A registered document is locked even when it has no schema to validate against.
        if (document.CurrentStatusId != DocumentStatusType.DraftId)
            return Result.Failure<DocumentFieldsResponse>(DomainErrors.Correspondence.OnlyDraftEditable);

        var applied = DocumentSchemaPinning.ApplyValues(document, schema, request.Values, _unitOfWork.UtcNow);
        if (applied.IsFailure)
            return Result.Failure<DocumentFieldsResponse>(applied.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DocumentFieldsResponse.From(document, schema);
    }
}

/// <summary>
/// Shared by every flow that creates a document or edits its dynamic values, so a document is always
/// pinned to the schema that was published when it was created and its values are checked against it.
/// </summary>
internal static class DocumentSchemaPinning
{
    /// <summary>
    /// Sets <paramref name="values"/> on the document against its pinned <paramref name="schema"/>.
    /// With no schema there is nothing a value could belong to, so any supplied value is rejected.
    /// </summary>
    public static Result ApplyValues(
        Document document,
        DocumentTypeSchemaVersion? schema,
        IReadOnlyList<DocumentFieldValueInput>? values,
        DateTime utcNow)
    {
        if (values is null || values.Count == 0)
            return Result.Success();

        if (schema is null)
            return Result.Failure(DomainErrors.DocumentFields.UnknownField(values[0].FieldKey));

        return document.SetFieldValues(schema, values, utcNow);
    }
}
