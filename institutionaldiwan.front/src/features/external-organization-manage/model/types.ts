/** `POST /api/external-organizations` payload; empty optional text is sent as `null`. */
export interface CreateExternalOrganizationRequest {
  code: string
  nameAr: string
  organizationTypeId: number
  nameEn: string | null
  contactInfo: string | null
}

/** `PUT /api/external-organizations/{id}` payload; the code is immutable. */
export interface UpdateExternalOrganizationRequest {
  nameAr: string
  organizationTypeId: number
  nameEn: string | null
  contactInfo: string | null
}
