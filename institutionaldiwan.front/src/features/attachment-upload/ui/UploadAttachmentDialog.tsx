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
import { formatFileSize } from '@/shared/lib'
import { Button, Checkbox, Dialog, FormField, Input, Select, Textarea } from '@/shared/ui'
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
          `رُفع الملف «${result.title}»، لكن بصمته المخزنة لا تطابق البصمة المحلية؛ تحقق من الملف.`
        )
      } else if (result.checksumState === true) {
        toast.success(`رُفع الملف «${result.title}» وبصمته مطابقة للنسخة المخزنة.`)
      } else {
        toast.success(`رُفع الملف «${result.title}» (${formatFileSize(values.file.size)}).`)
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
      title="رفع ملف للوثيقة"
      description="يُنشئ الرفع ملفًا جديدًا بإصدار أول في طلب واحد: يرفض الخادم الامتداد أو الحجم غير المسموحين قبل حفظ أي شيء."
      size="md"
      footer={
        outcome ? (
          <>
            <Button
              type="button"
              variant="outline"
              onClick={() => startOver(selectedTypeId)}
            >
              رفع ملف آخر
            </Button>
            <Button type="button" onClick={closeDialog}>
              إغلاق
            </Button>
          </>
        ) : (
          <>
            <Button type="button" variant="outline" onClick={closeDialog} disabled={isBusy}>
              إلغاء
            </Button>
            <Button type="submit" form="attachment-upload-form" loading={uploadMutation.isPending}>
              <Upload className="size-4" aria-hidden />
              رفع الملف
            </Button>
          </>
        )
      }
    >
      {outcome ? (
        <div className="flex flex-col gap-3 rounded-lg border border-border bg-surface-muted/30 p-3">
          <p className="text-sm font-medium text-foreground">
            رُفع الملف «{outcome.title}» كإصدار {outcome.versionNumber}.
          </p>
          <div className="flex flex-wrap items-center gap-2">
            <ChecksumBadge isMatch={outcome.checksumState} />
            {outcome.checksumState === null && (
              <span className="text-xs text-muted">
                لم تُحسب بصمة محلية لهذا الملف، فلم تُقارن بالنسخة المخزنة.
              </span>
            )}
          </div>
          <p className="text-xs text-muted">
            تظهر النسخة الجديدة في قائمة الملفات بعد تحديثها تلقائيًا.
          </p>
        </div>
      ) : (
        <form id="attachment-upload-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
          <FormField label="نوع الملف" required error={errors.fileTypeId?.message}>
            <Controller
              control={control}
              name="fileTypeId"
              render={({ field }) => (
                <Select
                  value={field.value || undefined}
                  onValueChange={(value) => {
                    field.onChange(value)
                    resetResult()
                    // Limits and the accepted extensions follow the type, so re-check the picked file.
                    void trigger(['file', 'fileTypeId'])
                  }}
                  options={typeOptions}
                  disabled={isBusy || fileTypesQuery.isLoading}
                  invalid={Boolean(errors.fileTypeId)}
                  placeholder={fileTypesQuery.isLoading ? 'جارٍ التحميل…' : 'اختر النوع…'}
                  aria-label="نوع الملف"
                />
              )}
            />
          </FormField>

          {limits ? (
            <p className="flex items-start gap-1.5 text-xs text-muted">
              <ShieldCheck className="mt-0.5 size-3.5 shrink-0" aria-hidden />
              <span>
                الحد الأقصى للحجم {formatFileSize(limits.maxBytes)}؛ الامتدادات المسموحة{' '}
                {limits.allowedExtensions.join('، ')}.
              </span>
            </p>
          ) : (
            selectedTypeId && (
              <p className="text-xs text-muted">
                لا حدود معلومة محليًا لهذا النوع؛ يتحقق منه الخادم عند الرفع.
              </p>
            )
          )}

          <FormField label="الملف" required error={errors.file?.message}>
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

          <FormField label="عنوان الملف" required error={errors.title?.message}>
            <Input
              id="attachment-title"
              placeholder="مثال: النسخة الممسوحة ضوئيًا"
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
                label="مسح ضوئي أساسي"
                description={
                  isPrimaryScan
                    ? 'يُعرَّف هذا الملف كمسح الوثيقة الأساسي؛ يمكن أن تظل ملفات أخرى مسوحًا غير أساسية.'
                    : 'لن يُعلَّم هذا الملف كمسح الوثيقة الأساسي.'
                }
                disabled={isBusy}
              />
            )}
          />

          <FormField label="ملاحظات التغيير" error={errors.changeNotes?.message}>
            <Textarea
              id="attachment-change-notes"
              rows={2}
              placeholder="ملاحظات اختيارية على الإصدار الأول…"
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
                aria-label="تقدّم رفع الملف"
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
