import type { PagedQueryParams } from '@/shared/types'

/** `ExternalOrganizationResponse` — an external correspondent (ministry, company, individual). */
export interface ExternalOrganizationResponse {
  externalOrganizationId: string
  code: string
  nameAr: string
  nameEn: string | null
  organizationTypeId: number
  contactInfo: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

/** Query params for the paged external organization list. */
export interface ExternalOrganizationListParams extends PagedQueryParams {
  includeInactive?: boolean
}
