import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { useDriveEditConfig, type DriveItemResponse } from '@/entities/drive-item'
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
  const editorRef = useRef<{ destroy?: () => void } | null>(null)
  const [loadError, setLoadError] = useState<Error | null>(null)
  const editConfig = useDriveEditConfig(item.itemId, { enabled: open })

  useEffect(() => {
    if (!open || !editConfig.data || !containerRef.current) return
    let cancelled = false

    void loadOnlyOfficeApi(editConfig.data.serverUrl)
      .then((api) => {
        if (cancelled || !containerRef.current) return
        const sourceConfig = editConfig.data?.config ?? {}
        const sourceEvents = isRecord(sourceConfig.events) ? sourceConfig.events : {}
        const config: Record<string, unknown> = {
          ...sourceConfig,
          events: {
            ...sourceEvents,
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
      editorRef.current?.destroy?.()
      editorRef.current = null
    }
  }, [editConfig.data, onRequestClose, open])

  if (editConfig.isLoading) {
    return <p className="flex min-h-[60vh] items-center justify-center text-sm text-muted"><Label k="drive.editor.loading" /></p>
  }
  if (editConfig.error) {
    return <p className="flex min-h-[60vh] items-center justify-center text-sm text-danger">{toArabicErrorMessage(editConfig.error)}</p>
  }
  if (loadError) {
    return <p className="flex min-h-[60vh] items-center justify-center text-sm text-danger"><Label k="drive.editor.not-configured" /></p>
  }

  return <div ref={containerRef} id={`drive-editor-${item.itemId}`} className="min-h-[60vh] w-full" />
}
