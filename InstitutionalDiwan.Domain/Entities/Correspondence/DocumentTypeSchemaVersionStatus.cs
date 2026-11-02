namespace InstitutionalDiwan.Domain.Entities.Correspondence;

/// <summary>
/// Lifecycle of a document type's field schema version: <c>Draft</c> (editable, at most one per type),
/// <c>Published</c> (frozen, the version new documents are pinned to) and <c>Retired</c> (frozen,
/// kept readable for the documents already pinned to it).
/// </summary>
public enum DocumentTypeSchemaVersionStatus
{
    Draft = 1,
    Published = 2,
    Retired = 3
}
