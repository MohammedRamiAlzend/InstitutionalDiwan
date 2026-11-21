import { useMutation, useQueryClient } from '@tanstack/react-query'
import { driveItemKeys, type DriveItemResponse } from '@/entities/drive-item'
import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type { CreateDriveFolderRequest, RenameDriveItemRequest } from '../model/types'

/** Structural writes (folder/rename/move) change browse, children and detail alike. */
function useItemInvalidation() {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: driveItemKeys.all })
  }
}

/** `POST /api/drive/items/folders` — a folder in a space (root when `parentId` is null). */
export function useCreateFolderMutation() {
  const invalidate = useItemInvalidation()
  return useMutation({
    mutationFn: async (request: CreateDriveFolderRequest): Promise<DriveItemResponse> => {
      const { data } = await httpClient.post<DriveItemResponse>('/drive/items/folders', request)
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PUT /api/drive/items/{itemId}/rename` — Editor access; sibling-unique name. */
export function useRenameItemMutation(itemId: string) {
  const invalidate = useItemInvalidation()
  return useMutation({
    mutationFn: async (request: RenameDriveItemRequest): Promise<DriveItemResponse> => {
      const { data } = await httpClient.put<DriveItemResponse>(
        `/drive/items/${itemId}/rename`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

export interface MoveItemVariables {
  itemId: string
  newParentId: string | null
}

/**
 * `PUT /api/drive/items/{itemId}/move` — cycle-safe; null destination is the space root. Takes the
 * item id as a variable so both the move dialog and drag-and-drop share one mutation instance.
 */
export function useMoveItemMutation() {
  const invalidate = useItemInvalidation()
  return useMutation({
    mutationFn: async ({ itemId, newParentId }: MoveItemVariables): Promise<DriveItemResponse> => {
      const { data } = await httpClient.put<DriveItemResponse>(`/drive/items/${itemId}/move`, {
        newParentId,
      })
      return data
    },
    onSuccess: invalidate,
  })
}

/** Paints the per-caller `starred` flag across every cached browse page and children list. */
function paintStarred(
  queryClient: ReturnType<typeof useQueryClient>,
  itemId: string,
  starred: boolean,
) {
  queryClient.setQueriesData<PagedResult<DriveItemResponse>>(
    { queryKey: [...driveItemKeys.all, 'browse'] },
    (old) =>
      old
        ? { ...old, items: old.items.map((it) => (it.itemId === itemId ? { ...it, starred } : it)) }
        : old,
  )
  queryClient.setQueriesData<DriveItemResponse[]>(
    { queryKey: [...driveItemKeys.all, 'children'] },
    (old) => (old ? old.map((it) => (it.itemId === itemId ? { ...it, starred } : it)) : old),
  )
}

export interface SetStarVariables {
  itemId: string
  starred: boolean
}

/**
 * `POST`/`DELETE /api/drive/items/{itemId}/star` — idempotent personal favorite. One variable-based
 * instance serves every row; the `starred` flag is painted optimistically across cached grids.
 */
export function useSetItemStarMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ itemId, starred }: SetStarVariables): Promise<DriveItemResponse> => {
      const { data } = starred
        ? await httpClient.post<DriveItemResponse>(`/drive/items/${itemId}/star`)
        : await httpClient.delete<DriveItemResponse>(`/drive/items/${itemId}/star`)
      return data
    },
    onMutate: ({ itemId, starred }) => paintStarred(queryClient, itemId, starred),
    onError: (_error, { itemId, starred }) => paintStarred(queryClient, itemId, !starred),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: driveItemKeys.all })
    },
  })
}