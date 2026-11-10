using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>One selectable option of a Select field.</summary>
public sealed record DocumentFieldOptionRequest(string Key, string LabelAr);

/// <summary>
/// How a field behaves inside one schema version. Which members apply depends on the field's data
/// type (regex and max length for text, min/max for numbers, options for selects).
/// </summary>
public sealed record DocumentFieldSettingsRequest(
    string LabelAr,
    bool IsRequired = false,
    int DisplayOrder = 0,
    string? RegexPattern = null,
    string? RegexErrorMessageAr = null,
    decimal? MinNumber = null,
    decimal? MaxNumber = null,
    int? MaxLength = null,
    IReadOnlyList<DocumentFieldOptionRequest>? Options = null,
    DocumentFieldBehavior? Behavior = null);

/// <summary>
/// Payload submitted to <c>POST /api/document-types/{documentTypeId}/fields</c>. The data type is
/// the integer value of <c>DocumentFieldDataType</c> (1 Text, 2 LongText, 3 Number, 4 Date, 5 Boolean, 6 Select, 7 MultiSelect)
/// and can never change afterwards.
/// </summary>
public sealed record CreateDocumentFieldRequest(string Key, DocumentFieldDataType DataType, Guid? DepartmentId = null);

/// <summary>Payload submitted to <c>PUT /api/registry-visits/{visitId}/fields</c>; replaces all values of the open record.</summary>
public sealed record SetVisitRecordRequest(IReadOnlyList<DocumentFieldValueRequest> Values);

/// <summary>Payload submitted to <c>PUT /api/registry-visits/{visitId}/fields/correct</c>; replaces the values of a locked record.</summary>
public sealed record CorrectVisitRecordRequest(IReadOnlyList<DocumentFieldValueRequest> Values, string Reason);

/// <summary>Payload submitted to <c>POST /api/schema-versions/{schemaVersionId}/fields</c>.</summary>
public sealed record AddSchemaFieldRequest(Guid DocumentFieldId, DocumentFieldSettingsRequest Settings);

/// <summary>One dynamic-field value as text (numbers invariant, dates ISO 8601, booleans true/false, selects the option key, multi-selects the keys joined by commas).</summary>
public sealed record DocumentFieldValueRequest(string FieldKey, string? Value);

/// <summary>Payload submitted to <c>PUT /api/documents/{documentId}/fields</c>; replaces all values of the draft.</summary>
public sealed record SetDocumentFieldValuesRequest(IReadOnlyList<DocumentFieldValueRequest> Values);
