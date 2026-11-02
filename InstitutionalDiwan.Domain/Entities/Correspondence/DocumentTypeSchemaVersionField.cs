using System.Globalization;
using System.Text.RegularExpressions;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// A field as configured inside one schema version: its settings (label, requiredness, order,
/// constraints) plus the key and data type copied from the stable <see cref="DocumentField"/> so a
/// value can be validated without loading it. It owns the parsing and validation of a raw value.
/// </summary>
public sealed class DocumentTypeSchemaVersionField : Entity<Guid>
{
    public Guid SchemaVersionFieldId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DocumentTypeSchemaVersion.SchemaVersionId (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentTypeSchemaVersion)
    public Guid SchemaVersionId { get; private set; }
    // FK → DocumentField.DocumentFieldId (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentField)
    public Guid DocumentFieldId { get; private set; }
    public string FieldKey { get; private set; } = null!;
    public DocumentFieldDataType DataType { get; private set; }
    public string LabelAr { get; private set; } = null!;
    public bool IsRequired { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? RegexPattern { get; private set; }
    public string? RegexErrorMessageAr { get; private set; }
    public decimal? MinNumber { get; private set; }
    public decimal? MaxNumber { get; private set; }
    public int? MaxLength { get; private set; }
    public IReadOnlyList<DocumentFieldOption> Options { get; private set; } = Array.Empty<DocumentFieldOption>();

    private DocumentTypeSchemaVersionField()
    {
    }

    internal static DocumentTypeSchemaVersionField Create(
        Guid schemaVersionId,
        DocumentField field,
        DocumentFieldSettings settings)
    {
        var versionField = new DocumentTypeSchemaVersionField
        {
            SchemaVersionFieldId = Guid.NewGuid(),
            SchemaVersionId = schemaVersionId,
            DocumentFieldId = field.DocumentFieldId,
            FieldKey = field.Key,
            DataType = field.DataType
        };
        versionField.Apply(settings);
        return versionField;
    }

    internal void Apply(DocumentFieldSettings settings)
    {
        LabelAr = settings.LabelAr;
        IsRequired = settings.IsRequired;
        DisplayOrder = settings.DisplayOrder;
        RegexPattern = settings.RegexPattern;
        RegexErrorMessageAr = settings.RegexErrorMessageAr;
        MinNumber = settings.MinNumber;
        MaxNumber = settings.MaxNumber;
        MaxLength = settings.MaxLength;
        Options = settings.Options;
    }

    /// <summary>A new row in another schema version carrying the same field identity and settings.</summary>
    internal DocumentTypeSchemaVersionField CloneInto(Guid schemaVersionId) =>
        new()
        {
            SchemaVersionFieldId = Guid.NewGuid(),
            SchemaVersionId = schemaVersionId,
            DocumentFieldId = DocumentFieldId,
            FieldKey = FieldKey,
            DataType = DataType,
            LabelAr = LabelAr,
            IsRequired = IsRequired,
            DisplayOrder = DisplayOrder,
            RegexPattern = RegexPattern,
            RegexErrorMessageAr = RegexErrorMessageAr,
            MinNumber = MinNumber,
            MaxNumber = MaxNumber,
            MaxLength = MaxLength,
            Options = Options
        };

    /// <summary>
    /// Parses and validates a non-blank raw value against this field and returns the typed value row.
    /// </summary>
    internal Result<DocumentFieldValue> BuildValue(Guid documentId, string raw)
    {
        switch (DataType)
        {
            case DocumentFieldDataType.Text:
            case DocumentFieldDataType.LongText:
            {
                var text = raw.Trim();
                var limit = MaxLength ?? (DataType == DocumentFieldDataType.Text
                    ? DocumentFieldSettings.MaxTextLength
                    : DocumentFieldSettings.MaxLongTextLength);
                if (text.Length > limit)
                    return Result.Failure<DocumentFieldValue>(DomainErrors.DocumentFields.ValueTooLong(FieldKey, limit));

                if (RegexPattern is not null)
                {
                    try
                    {
                        if (!Regex.IsMatch(text, RegexPattern, RegexOptions.CultureInvariant, DocumentFieldSettings.RegexTimeout))
                            return Result.Failure<DocumentFieldValue>(
                                DomainErrors.DocumentFields.ValuePatternMismatch(RegexErrorMessageAr ?? string.Empty));
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        return Result.Failure<DocumentFieldValue>(DomainErrors.DocumentFields.RegexTimedOut);
                    }
                }

                return Result.Success(DocumentFieldValue.Create(documentId, DocumentFieldId, text: text));
            }

            case DocumentFieldDataType.Number:
            {
                if (!decimal.TryParse(raw.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out var number))
                    return Result.Failure<DocumentFieldValue>(DomainErrors.DocumentFields.ValueTypeInvalid(FieldKey));
                if (Math.Abs(number) >= DocumentFieldSettings.MaxAbsoluteNumber)
                    return Result.Failure<DocumentFieldValue>(DomainErrors.DocumentFields.ValueTypeInvalid(FieldKey));
                if (MinNumber is { } min && number < min)
                    return Result.Failure<DocumentFieldValue>(DomainErrors.DocumentFields.ValueBelowMinimum(FieldKey, min));
                if (MaxNumber is { } max && number > max)
                    return Result.Failure<DocumentFieldValue>(DomainErrors.DocumentFields.ValueAboveMaximum(FieldKey, max));

                return Result.Success(DocumentFieldValue.Create(documentId, DocumentFieldId, number: number));
            }

            case DocumentFieldDataType.Date:
            {
                if (!DateTime.TryParse(raw.Trim(), CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date))
                    return Result.Failure<DocumentFieldValue>(DomainErrors.DocumentFields.ValueTypeInvalid(FieldKey));

                return Result.Success(DocumentFieldValue.Create(documentId, DocumentFieldId, date: date));
            }

            case DocumentFieldDataType.Boolean:
            {
                if (!bool.TryParse(raw.Trim(), out var boolean))
                    return Result.Failure<DocumentFieldValue>(DomainErrors.DocumentFields.ValueTypeInvalid(FieldKey));

                return Result.Success(DocumentFieldValue.Create(documentId, DocumentFieldId, boolean: boolean));
            }

            case DocumentFieldDataType.Select:
            {
                var key = raw.Trim();
                if (!Options.Any(o => o.Key == key))
                    return Result.Failure<DocumentFieldValue>(DomainErrors.DocumentFields.ValueOptionInvalid(FieldKey));

                return Result.Success(DocumentFieldValue.Create(documentId, DocumentFieldId, optionKey: key));
            }

            default:
                return Result.Failure<DocumentFieldValue>(DomainErrors.DocumentFields.DataTypeInvalid);
        }
    }
}
