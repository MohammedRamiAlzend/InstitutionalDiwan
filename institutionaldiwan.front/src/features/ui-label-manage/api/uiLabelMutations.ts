import { useMutation, useQueryClient } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import { useLabelsStore, type UiLabelSnapshot } from '@/shared/lib'
import { uiLabelKeys, type UiLabelOverridesResponse } from '@/entities/ui-label'
import type {
  SetUiLabelOverrideRequest,
  SetUiLabelOverrideVariables,
} from '../model/types'

/** Both writes return the full override set, so the store is confirmed from the response. */
function useConfirmLabels() {
  const queryClient = useQueryClient()
  return (data: UiLabelOverridesResponse) => {
    useLabelsStore.getState().applySnapshot(data)
    void queryClient.invalidateQueries({ queryKey: uiLabelKeys.overrides() })
  }
}

function snapshotOf(): UiLabelSnapshot {
  const { version, overrides } = useLabelsStore.getState()
  return { version, overrides }
}

/**
 * `PUT /api/ui-labels/{key}` — upserts one override. Applies optimistically for an instant
 * repaint, confirms from the authoritative response, and rolls back on failure. The server
 * guards the write to the superadmin account (403 for anyone else).
 */
export function useSetUiLabelMutation() {
  const confirm = useConfirmLabels()
  return useMutation({
    mutationFn: async ({ key, value }: SetUiLabelOverrideVariables) => {
      const { data } = await httpClient.put<UiLabelOverridesResponse>(
        `/ui-labels/${encodeURIComponent(key)}`,
        { value } satisfies SetUiLabelOverrideRequest,
      )
      return data
    },
    onMutate: ({ key, value }) => {
      const previous = snapshotOf()
      useLabelsStore
        .getState()
        .applySnapshot({ version: previous.version, overrides: { ...previous.overrides, [key]: value } })
      return { previous }
    },
    onSuccess: confirm,
    onError: (_error, _variables, context) => {
      if (context?.previous) useLabelsStore.getState().applySnapshot(context.previous)
    },
  })
}

/**
 * `DELETE /api/ui-labels/{key}` — idempotent reset to the built-in default. Same optimistic
 * apply/confirm/rollback shape as the upsert.
 */
export function useResetUiLabelMutation() {
  const confirm = useConfirmLabels()
  return useMutation({
    mutationFn: async (key: string) => {
      const { data } = await httpClient.delete<UiLabelOverridesResponse>(
        `/ui-labels/${encodeURIComponent(key)}`,
      )
      return data
    },
    onMutate: (key) => {
      const previous = snapshotOf()
      const overrides = { ...previous.overrides }
      delete overrides[key]
      useLabelsStore.getState().applySnapshot({ version: previous.version, overrides })
      return { previous }
    },
    onSuccess: confirm,
    onError: (_error, _key, context) => {
      if (context?.previous) useLabelsStore.getState().applySnapshot(context.previous)
    },
  })
}