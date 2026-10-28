import { useMemo } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { FilePlus2, Hash, Save } from 'lucide-react'
import { toast } from 'sonner'
import {
  DOCUMENT_CONFIDENTIALITY,
  DOCUMENT_DIRECTION,
  DOCUMENT_PRIORITY,
  DOCUMENT_TYPE_DIRECTION,
  LOOKUP_SLUGS,
  useLookupEntries,
} from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import { ApiError, toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { cn } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Select, Textarea } from '@/shared/ui'
import {
  createDocumentSchema,
  type CreateDocumentFormValues,
  type CreateDocumentMode,
} from '../model/schemas'
import {
  useCreateDocumentMutation,
  useCreateDraftDocumentMutation,
} from '../api/documentMutations'

interface CreateDocumentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const CURRENT_YEAR = new Date().getFullYear()

/**
 * Two creation paths sharing one form: save as DRAFT, or register immediately
 * with a manual official number for the numbering year.
 */
export function CreateDocumentDialog({ open, onOpenChange }: CreateDocumentDialogProps) {
  const canManageNumbering = useSessionStore((state) =>
    state.hasPermission(Permissions.NumberingManage),
  )

  const typesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)
  const directionsQuery = useLookupEntries(LOOKUP_SLUGS.DocumentDirections)
  const confidentialityQuery = useLookupEntries(LOOKUP_SLUGS.DocumentConfidentialityLevels)
  const priorityQuery = useLookupEntries(LOOKUP_SLUGS.DocumentPriorityLevels)

  const typeOptions = useMemo(
    () =>
      (typesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [typesQuery.data],
  )

  const directionOptions = useMemo(
    () =>
      (directionsQuery.data ?? [])
        .filter((entry) => entry.id !== DOCUMENT_DIRECTION.All)
        .map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    [directionsQuery.data],
  )

  const confidentialityOptions = useMemo(
    () =>
      (confidentialityQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [confidentialityQuery.data],
  )

  const priorityOptions = useMemo(
    () =>
      (priorityQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [priorityQuery.data],
  )

  const createMutation = useCreateDocumentMutation()
  const draftMutation = useCreateDraftDocumentMutation()
  const pending = createMutation.isPending || draftMutation.isPending

  const {
    register,
    control,
    handleSubmit,
    setValue,
    setError,
    clearErrors,
    formState: { errors },
  } = useForm<CreateDocumentFormValues>({
    resolver: zodResolver(createDocumentSchema),
    defaultValues: {
      title: '',
      documentTypeId: '',
      directionId: '',
      subjectSummary: '',
      confidentialityLevelId: String(DOCUMENT_CONFIDENTIALITY.Normal),
      priorityLevelId: String(DOCUMENT_PRIORITY.Normal),
      officialNumberText: '',
      numberingYear: String(CURRENT_YEAR),
      documentDate: '',
      externalReferenceNumber: '',
      externalDocumentDate: '',
      mode: canManageNumbering ? 'register' : 'draft',
    },
  })

  const mode = useWatch({ control, name: 'mode' })
  const mutationError = mode === 'register' ? createMutation.error : draftMutation.error

  const changeMode = (nextMode: CreateDocumentMode) => {
    setValue('mode', nextMode)
    clearErrors(['officialNumberText', 'numberingYear'])
  }

  const onSubmit = handleSubmit(async (values) => {
    const shared = {
      title: values.title.trim(),
      documentTypeId: Number(values.documentTypeId),
      directionId: Number(values.directionId),
      subjectSummary: values.subjectSummary.trim() || null,
      confidentialityLevelId: Number(values.confidentialityLevelId),
      priorityLevelId: Number(values.priorityLevelId),
    }

    try {
      if (values.mode === 'register') {
        await createMutation.mutateAsync({
          ...shared,
          officialNumberText: values.officialNumberText.trim(),
          numberingYear: Number(values.numberingYear),
        })
        toast.success(`تم تسجيل الوثيقة برقم ${values.officialNumberText.trim()}.`)
      } else {
        await draftMutation.mutateAsync({
          ...shared,
          documentDate: values.documentDate || null,
          externalReferenceNumber: values.externalReferenceNumber.trim() || null,
          externalDocumentDate: values.externalDocumentDate || null,
        })
        toast.success('تم حفظ الوثيقة كمسودة.')
      }
      onOpenChange(false)
    } catch (error) {
      if (error instanceof ApiError && error.code === 'Numbering.OfficialNumberTaken') {
        setError('officialNumberText', {
          type: 'server',
          message: toArabicErrorMessage(error),
        })
      }
    }
  })

  const modeCardClass = (selected: boolean) =>
    cn(
      'flex flex-col gap-1 rounded-lg border p-3 text-start transition-colors',
      selected
        ? 'border-accent bg-accent/10'
        : 'border-border bg-surface hover:bg-surface-muted/60',
    )

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="وثيقة جديدة"
      description="احفظ الوثيقة كمسودة لتسجيلها لاحقًا، أو سجّلها فورًا برقم رسمي. تُنسب الوثيقة إلى قسمك الأساسي تلقائيًا."
      size="lg"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={pending}
          >
            إلغاء
          </Button>
          <Button type="submit" form="document-create-form" loading={pending}>
            {mode === 'register' ? 'تسجيل الوثيقة' : 'حفظ كمسودة'}
          </Button>
        </>
      }
    >
      <form
        id="document-create-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <div
          role="radiogroup"
          aria-label="طريقة إنشاء الوثيقة"
          className="grid gap-2 sm:col-span-2 sm:grid-cols-2"
        >
          <button
            type="button"
            role="radio"
            aria-checked={mode === 'draft'}
            onClick={() => changeMode('draft')}
            disabled={pending}
            className={modeCardClass(mode === 'draft')}
          >
            <span className="flex items-center gap-1.5 text-sm font-medium text-foreground">
              <Save className="size-4" aria-hidden />
              حفظ كمسودة
            </span>
            <span className="text-xs text-muted">بدون رقم رسمي؛ تُسجَّل لاحقًا من سجل الوثائق.</span>
          </button>
          <button
            type="button"
            role="radio"
            aria-checked={mode === 'register'}
            onClick={() => changeMode('register')}
            disabled={pending}
            className={modeCardClass(mode === 'register')}
          >
            <span className="flex items-center gap-1.5 text-sm font-medium text-foreground">
              <FilePlus2 className="size-4" aria-hidden />
              تسجيل فوري برقم
            </span>
            <span className="text-xs text-muted">
              حالة «مسجل» مع رقم رسمي وسنة ترقيم غير مستخدمة.
            </span>
          </button>
        </div>

        <FormField
          label="عنوان الوثيقة"
          required
          error={errors.title?.message}
          className="sm:col-span-2"
        >
          <Input
            id="document-title"
            placeholder="مثال: طلب تصريح زيارة لوفد خارجي"
            invalid={Boolean(errors.title)}
            {...register('title')}
          />
        </FormField>

        <FormField
          label="ملخص الموضوع"
          error={errors.subjectSummary?.message}
          className="sm:col-span-2"
        >
          <Textarea
            id="document-summary"
            rows={3}
            placeholder="ملخص اختياري لمحتوى الوثيقة…"
            invalid={Boolean(errors.subjectSummary)}
            {...register('subjectSummary')}
          />
        </FormField>

        <FormField label="نوع الوثيقة" required error={errors.documentTypeId?.message}>
          <Controller
            control={control}
            name="documentTypeId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={(value) => {
                  field.onChange(value)
                  const impliedDirection = DOCUMENT_TYPE_DIRECTION[Number(value)]
                  if (impliedDirection) {
                    setValue('directionId', String(impliedDirection), {
                      shouldValidate: true,
                    })
                  }
                }}
                options={typeOptions}
                disabled={pending || typesQuery.isLoading}
                invalid={Boolean(errors.documentTypeId)}
                placeholder="اختر النوع…"
                aria-label="نوع الوثيقة"
              />
            )}
          />
        </FormField>

        <FormField label="الاتجاه" required error={errors.directionId?.message}>
          <Controller
            control={control}
            name="directionId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={directionOptions}
                disabled={pending || directionsQuery.isLoading}
                invalid={Boolean(errors.directionId)}
                placeholder="اختر الاتجاه…"
                aria-label="اتجاه الوثيقة"
              />
            )}
          />
        </FormField>

        <FormField
          label="مستوى السرية"
          required
          error={errors.confidentialityLevelId?.message}
        >
          <Controller
            control={control}
            name="confidentialityLevelId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={confidentialityOptions}
                disabled={pending}
                invalid={Boolean(errors.confidentialityLevelId)}
                aria-label="مستوى السرية"
              />
            )}
          />
        </FormField>

        <FormField label="درجة الأولوية" required error={errors.priorityLevelId?.message}>
          <Controller
            control={control}
            name="priorityLevelId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={priorityOptions}
                disabled={pending}
                invalid={Boolean(errors.priorityLevelId)}
                aria-label="درجة الأولوية"
              />
            )}
          />
        </FormField>

        {mode === 'register' ? (
          <>
            <FormField
              label="الرقم الرسمي"
              required
              error={errors.officialNumberText?.message}
            >
              <Input
                id="document-official-number"
                dir="ltr"
                placeholder="1447/2026"
                startIcon={<Hash className="size-4" aria-hidden />}
                invalid={Boolean(errors.officialNumberText)}
                {...register('officialNumberText')}
              />
            </FormField>

            <FormField label="سنة الترقيم" required error={errors.numberingYear?.message}>
              <Input
                id="document-numbering-year"
                type="number"
                inputMode="numeric"
                dir="ltr"
                min={1900}
                max={2200}
                invalid={Boolean(errors.numberingYear)}
                {...register('numberingYear')}
              />
            </FormField>
          </>
        ) : (
          <>
            <FormField label="تاريخ الوثيقة" error={errors.documentDate?.message}>
              <Input
                id="document-date"
                type="date"
                dir="ltr"
                invalid={Boolean(errors.documentDate)}
                {...register('documentDate')}
              />
            </FormField>

            <FormField
              label="الرقم المرجعي الخارجي"
              error={errors.externalReferenceNumber?.message}
            >
              <Input
                id="document-external-reference"
                dir="ltr"
                placeholder="EXT-2026-114"
                invalid={Boolean(errors.externalReferenceNumber)}
                {...register('externalReferenceNumber')}
              />
            </FormField>

            <FormField
              label="تاريخ الوثيقة الخارجية"
              error={errors.externalDocumentDate?.message}
              className="sm:col-span-2"
            >
              <Input
                id="document-external-date"
                type="date"
                dir="ltr"
                invalid={Boolean(errors.externalDocumentDate)}
                {...register('externalDocumentDate')}
              />
            </FormField>
          </>
        )}

        {mutationError && (
          <p
            role="alert"
            className="sm:col-span-2 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {toArabicErrorMessage(mutationError)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
