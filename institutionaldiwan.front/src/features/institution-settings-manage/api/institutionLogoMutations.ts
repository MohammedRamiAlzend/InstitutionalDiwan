import { useMutation, useQueryClient } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import { brandingKeys } from '@/entities/branding'
import {
  institutionSettingKeys,
  type InstitutionSettingResponse,
} from '@/entities/institution-setting'

function useRefreshAfterLogoChange() {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: institutionSettingKeys.all }),
      queryClient.invalidateQueries({ queryKey: brandingKeys.all }),
    ])
}

/** `PUT /api/institution-settings/logo` — uploads the image that becomes the institution logo. */
export function useUploadInstitutionLogoMutation() {
  const refresh = useRefreshAfterLogoChange()

  return useMutation({
    mutationFn: async (file: File): Promise<InstitutionSettingResponse> => {
      const form = new FormData()
      form.append('file', file)
      const { data } = await httpClient.put<InstitutionSettingResponse>(
        '/institution-settings/logo',
        form,
      )
      return data
    },
    onSuccess: () => void refresh(),
  })
}

/** `DELETE /api/institution-settings/logo` — removes the logo so the default mark returns. */
export function useRemoveInstitutionLogoMutation() {
  const refresh = useRefreshAfterLogoChange()

  return useMutation({
    mutationFn: async (): Promise<InstitutionSettingResponse> => {
      const { data } = await httpClient.delete<InstitutionSettingResponse>(
        '/institution-settings/logo',
      )
      return data
    },
    onSuccess: () => void refresh(),
  })
}
