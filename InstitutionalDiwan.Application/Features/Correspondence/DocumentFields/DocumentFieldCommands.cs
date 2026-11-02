using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

/// <summary>
/// Defines a new dynamic field (its stable key and data type) for a document type. The field only
/// appears on documents once it is added to a schema draft and that draft is published.
/// </summary>
public sealed record CreateDocumentFieldCommand(
    Guid UserId,
    int DocumentTypeId,
    string Key,
    DocumentFieldDataType DataType) : ICommand<DocumentFieldResponse>;

/// <summary>
/// Starts the editable schema draft of a document type, as a copy of the published version when
/// there is one. A type has at most one draft.
/// </summary>
public sealed record CreateSchemaDraftCommand(Guid UserId, int DocumentTypeId) : ICommand<SchemaVersionResponse>;

/// <summary>Throws away an unpublished draft; published versions cannot be discarded.</summary>
public sealed record DiscardSchemaDraftCommand(Guid SchemaVersionId) : ICommand;

/// <summary>Adds an existing field to a schema draft with its settings.</summary>
public sealed record AddFieldToDraftCommand(
    Guid SchemaVersionId,
    Guid DocumentFieldId,
    DocumentFieldSettingsInput Settings) : ICommand<SchemaVersionResponse>;

/// <summary>Replaces the settings of a field already in a schema draft; its data type never changes.</summary>
public sealed record UpdateDraftFieldCommand(
    Guid SchemaVersionId,
    Guid DocumentFieldId,
    DocumentFieldSettingsInput Settings) : ICommand<SchemaVersionResponse>;

/// <summary>Removes a field from a schema draft; the field and any stored values are untouched.</summary>
public sealed record RemoveDraftFieldCommand(Guid SchemaVersionId, Guid DocumentFieldId) : ICommand<SchemaVersionResponse>;

/// <summary>
/// Freezes a schema draft and makes it the version new documents of the type are pinned to; the
/// previously published version is retired but stays readable for the documents already on it.
/// </summary>
public sealed record PublishSchemaVersionCommand(Guid SchemaVersionId, Guid UserId) : ICommand<SchemaVersionResponse>;

/// <summary>
/// Replaces the dynamic-field values of a draft document, validated against the schema version the
/// document is pinned to. A blank value clears that field.
/// </summary>
public sealed record SetDocumentFieldValuesCommand(
    Guid DocumentId,
    IReadOnlyList<DocumentFieldValueInput> Values) : ICommand<DocumentFieldsResponse>;
