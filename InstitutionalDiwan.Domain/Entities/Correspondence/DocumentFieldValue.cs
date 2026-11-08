namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// One stored dynamic-field value of a document. Exactly one typed column is populated, chosen by the
/// field's data type, so registry filters can compare natively (numbers as numbers, dates as dates)
/// and be indexed. Built only through <see cref="DocumentTypeSchemaVersion.BuildValues"/>.
/// </summary>
public sealed class DocumentFieldValue : Entity<Guid>
{
    public Guid DocumentFieldValueId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid DocumentId { get; private set; }
    // FK → DocumentField.DocumentFieldId (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentField)
    public Guid DocumentFieldId { get; private set; }
    /// <summary>Text and long-text fields, and multi-select keys as <c>|a|b|</c>.</summary>
    public string? TextValue { get; private set; }
    public decimal? NumberValue { get; private set; }
    public DateTime? DateValue { get; private set; }
    public bool? BoolValue { get; private set; }
    /// <summary>The chosen option key of a select field.</summary>
    public string? OptionKey { get; private set; }

    private DocumentFieldValue()
    {
    }

    internal static DocumentFieldValue Create(
        Guid documentId,
        Guid documentFieldId,
        string? text = null,
        decimal? number = null,
        DateTime? date = null,
        bool? boolean = null,
        string? optionKey = null) =>
        new()
        {
            DocumentFieldValueId = Guid.NewGuid(),
            DocumentId = documentId,
            DocumentFieldId = documentFieldId,
            TextValue = text,
            NumberValue = number,
            DateValue = date,
            BoolValue = boolean,
            OptionKey = optionKey
        };

    /// <summary>True when this value carries <paramref name="choice"/>; used by visibility conditions.</summary>
    internal bool MatchesChoice(string choice)
    {
        if (BoolValue is { } boolean)
            return (boolean ? "true" : "false") == choice;
        if (OptionKey is not null)
            return OptionKey == choice;
        return TextValue?.Contains($"|{choice}|", StringComparison.Ordinal) == true;
    }

    internal bool HasSameContent(DocumentFieldValue other) =>
        TextValue == other.TextValue
        && NumberValue == other.NumberValue
        && DateValue?.Date == other.DateValue?.Date
        && BoolValue == other.BoolValue
        && OptionKey == other.OptionKey;
}
