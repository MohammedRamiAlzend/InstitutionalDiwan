import { useState } from 'react'
import { Loader2, Save } from 'lucide-react'
import { toast } from 'sonner'
import {
  useDriveEffectiveRole,
  useDriveTextContent,
  type DriveItemResponse,
} from '@/entities/drive-item'
import { toArabicErrorMessage } from '@/shared/api'
import { DRIVE_ROLE } from '@/shared/config'
import { t } from '@/shared/lib'
import { Button, Label } from '@/shared/ui'
import { useSaveDriveTextMutation } from '../api/driveTextMutations'

interface TextFileEditorProps {
  item: DriveItemResponse
  open: boolean
}

/**
 * In-app editor for plain-text and Markdown Drive files, so .md does not depend on OnlyOffice.
 * Every save posts the whole document to `PUT /api/drive/files/{id}/text`, which stores it as a new
 * immutable version, and the previous versions stay available in the history panel. Viewers get the
 * same surface read-only. The server re-checks the role and the size.
 */
export function TextFileEditor({ item, open }: TextFileEditorProps) {
  const contentQuery = useDriveTextContent(item.itemId, { enabled: open })
  const roleQuery = useDriveEffectiveRole(item.itemId, { enabled: open })
  const saveMutation = useSaveDriveTextMutation()
  const canEdit = (roleQuery.data?.roleId ?? DRIVE_ROLE.None) >= DRIVE_ROLE.Editor

  // The draft is keyed by item, so switching files discards it during render — no effect needed.
  const [draft, setDraft] = useState<{ itemId: string; value: string } | null>(null)

  const original = contentQuery.data ?? ''
  const activeDraft = draft?.itemId === item.itemId ? draft.value : null
  const value = activeDraft ?? original
  const isDirty = activeDraft !== null && activeDraft !== original

  const handleSave = async () => {
    if (!isDirty || saveMutation.isPending) return
    try {
      await saveMutation.mutateAsync({ itemId: item.itemId, content: value })
      toast.success(t('drive.text.saved'))
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  if (roleQuery.isLoading || contentQuery.isLoading) {
    return (
      <p className="flex h-full min-h-[60vh] items-center justify-center gap-2 text-sm text-muted">
        <Loader2 className="size-5 animate-spin" aria-hidden />
        <Label k="drive.text.loading" />
      </p>
    )
  }
  if (contentQuery.error || roleQuery.error) {
    const error = contentQuery.error ?? roleQuery.error
    return (
      <p className="flex h-full min-h-[60vh] items-center justify-center text-sm text-danger">
        {toArabicErrorMessage(error)}
      </p>
    )
  }

  return (
    <div className="flex h-full min-h-[60vh] w-full flex-col gap-2">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-xs text-muted">
          {canEdit ? <Label k="drive.text.hint" /> : <Label k="drive.editor.view-only" />}
        </p>
        {canEdit && (
          <div className="flex items-center gap-2">
            {isDirty && (
              <span className="text-xs text-muted">
                <Label k="drive.text.unsaved" />
              </span>
            )}
            <Button
              type="button"
              size="sm"
              onClick={() => void handleSave()}
              disabled={!isDirty || saveMutation.isPending}
              loading={saveMutation.isPending}
            >
              <Save className="size-4" aria-hidden />
              <Label k="drive.text.save" />
            </Button>
          </div>
        )}
      </div>

      <textarea
        dir="auto"
        value={value}
        readOnly={!canEdit}
        onChange={(event) => setDraft({ itemId: item.itemId, value: event.target.value })}
        aria-label={item.name}
        spellCheck={false}
        className="min-h-72 w-full flex-1 resize-none rounded-lg border border-border bg-surface p-3 font-mono text-sm leading-6 text-foreground focus:border-focus-ring focus:outline-none"
      />
    </div>
  )
}
