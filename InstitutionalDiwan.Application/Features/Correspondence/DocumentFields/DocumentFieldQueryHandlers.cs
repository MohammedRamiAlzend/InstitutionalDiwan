using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

internal sealed class ListDocumentFieldsQueryHandler
    : IQueryHandler<ListDocumentFieldsQuery, IReadOnlyList<DocumentFieldResponse>>
{
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IDocumentFieldRepository _fieldRepository;

    public ListDocumentFieldsQueryHandler(
        IDocumentTypeRepository documentTypeRepository,
        IDocumentFieldRepository fieldRepository)
    {
        _documentTypeRepository = documentTypeRepository;
        _fieldRepository = fieldRepository;
    }

    public async Task<Result<IReadOnlyList<DocumentFieldResponse>>> Handle(
        ListDocumentFieldsQuery request,
        CancellationToken cancellationToken)
    {
        if (await _documentTypeRepository.GetByIdAsync(request.DocumentTypeId, cancellationToken) is null)
            return Result.Failure<IReadOnlyList<DocumentFieldResponse>>(DomainErrors.Correspondence.DocumentTypeNotFound);

        var fields = await _fieldRepository.ListByDocumentTypeAsync(request.DocumentTypeId, cancellationToken);
        return Result.Success<IReadOnlyList<DocumentFieldResponse>>(
            fields.Select(DocumentFieldResponse.From).ToList());
    }
}

internal sealed class ListSchemaVersionsQueryHandler
    : IQueryHandler<ListSchemaVersionsQuery, IReadOnlyList<SchemaVersionSummaryResponse>>
{
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;

    public ListSchemaVersionsQueryHandler(
        IDocumentTypeRepository documentTypeRepository,
        IDocumentTypeSchemaVersionRepository versionRepository)
    {
        _documentTypeRepository = documentTypeRepository;
        _versionRepository = versionRepository;
    }

    public async Task<Result<IReadOnlyList<SchemaVersionSummaryResponse>>> Handle(
        ListSchemaVersionsQuery request,
        CancellationToken cancellationToken)
    {
        if (await _documentTypeRepository.GetByIdAsync(request.DocumentTypeId, cancellationToken) is null)
            return Result.Failure<IReadOnlyList<SchemaVersionSummaryResponse>>(DomainErrors.Correspondence.DocumentTypeNotFound);

        var versions = await _versionRepository.ListByDocumentTypeAsync(request.DocumentTypeId, cancellationToken);
        return Result.Success<IReadOnlyList<SchemaVersionSummaryResponse>>(
            versions.Select(SchemaVersionSummaryResponse.From).ToList());
    }
}

internal sealed class GetSchemaVersionQueryHandler : IQueryHandler<GetSchemaVersionQuery, SchemaVersionResponse>
{
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;

    public GetSchemaVersionQueryHandler(IDocumentTypeSchemaVersionRepository versionRepository) =>
        _versionRepository = versionRepository;

    public async Task<Result<SchemaVersionResponse>> Handle(
        GetSchemaVersionQuery request,
        CancellationToken cancellationToken)
    {
        var version = await _versionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        return version is null
            ? Result.Failure<SchemaVersionResponse>(DomainErrors.DocumentFields.SchemaVersionNotFound)
            : SchemaVersionResponse.From(version);
    }
}

internal sealed class GetDocumentTypeSchemaQueryHandler
    : IQueryHandler<GetDocumentTypeSchemaQuery, DocumentTypeSchemaResponse>
{
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;

    public GetDocumentTypeSchemaQueryHandler(
        IDocumentTypeRepository documentTypeRepository,
        IDocumentTypeSchemaVersionRepository versionRepository)
    {
        _documentTypeRepository = documentTypeRepository;
        _versionRepository = versionRepository;
    }

    public async Task<Result<DocumentTypeSchemaResponse>> Handle(
        GetDocumentTypeSchemaQuery request,
        CancellationToken cancellationToken)
    {
        if (await _documentTypeRepository.GetByIdAsync(request.DocumentTypeId, cancellationToken) is null)
            return Result.Failure<DocumentTypeSchemaResponse>(DomainErrors.Correspondence.DocumentTypeNotFound);

        var published = await _versionRepository.GetPublishedAsync(request.DocumentTypeId, cancellationToken);
        return new DocumentTypeSchemaResponse(
            request.DocumentTypeId,
            published is null ? null : SchemaVersionResponse.From(published));
    }
}

internal sealed class GetDocumentFieldsQueryHandler : IQueryHandler<GetDocumentFieldsQuery, DocumentFieldsResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public GetDocumentFieldsQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _documentRepository = documentRepository;
        _versionRepository = versionRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<DocumentFieldsResponse>> Handle(
        GetDocumentFieldsQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentFieldsResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<DocumentFieldsResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var schema = document.SchemaVersionId is { } schemaVersionId
            ? await _versionRepository.GetByIdAsync(schemaVersionId, cancellationToken)
            : null;

        return DocumentFieldsResponse.From(document, schema);
    }
}
