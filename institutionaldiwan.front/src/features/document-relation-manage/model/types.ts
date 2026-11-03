/** `CreateDocumentRelationRequest` — a link from the route document to `targetDocumentId`. */
export interface CreateDocumentRelationRequest {
  targetDocumentId: string
  relationTypeId: number
  notes?: string
}

/** `RemoveDocumentRelationRequest` — the reason is kept in the permanent relation history. */
export interface RemoveDocumentRelationRequest {
  reason: string
}
