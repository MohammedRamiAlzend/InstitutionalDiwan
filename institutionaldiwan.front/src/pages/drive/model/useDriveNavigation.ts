import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { isDriveView, type DriveView } from '@/entities/drive-item'

export interface DriveNavigation {
  view: DriveView
  /** The selected shared space (or an optional filter for starred/trash); null = resolve/default. */
  spaceId: string | null
  /** The open folder inside the active space; null = the space root. */
  folderId: string | null
  setView: (view: DriveView) => void
  setSpace: (spaceId: string | null) => void
  setFolder: (folderId: string | null) => void
}

/**
 * The Drive workspace state lives in the URL (`?view=&space=&folder=`) so a link restores the exact
 * place. Switching view or space resets the folder; `my-files` resolves to the personal space and
 * `starred`/`trash`/`deletion` span every reachable space unless a space is chosen.
 */
export function useDriveNavigation(): DriveNavigation {
  const [params, setParams] = useSearchParams()
  const viewParam = params.get('view')
  const view: DriveView = isDriveView(viewParam) ? viewParam : 'my-files'
  const spaceId = params.get('space')
  const folderId = params.get('folder')

  const setView = useCallback(
    (next: DriveView) => {
      setParams((prev) => {
        const nextParams = new URLSearchParams(prev)
        nextParams.set('view', next)
        nextParams.delete('folder')
        if (next !== 'shared' && next !== 'starred' && next !== 'trash') {
          nextParams.delete('space')
        }
        return nextParams
      })
    },
    [setParams],
  )

  const setSpace = useCallback(
    (next: string | null) => {
      setParams((prev) => {
        const nextParams = new URLSearchParams(prev)
        if (next) nextParams.set('space', next)
        else nextParams.delete('space')
        nextParams.delete('folder')
        return nextParams
      })
    },
    [setParams],
  )

  const setFolder = useCallback(
    (next: string | null) => {
      setParams((prev) => {
        const nextParams = new URLSearchParams(prev)
        if (next) nextParams.set('folder', next)
        else nextParams.delete('folder')
        return nextParams
      })
    },
    [setParams],
  )

  return useMemo(
    () => ({ view, spaceId, folderId, setView, setSpace, setFolder }),
    [view, spaceId, folderId, setView, setSpace, setFolder],
  )
}