import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { useAttachmentEditConfig, type DocumentFileResponse } from '@/entities/document-file'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { Label } from '@/shared/ui'
import { loadOnlyOfficeApi } from '../../drive-editor/api/onlyOfficeLoader'

interface AttachmentEditorProps {
  file: DocumentFileResponse
  open: boolean
  onRequestClose: () => void
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

export function AttachmentEditor({ file, open, onRequestClose }: AttachmentEditorProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const editorRef = useRef<{ destroy?: () => void } | null>(null)
  const [loadError, setLoadError] = useState<Error | null>(null)
  const editConfig = useAttachmentEditConfig(file.fileId, { enabled: open })

  useEffect(() => {
    if (!open || !editConfig.data || !containerRef.current) return
    let cancelled = false
    setLoadError(null)

    void loadOnlyOfficeApi(editConfig.data.serverUrl)
      .then((api) => {
        if (cancelled || !containerRef.current) return
        const sourceConfig = editConfig.data?.config ?? {}
        const sourceEvents = isRecord(sourceConfig.events) ? sourceConfig.events : {}
        editorRef.current = new api.DocEditor(containerRef.current.id, {
          ...sourceConfig,
          events: {
            ...sourceEvents,
            onError: () => toast.error(t('attachment.editor.save-failed')),
            onRequestClose,
          },
        })
      })
      .catch((error: unknown) => {
        if (!cancelled) setLoadError(error instanceof Error ? error : new Error(String(error)))
      })

    return () => {
      cancelled = true
      editorRef.current?.destroy?.()
      editorRef.current = null
    }
  }, [editConfig.data, file.fileId, onRequestClose, open])

  if (editConfig.isLoading) {
    return <p className="flex h-full min-h-[60vh] items-center justify-center text-sm text-muted"><Label k="attachment.editor.loading" /></p>
  }
  if (editConfig.error) {
    return <p className="flex h-full min-h-[60vh] items-center justify-center text-sm text-danger">{toArabicErrorMessage(editConfig.error)}</p>
  }
  if (loadError) {
    return <p className="flex h-full min-h-[60vh] items-center justify-center text-sm text-danger"><Label k="attachment.editor.not-configured" /></p>
  }

  return <div ref={containerRef} id={`attachment-editor-${file.fileId}`} className="h-full min-h-[60vh] w-full" />
}
