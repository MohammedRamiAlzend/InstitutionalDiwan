using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

/// <summary>Every dynamic field defined for a document type, oldest first.</summary>
public sealed record ListDocumentFieldsQuery(int DocumentTypeId) : IQuery<IReadOnlyList<DocumentFieldResponse>>;

/// <summary>A document type's schema version history, newest first.</summary>
public sealed record ListSchemaVersionsQuery(int DocumentTypeId) : IQuery<IReadOnlyList<SchemaVersionSummaryResponse>>;

/// <summary>One schema version with its fields.</summary>
public sealed record GetSchemaVersionQuery(Guid SchemaVersionId) : IQuery<SchemaVersionResponse>;

/// <summary>The published schema new documents of a type are pinned to (none when the type has no fields).</summary>
public sealed record GetDocumentTypeSchemaQuery(int DocumentTypeId) : IQuery<DocumentTypeSchemaResponse>;

/// <summary>A document's pinned schema with the values it stores.</summary>
public sealed record GetDocumentFieldsQuery(Guid DocumentId) : IQuery<DocumentFieldsResponse>;
