import { useMemo, useRef, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ShieldCheck, Upload } from 'lucide-react'
import { toast } from 'sonner'
import {
  ChecksumBadge,
  currentVersionOf,
  type DocumentFileResponse,
} from '@/entities/document-file'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { DOCUMENT_FILE_TYPE, fileTypeLimits } from '@/shared/config'
import { formatFileSize, t } from '@/shared/lib'
import { Button, Checkbox, Dialog, FormField, Input, Label, Select, Textarea } from '@/shared/ui'
import { useUploadInitialMutation } from '../api/attachmentMutations'
import {
  uploadAttachmentSchema,
  type UploadAttachmentFormValues,
} from '../model/upload.schema'
import { useLocalChecksum } from '../model/useLocalChecksum'
import { FilePickSummary } from './FilePickSummary'

interface UploadAttachmentDialogProps {
  documentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

interface UploadOutcome {
  title: string
  versionNumber: number
  /** `null` when no local checksum could be computed, so nothing was compared. */
  checksumState: boolean | null
}

const DEFAULT_VALUES: UploadAttachmentFormValues = {
  fileTypeId: String(DOCUMENT_FILE_TYPE.Scan),
  title: '',
  isPrimaryScan: false,
  changeNotes: '',
  file: null,
}

/** Shown after a successful upload: the stored checksum is compared with the one computed locally. */
function outcomeOf(created: DocumentFileResponse, localChecksum: string | null): UploadOutcome {
  const version = currentVersionOf(created)
  const checksumState =
    localChecksum && version ? version.checksumSha256.toLowerCase() === localChecksum : null

  return {
    title: created.title,
    versionNumber: version?.versionNumber ?? created.currentVersionNumber,
    checksumState,
  }
}

/**
 * Atomic first-version upload for one document: metadata plus bytes in a single request, so a
 * rejected file never leaves an empty version chain behind. The extension and size are pre-checked
 * against the client-side mirror of the seeded limits, and the local SHA-256 is compared with the
 * stored one afterwards, which makes the check cover the transfer as well as the storage.
 */
export function UploadAttachmentDialog({
  documentId,
  open,
  onOpenChange,
}: UploadAttachmentDialogProps) {
  const fileTypesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentFileTypes)
  const uploadMutation = useUploadInitialMutation(documentId)

  const [progress, setProgress] = useState<number | null>(null)
  const [outcome, setOutcome] = useState<UploadOutcome | null>(null)

  const checksum = useLocalChecksum()
  // A file input cannot be assigned a value, so clearing a pick means clearing the element itself.
  const fileInputRef = useRef<HTMLInputElement>(null)

  const {
    register,
    control,
    handleSubmit,
    trigger,
    reset,
    setValue,
    formState: { errors },
  } = useForm<UploadAttachmentFormValues>({
    resolver: zodResolver(uploadAttachmentSchema),
    defaultValues: DEFAULT_VALUES,
  })

  const selectedTypeId = useWatch({ control, name: 'fileTypeId' })
  const selectedFile = useWatch({ control, name: 'file' })
  const isPrimaryScan = useWatch({ control, name: 'isPrimaryScan' })

  const typeOptions = useMemo(
    () =>
      (fileTypesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [fileTypesQuery.data],
  )

  // `null` limits mean the server seeded a type this client does not know, so only the server checks.
  const limits = fileTypeLimits(Number(selectedTypeId))

  const resetResult = () => {
    setOutcome(null)
    setProgress(null)
  }

  const startOver = (fileTypeId: string) => {
    if (fileInputRef.current) fileInputRef.current.value = ''
    reset(DEFAULT_VALUES)
    setValue('fileTypeId', fileTypeId)
    checksum.clear()
    resetResult()
    // The dialog stays mounted while closed, so the last failure would otherwise greet the next open.
    uploadMutation.reset()
  }

  const selectFile = (file: File | null, field: { onChange: (value: File | null) => void }) => {
    field.onChange(file)
    resetResult()
    void checksum.compute(file)

    // Type and file are validated together, so a new pick must re-check the other one.
    void trigger(['file', 'fileTypeId'])
  }

  const onSubmit = handleSubmit(async (values) => {
    if (!values.file) return

    resetResult()
    setProgress(0)

    try {
      const created = await uploadMutation.mutateAsync({
        file: values.file,
        fileTypeId: Number(values.fileTypeId),
        title: values.title.trim(),
        isPrimaryScan: values.isPrimaryScan,
        changeNotes: values.changeNotes.trim(),
        onProgress: setProgress,
      })

      const result = outcomeOf(created, checksum.digest)
      setOutcome(result)

      if (result.checksumState === false) {
        toast.warning(
          t('attachment.upload.toast-mismatch', { title: result.title })
        )
      } else if (result.checksumState === true) {
        toast.success(t('attachment.upload.toast-match', { title: result.title }))
      } else {
        toast.success(t('attachment.upload.toast-done', { title: result.title, size: formatFileSize(values.file.size) }))
      }
    } catch {
      // The interceptor already normalized the failure; it is rendered in the form banner below.
      setProgress(null)
    }
  })

  const isBusy = uploadMutation.isPending || checksum.isHashing

  const closeDialog = () => {
    startOver(DEFAULT_VALUES.fileTypeId)
    onOpenChange(false)
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) closeDialog()
      }}
      title={t('attachment.upload.title')}
      description={t('attachment.upload.description')}
      size="md"
      footer={
        outcome ? (
          <>
            <Button
              type="button"
              variant="outline"
              onClick={() => startOver(selectedTypeId)}
            >
              <Label k="attachment.upload.another" />
            </Button>
            <Button type="button" onClick={closeDialog}>
              <Label k="common.action.close" />
            </Button>
          </>
        ) : (
          <>
            <Button type="button" variant="outline" onClick={closeDialog} disabled={isBusy}>
              <Label k="common.action.cancel" />
            </Button>
            <Button type="submit" form="attachment-upload-form" loading={uploadMutation.isPending}>
              <Upload className="size-4" aria-hidden />
              <Label k="attachment.upload.submit" />
            </Button>
          </>
        )
      }
    >
      {outcome ? (
        <div className="flex flex-col gap-3 rounded-lg border border-border bg-surface-muted/30 p-3">
          <p className="text-sm font-medium text-foreground">
            <Label k="attachment.upload.outcome" params={{ title: outcome.title, version: outcome.versionNumber }} />
          </p>
          <div className="flex flex-wrap items-center gap-2">
            <ChecksumBadge isMatch={outcome.checksumState} />
            {outcome.checksumState === null && (
              <span className="text-xs text-muted">
                <Label k="attachment.upload.no-local" />
              </span>
            )}
          </div>
          <p className="text-xs text-muted">
            <Label k="attachment.upload.auto-refresh" />
          </p>
        </div>
      ) : (
        <form id="attachment-upload-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
          <FormField labelKey="attachment.upload.field-type" required error={errors.fileTypeId?.message}>
            <Controller
              control={control}
              name="fileTypeId"
              render={({ field }) => (
                <Select
                  value={field.value || undefined}
                  onValueChange={(value) => {
                    field.onChange(value)
                    resetResult()
                    // Limits and the accepted extensions follow the type, so a picked file must be
                    // re-checked. Without a pick there is nothing to check: re-validating here would
                    // flag «اختر ملفًا للرفع» on a field the user has not used yet.
                    if (selectedFile) void trigger(['file', 'fileTypeId'])
                  }}
                  options={typeOptions}
                  disabled={isBusy || fileTypesQuery.isLoading}
                  invalid={Boolean(errors.fileTypeId)}
                  placeholder={fileTypesQuery.isLoading ? t('documents.create.loading') : t('documents.create.type-placeholder')}
                  aria-label={t('attachment.upload.field-type')}
                />
              )}
            />
          </FormField>

          {limits ? (
            <p className="flex items-start gap-1.5 text-xs text-muted">
              <ShieldCheck className="mt-0.5 size-3.5 shrink-0" aria-hidden />
              <span>
                <Label k="attachment.upload.limits-prefix" params={{ size: formatFileSize(limits.maxBytes) }} />{' '}
                {limits.allowedExtensions.join('، ')}.
              </span>
            </p>
          ) : (
            selectedTypeId && (
              <p className="text-xs text-muted">
                <Label k="attachment.upload.no-limits" />
              </p>
            )
          )}

          <FormField labelKey="attachment.upload.field-file" required error={errors.file?.message}>
            <Controller
              control={control}
              name="file"
              render={({ field }) => (
                <Input
                  ref={fileInputRef}
                  id="attachment-file"
                  type="file"
                  dir="ltr"
                  accept={limits?.allowedExtensions.join(',')}
                  onChange={(event) => selectFile(event.target.files?.[0] ?? null, field)}
                  disabled={isBusy}
                  invalid={Boolean(errors.file)}
                  className="text-xs"
                />
              )}
            />
          </FormField>

          {selectedFile && (
            <FilePickSummary file={selectedFile} checksum={checksum} />
          )}

          <FormField labelKey="attachment.upload.field-title" required error={errors.title?.message}>
            <Input
              id="attachment-title"
              placeholder={t('attachment.upload.title-placeholder')}
              invalid={Boolean(errors.title)}
              {...register('title')}
            />
          </FormField>

          <Controller
            control={control}
            name="isPrimaryScan"
            render={({ field }) => (
              <Checkbox
                checked={field.value}
                onCheckedChange={field.onChange}
                label={t('attachment.primary-scan')}
                description={
                  isPrimaryScan
                    ? t('attachment.upload.primary-on')
                    : t('attachment.upload.primary-off')
                }
                disabled={isBusy}
              />
            )}
          />

          <FormField labelKey="attachment.upload.field-notes" error={errors.changeNotes?.message}>
            <Textarea
              id="attachment-change-notes"
              rows={2}
              placeholder={t('attachment.upload.notes-placeholder')}
              invalid={Boolean(errors.changeNotes)}
              {...register('changeNotes')}
            />
          </FormField>

          {uploadMutation.isPending && (
            <div className="flex flex-col gap-1">
              <div
                role="progressbar"
                aria-valuenow={progress ?? 0}
                aria-valuemin={0}
                aria-valuemax={100}
                aria-label={t('attachment.upload.progress-aria')}
                className="h-2 w-full overflow-hidden rounded-full bg-surface-muted"
              >
                <div
                  className="h-full rounded-full bg-accent transition-[width] duration-200"
                  style={{ width: `${progress ?? 0}%` }}
                />
              </div>
              <p className="text-xs text-muted">
                {progress === null
                  ? t('attachment.upload.preparing')
                  : progress < 100
                    ? t('attachment.upload.uploading', { progress })
                    : t('attachment.upload.verifying')}
              </p>
            </div>
          )}

          {uploadMutation.error && (
            <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
              {toArabicErrorMessage(uploadMutation.error)}
            </p>
          )}
        </form>
      )}
    </Dialog>
  )
}
