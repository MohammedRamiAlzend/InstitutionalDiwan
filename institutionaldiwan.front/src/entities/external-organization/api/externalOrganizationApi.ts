import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type {
  ExternalOrganizationListParams,
  ExternalOrganizationResponse,
} from '../model/types'

/** Typed read calls for external correspondents (requires `DOCUMENT_VIEW`). */
export const externalOrganizationApi = {
  list: async (
    params: ExternalOrganizationListParams,
  ): Promise<PagedResult<ExternalOrganizationResponse>> => {
    const { data } = await httpClient.get<PagedResult<ExternalOrganizationResponse>>(
      '/external-organizations',
      {
        params: {
          page: params.page,
          pageSize: params.pageSize,
          search: params.search?.trim() || undefined,
          includeInactive: params.includeInactive,
        },
      },
    )
    return data
  },

  byId: async (externalOrganizationId: string): Promise<ExternalOrganizationResponse> => {
    const { data } = await httpClient.get<ExternalOrganizationResponse>(
      `/external-organizations/${externalOrganizationId}`,
    )
    return data
  },
}
