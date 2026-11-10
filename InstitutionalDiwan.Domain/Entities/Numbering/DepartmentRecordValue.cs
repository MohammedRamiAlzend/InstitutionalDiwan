using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Domain.Entities.Numbering;

/// <summary>
/// One value a department wrote in its record form on one visit of a document. It mirrors the typed
/// columns of <see cref="DocumentFieldValue"/> but lives in its own table owned by the visit, so the
/// creation values of the document, their filters and indexes are never mixed with visit values.
/// </summary>
public sealed class DepartmentRecordValue : Entity<Guid>
{
    public Guid ValueId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DocumentRegistryVisit.VisitId (InstitutionalDiwan.Domain.Entities.Numbering.DocumentRegistryVisit)
    public Guid VisitId { get; private set; }
    // FK → DocumentField.DocumentFieldId (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentField)
    public Guid DocumentFieldId { get; private set; }
    public string? TextValue { get; private set; }
    public decimal? NumberValue { get; private set; }
    public DateTime? DateValue { get; private set; }
    public bool? BoolValue { get; private set; }
    public string? OptionKey { get; private set; }

    private DepartmentRecordValue()
    {
    }

    internal static DepartmentRecordValue From(Guid visitId, DocumentFieldValue value)
    {
        var record = new DepartmentRecordValue
        {
            ValueId = Guid.NewGuid(),
            VisitId = visitId,
            DocumentFieldId = value.DocumentFieldId
        };
        record.Apply(value);
        return record;
    }

    internal void Apply(DocumentFieldValue value)
    {
        TextValue = value.TextValue;
        NumberValue = value.NumberValue;
        DateValue = value.DateValue;
        BoolValue = value.BoolValue;
        OptionKey = value.OptionKey;
    }

    /// <summary>
    /// The same content as a transient field value, so schema validation, visibility conditions and
    /// text formatting written for document values work on visit values unchanged.
    /// </summary>
    public DocumentFieldValue AsFieldValue() =>
        DocumentFieldValue.Create(VisitId, DocumentFieldId, TextValue, NumberValue, DateValue, BoolValue, OptionKey);
}
