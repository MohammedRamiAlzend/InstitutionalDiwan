/** Body of `POST /api/documents/{documentId}/access-grants` (`GrantDocumentAccessRequest`). */
export interface GrantDocumentAccessRequest {
  userId: string
  accessLevelId: number
  reason?: string | null
  /** ISO date (`yyyy-MM-dd`); the server rejects a value that is not in the future. */
  expiresAt?: string | null
}
