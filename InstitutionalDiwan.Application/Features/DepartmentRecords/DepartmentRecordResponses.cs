using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.DepartmentRecords;

/// <summary>The department records on one document that the caller may read.</summary>
public sealed record DocumentDepartmentRecordsResponse(
    Guid DocumentId,
    IReadOnlyList<DepartmentRecordResponse> Records);

/// <summary>One department's register line on the document with the record of each of its visits.</summary>
public sealed record DepartmentRecordResponse(
    Guid RegistryNumberId,
    Guid DepartmentId,
    string DepartmentNameAr,
    string RegistryNumberText,
    int NumberingYear,
    IReadOnlyList<DepartmentRecordVisitResponse> Visits);

/// <summary>
/// One visit and the record the department kept on it. <c>Fields</c> is empty when the department had
/// no published form for the document type when the visit was opened.
/// </summary>
public sealed record DepartmentRecordVisitResponse(
    Guid VisitId,
    DateTime RegistryDate,
    Guid? MovementStepId,
    DateTime RecordedAt,
    Guid? SchemaVersionId,
    int? VersionNumber,
    bool IsLocked,
    DateTime? LockedAt,
    DateTime? RecordUpdatedAt,
    DateTime? CorrectedAt,
    string? CorrectionReason,
    bool CanEdit,
    bool CanCorrect,
    IReadOnlyList<DocumentFieldValueResponse> Fields)
{
    /// <param name="visit">The visit.</param>
    /// <param name="schema">The form the visit is pinned to, when it has one.</param>
    /// <param name="sourcedOptions">Current choices of the source-fed fields of <paramref name="schema"/>.</param>
    /// <param name="canEditOpen">The caller may edit this department's open records.</param>
    /// <param name="canCorrectLocked">The caller may correct locked records.</param>
    public static DepartmentRecordVisitResponse From(
        DocumentRegistryVisit visit,
        DocumentTypeSchemaVersion? schema,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions,
        bool canEditOpen,
        bool canCorrectLocked)
    {
        var fields = schema is null
            ? Array.Empty<DocumentFieldValueResponse>()
            : RecordFields(visit, schema, sourcedOptions);

        return new DepartmentRecordVisitResponse(
            visit.VisitId,
            visit.RegistryDate,
            visit.MovementStepId,
            visit.RecordedAt,
            visit.SchemaVersionId,
            schema?.VersionNumber,
            visit.IsLocked,
            visit.LockedAt,
            visit.RecordUpdatedAt,
            visit.CorrectedAt,
            visit.CorrectionReason,
            CanEdit: schema is not null && !visit.IsLocked && canEditOpen,
            CanCorrect: schema is not null && visit.IsLocked && canCorrectLocked,
            fields);
    }

    private static IReadOnlyList<DocumentFieldValueResponse> RecordFields(
        DocumentRegistryVisit visit,
        DocumentTypeSchemaVersion schema,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions)
    {
        var values = visit.FieldValues().ToDictionary(value => value.DocumentFieldId);
        return SchemaVersionResponse.From(schema, sourcedOptions).Fields
            .Select(field => new DocumentFieldValueResponse(
                field,
                values.TryGetValue(field.DocumentFieldId, out var value)
                    ? DocumentFieldValueText.Format(field.DataType, value)
                    : null))
            .ToList();
    }
}
