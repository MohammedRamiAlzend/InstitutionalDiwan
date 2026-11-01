import { useMemo, useRef, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ShieldCheck, Upload } from 'lucide-react'
import { toast } from 'sonner'
import {
  ChecksumBadge,
  currentVersionOf,
  FileArchivedBadge,
  type DocumentFileResponse,
} from '@/entities/document-file'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { fileTypeLimits } from '@/shared/config'
import { formatFileSize } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Textarea } from '@/shared/ui'
import { useUploadVersionMutation } from '../api/attachmentMutations'
import { buildUploadVersionSchema, type UploadVersionFormValues } from '../model/upload.schema'
import { useLocalChecksum } from '../model/useLocalChecksum'
import { FilePickSummary } from './FilePickSummary'

interface UploadVersionDialogProps {
  file: DocumentFileResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

const DEFAULT_VALUES: UploadVersionFormValues = { file: null, changeNotes: '' }

/**
 * The next immutable version of one file. The type, title, and primary-scan flag stay as they are, so
 * only the bytes and the change notes are asked for; the version number is assigned by the server and
 * shown here beforehand as `currentVersionNumber + 1`.
 */
export function UploadVersionDialog({
  file,
  open,
  onOpenChange,
}: UploadVersionDialogProps) {
  const uploadMutation = useUploadVersionMutation(file.fileId)
  const checksum = useLocalChecksum()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [progress, setProgress] = useState<number | null>(null)
  const [outcome, setOutcome] = useState<{ versionNumber: number; checksumState: boolean | null } | null>(null)

  // The file's own type carries the limits, and it never changes here, so the schema is built once.
  const schema = useMemo(() => buildUploadVersionSchema(file.fileTypeId), [file.fileTypeId])

  const {
    control,
    register,
    handleSubmit,
    trigger,
    reset,
    formState: { errors },
  } = useForm<UploadVersionFormValues>({ resolver: zodResolver(schema), defaultValues: DEFAULT_VALUES })

  const selectedFile = useWatch({ control, name: 'file' })
  const limits = fileTypeLimits(file.fileTypeId)
  const nextVersionNumber = file.currentVersionNumber + 1

  const startOver = () => {
    if (fileInputRef.current) fileInputRef.current.value = ''
    reset(DEFAULT_VALUES)
    checksum.clear()
    setProgress(null)
    setOutcome(null)
  }

  const closeDialog = () => {
    startOver()
    onOpenChange(false)
  }

  const selectFile = (value: File | null, field: { onChange: (next: File | null) => void }) => {
    field.onChange(value)
    setOutcome(null)
    setProgress(null)
    void checksum.compute(value)
    void trigger('file')
  }

  const onSubmit = handleSubmit(async (values) => {
    if (!values.file) return

    setProgress(0)

    try {
      const updated = await uploadMutation.mutateAsync({
        file: values.file,
        changeNotes: values.changeNotes.trim(),
        onProgress: setProgress,
      })
      const version = currentVersionOf(updated)
      const checksumState =
        checksum.digest && version ? version.checksumSha256.toLowerCase() === checksum.digest : null

      setOutcome({ versionNumber: version?.versionNumber ?? updated.currentVersionNumber, checksumState })

      if (checksumState === false) {
        toast.warning(
          `رُفع الإصدار ${updated.currentVersionNumber} من «${file.title}»، لكن بصمته المخزنة لا تطابق البصمة المحلية.`
        )
      } else if (checksumState === true) {
        toast.success(
          `رُفع الإصدار ${updated.currentVersionNumber} من «${file.title}» وبصمته مطابقة للنسخة المخزنة.`
        )
      } else {
        toast.success(
          `رُفع الإصدار ${updated.currentVersionNumber} من «${file.title}» (${formatFileSize(values.file.size)}).`
        )
      }
    } catch {
      // The interceptor already normalized the failure; it is rendered in the form banner below.
      setProgress(null)
    }
  })

  const isBusy = uploadMutation.isPending || checksum.isHashing

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) closeDialog()
      }}
      title="رفع إصدار جديد"
      description={`يضيف إصدارًا إلى «${file.title}». الإصدار الحالي ${file.currentVersionNumber} يليه ${nextVersionNumber}؛ النوع والعنوان لا يتغيران.`}
      size="md"
      footer={
        outcome ? (
          <Button type="button" onClick={closeDialog}>
            إغلاق
          </Button>
        ) : (
          <>
            <Button type="button" variant="outline" onClick={closeDialog} disabled={isBusy}>
              إلغاء
            </Button>
            <Button type="submit" form="attachment-version-form" loading={uploadMutation.isPending}>
              <Upload className="size-4" aria-hidden />
              رفع الإصدار {nextVersionNumber}
            </Button>
          </>
        )
      }
    >
      {file.isArchived ? (
        <div className="flex flex-col gap-2 rounded-lg border border-border bg-surface-muted/30 p-3">
          <FileArchivedBadge isArchived />
          <p className="text-sm text-foreground">
            هذا الملف مؤرشف، فلا يقبل إصدارات جديدة (Attachments.FileArchived). إصداراته الحالية تبقى
            قابلة للقراءة والتنزيل.
          </p>
        </div>
      ) : outcome ? (
        <div className="flex flex-col gap-3 rounded-lg border border-border bg-surface-muted/30 p-3">
          <p className="text-sm font-medium text-foreground">
            رُفع الإصدار {outcome.versionNumber} من «{file.title}».
          </p>
          <div className="flex flex-wrap items-center gap-2">
            <ChecksumBadge isMatch={outcome.checksumState} />
            {outcome.checksumState === null && (
              <span className="text-xs text-muted">
                لم تُحسب بصمة محلية لهذا الملف، فلم تُقارن بالنسخة المخزنة.
              </span>
            )}
          </div>
        </div>
      ) : (
        <form
          id="attachment-version-form"
          onSubmit={onSubmit}
          noValidate
          className="flex flex-col gap-4"
        >
          <p className="flex items-center gap-1.5 text-xs text-muted">
            <span>نوع الملف:</span>
            <LookupLabel lookup={LOOKUP_SLUGS.DocumentFileTypes} id={file.fileTypeId} />
          </p>

          {limits ? (
            <p className="flex items-start gap-1.5 text-xs text-muted">
              <ShieldCheck className="mt-0.5 size-3.5 shrink-0" aria-hidden />
              <span>
                الحد الأقصى للحجم {formatFileSize(limits.maxBytes)}؛ الامتدادات المسموحة:{' '}
                <span dir="ltr">{limits.allowedExtensions.join('، ')}</span>.
              </span>
            </p>
          ) : (
            <p className="text-xs text-muted">
              لا حدود معلومة محليًا لنوع هذا الملف؛ يتحقق منه الخادم عند الرفع.
            </p>
          )}

          <FormField label="الملف" required error={errors.file?.message}>
            <Controller
              control={control}
              name="file"
              render={({ field }) => (
                <Input
                  ref={fileInputRef}
                  id="attachment-version-file"
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

          {selectedFile && <FilePickSummary file={selectedFile} checksum={checksum} />}

          <FormField label="ملاحظات التغيير" error={errors.changeNotes?.message}>
            <Textarea
              id="attachment-version-notes"
              rows={2}
              placeholder="مثال: إعادة مسح بدقة أعلى…"
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
                aria-label="تقدّم رفع الإصدار"
                className="h-2 w-full overflow-hidden rounded-full bg-surface-muted"
              >
                <div
                  className="h-full rounded-full bg-accent transition-[width] duration-200"
                  style={{ width: `${progress ?? 0}%` }}
                />
              </div>
              <p className="text-xs text-muted">
                {progress === null
                  ? 'جارٍ تجهيز الطلب…'
                  : progress < 100
                    ? `جارٍ رفع الملف… ${progress}%`
                    : 'رُفعته، وجارٍ التحقق منه وحفظه…'}
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
