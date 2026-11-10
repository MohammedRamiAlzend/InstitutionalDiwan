using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.DepartmentRecords;

/// <summary>Loads a visit together with the document and register line that hold it.</summary>
internal static class VisitLoader
{
    public static async Task<Result<(Document Document, DocumentNumber Line, DocumentRegistryVisit Visit)>> LoadAsync(
        Guid visitId,
        IDocumentNumberRepository numberRepository,
        IDocumentRepository documentRepository,
        CancellationToken cancellationToken)
    {
        var documentId = await numberRepository.FindDocumentIdByVisitAsync(visitId, cancellationToken);
        var document = documentId is { } id
            ? await documentRepository.GetWithNumbersAsync(id, cancellationToken)
            : null;
        var found = document?.FindRegistryVisit(visitId);
        return found is { } pair
            ? Result.Success((document!, pair.Line, pair.Visit))
            : Result.Failure<(Document, DocumentNumber, DocumentRegistryVisit)>(DomainErrors.DepartmentRecords.VisitNotFound);
    }

    public static async Task<Result<(DocumentTypeSchemaVersion Schema, IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>> Options)>> SchemaOfAsync(
        DocumentRegistryVisit visit,
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDocumentFieldOptionResolver optionResolver,
        CancellationToken cancellationToken)
    {
        var schema = visit.SchemaVersionId is { } schemaVersionId
            ? await versionRepository.GetByIdAsync(schemaVersionId, cancellationToken)
            : null;
        if (schema is null)
            return Result.Failure<(DocumentTypeSchemaVersion, IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>)>(
                DomainErrors.DepartmentRecords.SchemaNotFound);

        return Result.Success((schema, await optionResolver.ResolveAsync(schema, cancellationToken)));
    }
}

internal sealed class SetVisitRecordCommandHandler : ICommandHandler<SetVisitRecordCommand, DepartmentRecordVisitResponse>
{
    private readonly IDocumentNumberRepository _numberRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IDocumentFieldOptionResolver _optionResolver;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public SetVisitRecordCommandHandler(
        IDocumentNumberRepository numberRepository,
        IDocumentRepository documentRepository,
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDocumentFieldOptionResolver optionResolver,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _numberRepository = numberRepository;
        _documentRepository = documentRepository;
        _versionRepository = versionRepository;
        _optionResolver = optionResolver;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DepartmentRecordVisitResponse>> Handle(SetVisitRecordCommand request, CancellationToken cancellationToken)
    {
        var loaded = await VisitLoader.LoadAsync(request.VisitId, _numberRepository, _documentRepository, cancellationToken);
        if (loaded.IsFailure)
            return Result.Failure<DepartmentRecordVisitResponse>(loaded.Error);

        var (_, line, visit) = loaded.Value;

        // The record speaks for its department only: custody follows the department, not document access.
        if (line.DepartmentId is not { } departmentId
            || !await _departmentScopeEvaluator.CanAccessDepartmentAsync(departmentId, cancellationToken))
        {
            return Result.Failure<DepartmentRecordVisitResponse>(DomainErrors.DepartmentRecords.NotOwnerDepartment);
        }

        if (visit.IsLocked)
            return Result.Failure<DepartmentRecordVisitResponse>(DomainErrors.DepartmentRecords.VisitLocked);

        var schema = await VisitLoader.SchemaOfAsync(visit, _versionRepository, _optionResolver, cancellationToken);
        if (schema.IsFailure)
            return Result.Failure<DepartmentRecordVisitResponse>(schema.Error);

        var applied = visit.SetRecordValues(schema.Value.Schema, request.Values, request.UserId, _unitOfWork.UtcNow, schema.Value.Options);
        if (applied.IsFailure)
            return Result.Failure<DepartmentRecordVisitResponse>(applied.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DepartmentRecordVisitResponse.From(
            visit, schema.Value.Schema, schema.Value.Options,
            canEditOpen: true,
            canCorrectLocked: _userContext.Permissions.Contains(Permissions.DepartmentRecordCorrect));
    }
}

internal sealed class CorrectVisitRecordCommandHandler : ICommandHandler<CorrectVisitRecordCommand, DepartmentRecordVisitResponse>
{
    private readonly IDocumentNumberRepository _numberRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IDocumentFieldOptionResolver _optionResolver;
    private readonly IUnitOfWork _unitOfWork;

    public CorrectVisitRecordCommandHandler(
        IDocumentNumberRepository numberRepository,
        IDocumentRepository documentRepository,
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDocumentFieldOptionResolver optionResolver,
        IUnitOfWork unitOfWork)
    {
        _numberRepository = numberRepository;
        _documentRepository = documentRepository;
        _versionRepository = versionRepository;
        _optionResolver = optionResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DepartmentRecordVisitResponse>> Handle(CorrectVisitRecordCommand request, CancellationToken cancellationToken)
    {
        var loaded = await VisitLoader.LoadAsync(request.VisitId, _numberRepository, _documentRepository, cancellationToken);
        if (loaded.IsFailure)
            return Result.Failure<DepartmentRecordVisitResponse>(loaded.Error);

        var visit = loaded.Value.Visit;
        var schema = await VisitLoader.SchemaOfAsync(visit, _versionRepository, _optionResolver, cancellationToken);
        if (schema.IsFailure)
            return Result.Failure<DepartmentRecordVisitResponse>(schema.Error);

        var corrected = visit.CorrectRecordValues(
            schema.Value.Schema, request.Values, request.Reason, request.UserId, _unitOfWork.UtcNow, schema.Value.Options);
        if (corrected.IsFailure)
            return Result.Failure<DepartmentRecordVisitResponse>(corrected.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DepartmentRecordVisitResponse.From(visit, schema.Value.Schema, schema.Value.Options, canEditOpen: false, canCorrectLocked: true);
    }
}

internal sealed class GetDocumentDepartmentRecordsQueryHandler
    : IQueryHandler<GetDocumentDepartmentRecordsQuery, DocumentDepartmentRecordsResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IDocumentFieldOptionResolver _optionResolver;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUserContext _userContext;

    public GetDocumentDepartmentRecordsQueryHandler(
        IDocumentRepository documentRepository,
        IDepartmentRepository departmentRepository,
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDocumentFieldOptionResolver optionResolver,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUserContext userContext)
    {
        _documentRepository = documentRepository;
        _departmentRepository = departmentRepository;
        _versionRepository = versionRepository;
        _optionResolver = optionResolver;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _documentAccessEvaluator = documentAccessEvaluator;
        _userContext = userContext;
    }

    /// <summary>
    /// A department's records are shown to that department (its scope), and to callers holding
    /// <c>DEPARTMENT_RECORD_VIEW_ALL</c> who may also view the document. Anything else is left out,
    /// so the caller does not learn what another department wrote.
    /// </summary>
    public async Task<Result<DocumentDepartmentRecordsResponse>> Handle(
        GetDocumentDepartmentRecordsQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetWithNumbersAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentDepartmentRecordsResponse>(DomainErrors.Correspondence.DocumentNotFound);

        var permissions = _userContext.Permissions;
        var canViewAll = permissions.Contains(Permissions.DepartmentRecordViewAll)
            && await _documentAccessEvaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ViewId, cancellationToken);
        var canEdit = permissions.Contains(Permissions.DocumentEdit);
        var canCorrect = permissions.Contains(Permissions.DepartmentRecordCorrect);

        var schemas = new Dictionary<Guid, (DocumentTypeSchemaVersion Schema, IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>> Options)?>();
        var records = new List<DepartmentRecordResponse>();

        foreach (var line in document.Numbers
                     .Where(number => number.IsDepartmentEntry && number.DepartmentId is not null)
                     .OrderBy(number => number.NumberingYear)
                     .ThenBy(number => number.IssuedAt))
        {
            var departmentId = line.DepartmentId!.Value;
            var ownDepartment = await _departmentScopeEvaluator.CanAccessDepartmentAsync(departmentId, cancellationToken);
            if (!ownDepartment && !canViewAll)
                continue;

            var visits = new List<DepartmentRecordVisitResponse>();
            foreach (var visit in line.Visits.OrderBy(visit => visit.RegistryDate).ThenBy(visit => visit.RecordedAt))
            {
                var schema = await SchemaOfAsync(visit, schemas, cancellationToken);
                visits.Add(DepartmentRecordVisitResponse.From(
                    visit, schema?.Schema, schema?.Options,
                    canEditOpen: ownDepartment && canEdit,
                    canCorrectLocked: canCorrect));
            }

            var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);
            records.Add(new DepartmentRecordResponse(
                line.DocumentNumberId,
                departmentId,
                department?.NameAr ?? string.Empty,
                line.OfficialNumberText,
                line.NumberingYear,
                visits));
        }

        return new DocumentDepartmentRecordsResponse(document.DocumentId, records);
    }

    private async Task<(DocumentTypeSchemaVersion Schema, IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>> Options)?> SchemaOfAsync(
        DocumentRegistryVisit visit,
        Dictionary<Guid, (DocumentTypeSchemaVersion Schema, IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>> Options)?> cache,
        CancellationToken cancellationToken)
    {
        if (visit.SchemaVersionId is not { } schemaVersionId)
            return null;

        if (cache.TryGetValue(schemaVersionId, out var cached))
            return cached;

        var schema = await _versionRepository.GetByIdAsync(schemaVersionId, cancellationToken);
        var entry = schema is null
            ? ((DocumentTypeSchemaVersion, IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>)?)null
            : (schema, await _optionResolver.ResolveAsync(schema, cancellationToken));
        cache[schemaVersionId] = entry;
        return entry;
    }
}
