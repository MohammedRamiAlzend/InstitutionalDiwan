import { useMutation, useQueryClient } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import { brandingKeys } from '@/entities/branding'
import {
  institutionSettingKeys,
  type InstitutionSettingResponse,
} from '@/entities/institution-setting'
import type { UpdateInstitutionSettingsRequest } from '../model/types'

/** `PUT /api/institution-settings` — replaces the singleton settings row. */
export function useUpdateInstitutionSettingsMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (
      request: UpdateInstitutionSettingsRequest,
    ): Promise<InstitutionSettingResponse> => {
      const { data } = await httpClient.put<InstitutionSettingResponse>(
        '/institution-settings',
        request,
      )
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: institutionSettingKeys.all })
      // The name shown in the shell and on the sign-in page comes from the branding read.
      void queryClient.invalidateQueries({ queryKey: brandingKeys.all })
    },
  })
}
