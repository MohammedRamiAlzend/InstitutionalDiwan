import { useEffect, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { driveItemKeys, useDriveEditConfig, type DriveItemResponse } from '@/entities/drive-item'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { Label } from '@/shared/ui'
import { loadOnlyOfficeApi } from '../api/onlyOfficeLoader'

interface DriveEditorProps {
  item: DriveItemResponse
  open: boolean
  onRequestClose: () => void
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

export function DriveEditor({ item, open, onRequestClose }: DriveEditorProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const editorRef = useRef<{ destroy?: () => void; serviceCommand?: (command: string) => void } | null>(null)
  const [loadError, setLoadError] = useState<Error | null>(null)
  const queryClient = useQueryClient()
  const editConfig = useDriveEditConfig(item.itemId, { enabled: open })

  useEffect(() => {
    if (!open || !editConfig.data || !containerRef.current) return
    let cancelled = false
    // OnlyOffice answers a save with a server callback that commits a new version; a forcesave on
    // close makes that callback fire for a document the user just edited and then closed.
    let modified = false

    void loadOnlyOfficeApi(editConfig.data.serverUrl)
      .then((api) => {
        if (cancelled || !containerRef.current) return
        const sourceConfig = editConfig.data?.config ?? {}
        const sourceEvents = isRecord(sourceConfig.events) ? sourceConfig.events : {}
        const config: Record<string, unknown> = {
          ...sourceConfig,
          events: {
            ...sourceEvents,
            onDocumentStateChange: (event: unknown) => {
              modified = isRecord(event) ? Boolean(event.data) : Boolean(event)
            },
            onError: () => {
              toast.error(t('drive.editor.save-failed'))
            },
            onRequestClose,
          },
        }
        editorRef.current = new api.DocEditor(containerRef.current.id, config)
      })
      .catch((error: unknown) => {
        if (!cancelled) setLoadError(error instanceof Error ? error : new Error(String(error)))
      })

    return () => {
      cancelled = true
      if (modified) {
        try {
          editorRef.current?.serviceCommand?.('forcesave')
        } catch {
          // The editor is going away; a failed forcesave must not block the close.
        }
      }
      editorRef.current?.destroy?.()
      editorRef.current = null
      // The save callback commits asynchronously, so refresh the grid and version history shortly after.
      window.setTimeout(() => {
        void queryClient.invalidateQueries({ queryKey: driveItemKeys.all })
      }, 3000)
    }
  }, [editConfig.data, onRequestClose, open, queryClient])

  if (editConfig.isLoading) {
    return <p className="flex h-full min-h-[60vh] items-center justify-center text-sm text-muted"><Label k="drive.editor.loading" /></p>
  }
  if (editConfig.error) {
    return <p className="flex h-full min-h-[60vh] items-center justify-center text-sm text-danger">{toArabicErrorMessage(editConfig.error)}</p>
  }
  if (loadError) {
    return <p className="flex h-full min-h-[60vh] items-center justify-center text-sm text-danger"><Label k="drive.editor.not-configured" /></p>
  }

  return <div ref={containerRef} id={`drive-editor-${item.itemId}`} className="h-full min-h-[60vh] w-full" />
}
