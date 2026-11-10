using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

// A null DepartmentId means the type's creation form; a department id means that department's record form.

/// <summary>Every dynamic field defined for one form of a document type, oldest first.</summary>
public sealed record ListDocumentFieldsQuery(int DocumentTypeId, Guid? DepartmentId = null) : IQuery<IReadOnlyList<DocumentFieldResponse>>;

/// <summary>One form's schema version history, newest first.</summary>
public sealed record ListSchemaVersionsQuery(int DocumentTypeId, Guid? DepartmentId = null) : IQuery<IReadOnlyList<SchemaVersionSummaryResponse>>;

/// <summary>One schema version with its fields.</summary>
public sealed record GetSchemaVersionQuery(Guid SchemaVersionId) : IQuery<SchemaVersionResponse>;

/// <summary>
/// The published schema of a form: the one new documents of a type are pinned to, or the record form
/// a department fills when it receives one (none when the form has no published fields).
/// </summary>
public sealed record GetDocumentTypeSchemaQuery(int DocumentTypeId, Guid? DepartmentId = null) : IQuery<DocumentTypeSchemaResponse>;

/// <summary>A document's pinned schema with the values it stores.</summary>
public sealed record GetDocumentFieldsQuery(Guid DocumentId) : IQuery<DocumentFieldsResponse>;
