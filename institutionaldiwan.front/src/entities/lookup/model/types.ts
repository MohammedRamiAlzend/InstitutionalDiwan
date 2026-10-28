/** `LookupEntryResponse` from `/api/lookups/{lookup}` (camelCase JSON). */
export interface LookupEntryResponse {
  id: number
  desc: string
  descAr: string
  isActive: boolean
  createdAt: string
}

/** Seeded catalog row used as an offline/placeholder label source. */
export interface LookupCatalogEntry {
  id: number
  desc: string
  descAr: string
}
