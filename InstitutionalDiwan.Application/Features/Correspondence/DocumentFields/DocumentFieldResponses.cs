using System.Globalization;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

/// <summary>The stable identity of a dynamic field of a document type.</summary>
public sealed record DocumentFieldResponse(
    Guid DocumentFieldId,
    int DocumentTypeId,
    string Key,
    DocumentFieldDataType DataType,
    DateTime CreatedAt)
{
    public static DocumentFieldResponse From(DocumentField field) =>
        new(field.DocumentFieldId, field.DocumentTypeId, field.Key, field.DataType, field.CreatedAt);
}

/// <summary>A field as configured inside one schema version.</summary>
public sealed record SchemaFieldResponse(
    Guid DocumentFieldId,
    string Key,
    DocumentFieldDataType DataType,
    string LabelAr,
    bool IsRequired,
    int DisplayOrder,
    string? RegexPattern,
    string? RegexErrorMessageAr,
    decimal? MinNumber,
    decimal? MaxNumber,
    int? MaxLength,
    IReadOnlyList<DocumentFieldOption> Options,
    DocumentFieldBehavior Behavior)
{
    /// <param name="field">The configured field.</param>
    /// <param name="sourcedOptions">Replaces <c>Options</c> when the field takes its choices from a source.</param>
    public static SchemaFieldResponse From(
        DocumentTypeSchemaVersionField field,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions = null) => new(
        field.DocumentFieldId, field.FieldKey, field.DataType, field.LabelAr, field.IsRequired,
        field.DisplayOrder, field.RegexPattern, field.RegexErrorMessageAr, field.MinNumber,
        field.MaxNumber, field.MaxLength,
        sourcedOptions is not null && sourcedOptions.TryGetValue(field.DocumentFieldId, out var options)
            ? options
            : field.Options,
        field.Behavior);
}

/// <summary>One schema version with its fields in display order.</summary>
public sealed record SchemaVersionResponse(
    Guid SchemaVersionId,
    int DocumentTypeId,
    int VersionNumber,
    DocumentTypeSchemaVersionStatus Status,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    Guid? PublishedByUserId,
    IReadOnlyList<SchemaFieldResponse> Fields)
{
    public static SchemaVersionResponse From(
        DocumentTypeSchemaVersion version,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions = null) => new(
        version.SchemaVersionId, version.DocumentTypeId, version.VersionNumber, version.Status,
        version.CreatedAt, version.PublishedAt, version.PublishedByUserId,
        version.Fields
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.FieldKey, StringComparer.Ordinal)
            .Select(field => SchemaFieldResponse.From(field, sourcedOptions))
            .ToList());
}

/// <summary>A version without its fields, for the version history list.</summary>
public sealed record SchemaVersionSummaryResponse(
    Guid SchemaVersionId,
    int VersionNumber,
    DocumentTypeSchemaVersionStatus Status,
    int FieldCount,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    Guid? PublishedByUserId)
{
    public static SchemaVersionSummaryResponse From(DocumentTypeSchemaVersion version) => new(
        version.SchemaVersionId, version.VersionNumber, version.Status, version.Fields.Count,
        version.CreatedAt, version.PublishedAt, version.PublishedByUserId);
}

/// <summary>
/// The schema a new document of a type would be pinned to: the published version, or none when the
/// type has no dynamic fields yet.
/// </summary>
public sealed record DocumentTypeSchemaResponse(int DocumentTypeId, SchemaVersionResponse? Schema);

/// <summary>A schema field together with the value a document stores for it, as text.</summary>
public sealed record DocumentFieldValueResponse(SchemaFieldResponse Field, string? Value);

/// <summary>The dynamic fields of one document: its pinned schema version and the stored values.</summary>
public sealed record DocumentFieldsResponse(
    Guid DocumentId,
    Guid? SchemaVersionId,
    int? VersionNumber,
    IReadOnlyList<DocumentFieldValueResponse> Fields)
{
    /// <summary>A document with no pinned schema has no dynamic fields.</summary>
    public static DocumentFieldsResponse From(
        Document document,
        DocumentTypeSchemaVersion? schema,
        IReadOnlyDictionary<Guid, IReadOnlyList<DocumentFieldOption>>? sourcedOptions = null)
    {
        if (schema is null)
            return new DocumentFieldsResponse(document.DocumentId, document.SchemaVersionId, null, Array.Empty<DocumentFieldValueResponse>());

        var values = document.FieldValues.ToDictionary(value => value.DocumentFieldId);
        var fields = SchemaVersionResponse.From(schema, sourcedOptions).Fields
            .Select(field => new DocumentFieldValueResponse(
                field,
                values.TryGetValue(field.DocumentFieldId, out var value) ? DocumentFieldValueText.Format(field.DataType, value) : null))
            .ToList();

        return new DocumentFieldsResponse(document.DocumentId, schema.SchemaVersionId, schema.VersionNumber, fields);
    }
}

/// <summary>The text form of a stored value, the same one <see cref="DocumentFieldValueInput"/> accepts.</summary>
public static class DocumentFieldValueText
{
    public static string? Format(DocumentFieldDataType dataType, DocumentFieldValue value) => dataType switch
    {
        DocumentFieldDataType.Text or DocumentFieldDataType.LongText => value.TextValue,
        DocumentFieldDataType.Number => value.NumberValue?.ToString(CultureInfo.InvariantCulture),
        DocumentFieldDataType.Date => value.DateValue?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
        DocumentFieldDataType.Boolean => value.BoolValue is { } flag ? (flag ? "true" : "false") : null,
        DocumentFieldDataType.Select => value.OptionKey,
        DocumentFieldDataType.MultiSelect => value.TextValue?.Trim('|').Replace('|', ','),
        _ => null
    };
}
