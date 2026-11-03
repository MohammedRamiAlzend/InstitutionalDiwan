namespace InstitutionalDiwan.Server.Contracts.Relations;

/// <summary>Payload submitted to <c>POST /api/documents/{documentId}/relations</c>; the target and type are required.</summary>
public sealed record CreateDocumentRelationRequest(Guid TargetDocumentId, int RelationTypeId, string? Notes = null);

/// <summary>Payload submitted to <c>DELETE /api/documents/{documentId}/relations/{relationId}</c>; the reason is required.</summary>
public sealed record RemoveDocumentRelationRequest(string Reason);
