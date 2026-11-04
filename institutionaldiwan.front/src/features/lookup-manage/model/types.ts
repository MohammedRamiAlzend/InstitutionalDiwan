/** `POST /api/lookups/{lookup}` payload. */
export interface CreateLookupEntryRequest {
  id: number
  desc: string
  descAr: string
}

/** `PUT /api/lookups/{lookup}/{id}` payload; the id is immutable. */
export interface RelabelLookupEntryRequest {
  desc: string
  descAr: string
}
