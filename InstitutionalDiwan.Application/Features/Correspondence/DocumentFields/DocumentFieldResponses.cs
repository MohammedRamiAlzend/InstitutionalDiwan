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
    IReadOnlyList<DocumentFieldOption> Options)
{
    public static SchemaFieldResponse From(DocumentTypeSchemaVersionField field) => new(
        field.DocumentFieldId, field.FieldKey, field.DataType, field.LabelAr, field.IsRequired,
        field.DisplayOrder, field.RegexPattern, field.RegexErrorMessageAr, field.MinNumber,
        field.MaxNumber, field.MaxLength, field.Options);
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
    public static SchemaVersionResponse From(DocumentTypeSchemaVersion version) => new(
        version.SchemaVersionId, version.DocumentTypeId, version.VersionNumber, version.Status,
        version.CreatedAt, version.PublishedAt, version.PublishedByUserId,
        version.Fields
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.FieldKey, StringComparer.Ordinal)
            .Select(SchemaFieldResponse.From)
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
    public static DocumentFieldsResponse From(Document document, DocumentTypeSchemaVersion? schema)
    {
        if (schema is null)
            return new DocumentFieldsResponse(document.DocumentId, document.SchemaVersionId, null, Array.Empty<DocumentFieldValueResponse>());

        var values = document.FieldValues.ToDictionary(value => value.DocumentFieldId);
        var fields = SchemaVersionResponse.From(schema).Fields
            .Select(field => new DocumentFieldValueResponse(
                field,
                values.TryGetValue(field.DocumentFieldId, out var value) ? FormatValue(field.DataType, value) : null))
            .ToList();

        return new DocumentFieldsResponse(document.DocumentId, schema.SchemaVersionId, schema.VersionNumber, fields);
    }

    /// <summary>Formats a stored value in the same text form <see cref="DocumentFieldValueInput"/> accepts.</summary>
    private static string? FormatValue(DocumentFieldDataType dataType, DocumentFieldValue value) => dataType switch
    {
        DocumentFieldDataType.Text or DocumentFieldDataType.LongText => value.TextValue,
        DocumentFieldDataType.Number => value.NumberValue?.ToString(CultureInfo.InvariantCulture),
        DocumentFieldDataType.Date => value.DateValue?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
        DocumentFieldDataType.Boolean => value.BoolValue is { } flag ? (flag ? "true" : "false") : null,
        DocumentFieldDataType.Select => value.OptionKey,
        _ => null
    };
}
