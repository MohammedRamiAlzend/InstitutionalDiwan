/** `BrandingResponse` from the anonymous `GET /api/branding` (camelCase JSON). */
export interface BrandingResponse {
  institutionNameAr: string
  institutionNameEn: string | null
  /** When the uploaded logo last changed; `null` while there is none. Doubles as the cache version. */
  logoUpdatedAt: string | null
}
