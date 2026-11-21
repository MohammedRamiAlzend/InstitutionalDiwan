import { useRef, useState } from 'react'
import { CheckCircle2, Loader2, Upload, XCircle } from 'lucide-react'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import {
  DEFAULT_DRIVE_ALLOWED_EXTENSIONS,
  DEFAULT_DRIVE_MAX_FILE_BYTES,
  driveFileRejection,
} from '@/shared/config'
import { formatFileSize, t } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Label } from '@/shared/ui'
import {
  DriveUploadPutError,
  useUploadDriveFileMutation,
  type UploadStage,
} from '../api/driveUploadMutations'
import type { DriveUploadTarget } from '../model/types'

interface UploadDriveDialogProps {
  target: DriveUploadTarget
  /** The space's remaining quota, when known; adds a local pre-check (the server re-checks). */
  remainingQuotaBytes?: number | null
  /** Files dropped on the browser, seeded into the queue on open. */
  initialFiles?: File[]
  open: boolean
  onOpenChange: (open: boolean) => void
}

type EntryStatus = 'queued' | UploadStage | 'done' | 'error'

interface QueueEntry {
  id: string
  file: File
  rejection: string | null
  status: EntryStatus
  percent: number
  error: string | null
}

let entrySeq = 0

function buildEntries(files: File[], remainingQuotaBytes?: number | null): QueueEntry[] {
  return files.map((file) => ({
    id: `drive-upload-${entrySeq++}`,
    file,
    rejection: driveFileRejection(file, remainingQuotaBytes),
    status: 'queued',
    percent: 0,
    error: null,
  }))
}

/**
 * Presigned direct-to-storage upload (Module 11). Picks one or many files — or a single new version
 * of an existing file when `target.fileItemId` is set — pre-checks each against the client mirror of
 * the Drive limits, then uploads them sequentially (initiate → PUT → complete) with a three-stage
 * progress bar per file. A failed or cancelled reservation is abandoned by the mutation.
 */
export function UploadDriveDialog({
  target,
  remainingQuotaBytes,
  initialFiles,
  open,
  onOpenChange,
}: UploadDriveDialogProps) {
  const isNewVersion = Boolean(target.fileItemId)
  const uploadMutation = useUploadDriveFileMutation()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const cancelledRef = useRef(false)

  const [queue, setQueue] = useState<QueueEntry[]>(() =>
    buildEntries(initialFiles ?? [], remainingQuotaBytes),
  )
  const [isRunning, setIsRunning] = useState(false)

  const patchEntry = (id: string, patch: Partial<QueueEntry>) =>
    setQueue((current) => current.map((entry) => (entry.id === id ? { ...entry, ...patch } : entry)))

  const resetAll = () => {
    cancelledRef.current = false
    if (fileInputRef.current) fileInputRef.current.value = ''
    setQueue([])
    setIsRunning(false)
    uploadMutation.reset()
  }

  const pickFiles = (files: File[]) => {
    const entries = buildEntries(files, remainingQuotaBytes)
    setQueue(isNewVersion ? entries.slice(0, 1) : entries)
  }

  const runQueue = async () => {
    cancelledRef.current = false
    setIsRunning(true)
    let doneCount = 0

    for (const entry of queue) {
      if (cancelledRef.current) break
      if (entry.rejection) {
        patchEntry(entry.id, { status: 'error', error: entry.rejection })
        continue
      }

      patchEntry(entry.id, { status: 'preparing', percent: 0, error: null })
      try {
        await uploadMutation.mutateAsync({
          target,
          file: entry.file,
          onStage: (stage) => patchEntry(entry.id, { status: stage }),
          onProgress: (percent) => patchEntry(entry.id, { percent }),
        })
        patchEntry(entry.id, { status: 'done', percent: 100 })
        doneCount += 1
      } catch (error) {
        const message =
          error instanceof DriveUploadPutError
            ? t('drive.upload.put-failed')
            : toArabicErrorMessage(error)
        patchEntry(entry.id, { status: 'error', error: message })
      }
    }

    setIsRunning(false)
    if (doneCount > 0) {
      toast.success(t('drive.upload.summary', { count: doneCount }))
    }
  }

  const closeDialog = () => {
    if (isRunning) return
    resetAll()
    onOpenChange(false)
  }

  const cancelQueue = () => {
    cancelledRef.current = true
  }

  const hasPick = queue.length > 0
  const allRejected = hasPick && queue.every((entry) => entry.rejection)
  const finished = !isRunning && hasPick && queue.every((e) => e.status === 'done' || e.status === 'error')

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) closeDialog()
      }}
      title={t(isNewVersion ? 'drive.dialog.upload-new-version' : 'drive.dialog.upload-title')}
      description={t('drive.upload.description')}
      size="md"
      footer={
        finished ? (
          <>
            <Button type="button" variant="outline" onClick={resetAll}>
              <Label k="drive.upload.another" />
            </Button>
            <Button type="button" onClick={closeDialog}>
              <Label k="common.action.close" />
            </Button>
          </>
        ) : (
          <>
            <Button type="button" variant="outline" onClick={isRunning ? cancelQueue : closeDialog}>
              <Label k="common.action.cancel" />
            </Button>
            <Button
              type="button"
              onClick={() => void runQueue()}
              disabled={!hasPick || allRejected || isRunning}
              loading={isRunning}
            >
              <Upload className="size-4" aria-hidden />
              <Label k="drive.upload.submit" />
            </Button>
          </>
        )
      }
    >
      <div className="flex flex-col gap-4">
        {isNewVersion && (
          <p className="text-xs text-muted">
            <Label k="drive.upload.new-version-hint" params={{ name: target.fileName ?? '' }} />
          </p>
        )}

        <FormField labelKey="drive.upload.field-file" required>
          <Input
            ref={fileInputRef}
            id="drive-upload-file"
            type="file"
            dir="ltr"
            multiple={!isNewVersion}
            accept={DEFAULT_DRIVE_ALLOWED_EXTENSIONS.join(',')}
            disabled={isRunning}
            className="text-xs"
            onChange={(event) => pickFiles(Array.from(event.target.files ?? []))}
          />
        </FormField>

        <p className="text-xs text-muted">
          <Label
            k="drive.upload.limits-prefix"
            params={{ size: formatFileSize(DEFAULT_DRIVE_MAX_FILE_BYTES) }}
          />{' '}
          {DEFAULT_DRIVE_ALLOWED_EXTENSIONS.join('، ')}.
        </p>

        {hasPick && (
          <ul className="flex flex-col gap-2">
            {queue.map((entry) => (
              <li
                key={entry.id}
                className="flex flex-col gap-1 rounded-lg border border-border bg-surface-muted/30 p-2"
              >
                <div className="flex items-center gap-2">
                  <span className="min-w-0 flex-1 truncate text-sm text-foreground" title={entry.file.name}>
                    {entry.file.name}
                  </span>
                  <span dir="ltr" className="shrink-0 text-xs text-muted">
                    {formatFileSize(entry.file.size)}
                  </span>
                  {entry.status === 'done' && <CheckCircle2 className="size-4 shrink-0 text-success" aria-hidden />}
                  {entry.status === 'error' && <XCircle className="size-4 shrink-0 text-danger" aria-hidden />}
                  {(entry.status === 'preparing' || entry.status === 'uploading' || entry.status === 'verifying') && (
                    <Loader2 className="size-4 shrink-0 animate-spin text-accent" aria-hidden />
                  )}
                </div>

                {(entry.status === 'uploading' || entry.status === 'verifying' || entry.status === 'preparing') && (
                  <div
                    role="progressbar"
                    aria-valuenow={entry.percent}
                    aria-valuemin={0}
                    aria-valuemax={100}
                    aria-label={t('drive.upload.progress-aria')}
                    className="h-1.5 w-full overflow-hidden rounded-full bg-surface-muted"
                  >
                    <div
                      className="h-full rounded-full bg-accent transition-[width] duration-200"
                      style={{ width: `${entry.status === 'verifying' ? 100 : entry.percent}%` }}
                    />
                  </div>
                )}

                <p className="text-xs text-muted">
                  {entry.status === 'queued' && <Label k="drive.upload.status-queued" />}
                  {entry.status === 'preparing' && <Label k="drive.upload.preparing" />}
                  {entry.status === 'uploading' && (
                    <Label k="drive.upload.uploading" params={{ percent: entry.percent }} />
                  )}
                  {entry.status === 'verifying' && <Label k="drive.upload.verifying" />}
                  {entry.status === 'done' && <Label k="drive.upload.status-done" />}
                </p>

                {entry.error && (
                  <p role="alert" className="text-xs text-danger">
                    {entry.error}
                  </p>
                )}
              </li>
            ))}
          </ul>
        )}

        {!hasPick && (
          <p className="text-xs text-muted">
            <Label k="drive.upload.queue-empty" />
          </p>
        )}
      </div>
    </Dialog>
  )
}