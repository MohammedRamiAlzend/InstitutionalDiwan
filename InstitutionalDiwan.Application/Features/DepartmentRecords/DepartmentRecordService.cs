using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.DepartmentRecords;

/// <summary>
/// Opens and locks the record a department keeps on each visit of a document. Every movement and
/// document handler that hands the document to a department, or takes it away, goes through here, so
/// the rule lives in one place:
/// <list type="bullet">
/// <item>A receipt opens a record on its visit with the department's published form for the
/// document type, and every required field must be filled.</item>
/// <item>Leaving a department locks the record of the visit that held the document: the visit linked
/// to the step being left (redirect, reject, return), or the sender's open visit for a new send.</item>
/// <item>The document is held by one department at a time, so a receipt also locks any other record
/// still open on it, and archiving or cancelling locks them all.</item>
/// </list>
/// All writes join the caller's unit of work, so they commit (or fail) with the command itself.
/// </summary>
internal sealed class DepartmentRecordService
{
    private const string ReturnVisitNote = "Returned to sender";

    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IDocumentFieldOptionResolver _optionResolver;

    public DepartmentRecordService(
        IDocumentRepository documentRepository,
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDocumentFieldOptionResolver optionResolver)
    {
        _documentRepository = documentRepository;
        _versionRepository = versionRepository;
        _optionResolver = optionResolver;
    }

    /// <summary>
    /// Opens the department record on the visit a receipt wrote. Without a published form for
    /// (type, department) the receipt works as before and no value may be sent.
    /// </summary>
    public async Task<Result> OpenOnReceiptAsync(
        Document document,
        Guid departmentId,
        DocumentRegistryVisit visit,
        IReadOnlyList<DocumentFieldValueInput>? values,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        document.LockOpenRegistryVisits(utcNow, exceptVisitId: visit.VisitId);
        return await OpenAsync(
            document, departmentId, visit, values ?? Array.Empty<DocumentFieldValueInput>(),
            actorUserId, utcNow, requireComplete: true, cancellationToken);
    }

    /// <summary>A new movement is sent: the sending department's open record is locked.</summary>
    public async Task LockDepartmentAsync(
        Guid documentId,
        Guid departmentId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetWithNumbersAsync(documentId, cancellationToken);
        document?.LockOpenRegistryVisits(utcNow, departmentId);
    }

    /// <summary>
    /// The document leaves the department that received <paramref name="step"/>: that visit's record
    /// is locked. A step that was never received has no visit, so nothing is locked.
    /// </summary>
    public async Task LockStepVisitAsync(
        Guid documentId,
        DocumentMovementStep step,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (step.RegistryVisitId is not { } visitId)
            return;

        var document = await _documentRepository.GetWithNumbersAsync(documentId, cancellationToken);
        document?.FindRegistryVisit(visitId)?.Visit.Lock(utcNow);
    }

    /// <summary>Archiving or cancelling the document locks every record still open on it.</summary>
    public async Task LockAllAsync(Guid documentId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetWithNumbersAsync(documentId, cancellationToken);
        document?.LockOpenRegistryVisits(utcNow);
    }

    /// <summary>
    /// A movement step was returned to its sender. The returning department's record is locked and
    /// the sender gets a new visit dated today on its register line for this year, with its form
    /// opened (required fields are not enforced until the sender saves the record). A sender with no
    /// active line this year gets no visit.
    /// </summary>
    public async Task<Result> OpenOnReturnAsync(
        Guid documentId,
        DocumentMovementStep returnedStep,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetWithNumbersAsync(documentId, cancellationToken);
        if (document is null)
            return Result.Success();

        if (returnedStep.RegistryVisitId is { } returningVisitId)
            document.FindRegistryVisit(returningVisitId)?.Visit.Lock(utcNow);

        var registryDate = utcNow.Date;
        var line = document.FindDepartmentNumber(returnedStep.FromDepartmentId, registryDate.Year);
        if (line is null || line.StatusId != DocumentNumberStatus.ActiveId)
            return Result.Success();

        var visit = line.RecordReturnVisit(actorUserId, registryDate, returnedStep.StepId, ReturnVisitNote, utcNow);
        if (visit.IsFailure)
            return visit;

        document.LockOpenRegistryVisits(utcNow, exceptVisitId: visit.Value.VisitId);
        return await OpenAsync(
            document, returnedStep.FromDepartmentId, visit.Value, Array.Empty<DocumentFieldValueInput>(),
            actorUserId, utcNow, requireComplete: false, cancellationToken);
    }

    private async Task<Result> OpenAsync(
        Document document,
        Guid departmentId,
        DocumentRegistryVisit visit,
        IReadOnlyList<DocumentFieldValueInput> values,
        Guid actorUserId,
        DateTime utcNow,
        bool requireComplete,
        CancellationToken cancellationToken)
    {
        var schema = await _versionRepository.GetPublishedAsync(document.DocumentTypeId, departmentId, cancellationToken);
        var sourcedOptions = schema is null ? null : await _optionResolver.ResolveAsync(schema, cancellationToken);
        return visit.OpenRecord(schema, values, actorUserId, utcNow, requireComplete, sourcedOptions);
    }
}
