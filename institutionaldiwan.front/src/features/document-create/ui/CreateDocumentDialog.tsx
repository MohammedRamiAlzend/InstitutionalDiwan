import { useEffect, useMemo, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Building2, FilePlus2, Hash, Landmark, Save, Search, UserRound } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import {
  DynamicFieldInputs,
  toFieldValueRequests,
  useDocumentTypeSchema,
  validateFieldValues,
  type FieldValueErrors,
  type FieldValueMap,
} from '@/entities/document-schema'
import { useExternalOrganizations } from '@/entities/external-organization'
import { LOOKUP_SLUGS, useLookupEntries, usePreselectLookupDefault } from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import { ApiError, toArabicErrorMessage } from '@/shared/api'
import { PARTICIPANT_ROLE, Permissions } from '@/shared/config'
import {
  clearFormDraft,
  cn,
  formDraftKey,
  readFormDraft,
  useDebouncedValue,
  useFormDraftAutosave,
} from '@/shared/lib'
import {
  Button,
  Checkbox,
  Dialog,
  FormField,
  Input,
  Select,
  Textarea,
} from '@/shared/ui'
import {
  createDocumentSchema,
  type CreateDocumentFormValues,
  type CreateDocumentMode,
} from '../model/schemas'
import {
  useAddCreationRecipientMutation,
  useCreateDocumentMutation,
  useCreateDraftDocumentMutation,
} from '../api/documentMutations'

interface CreateDocumentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const NO_DESTINATION = '__none__'

const todayIso = () => new Date().toISOString().slice(0, 10)

const RECIPIENT_TARGET_OPTIONS = [
  {
    key: 'external',
    label: 'جهة خارجية',
    hint: 'وزارة أو شركة أو منظمة مسجّلة.',
    icon: Building2,
  },
  {
    key: 'department',
    label: 'قسم داخلي',
    hint: 'دائرة أو قسم داخل المؤسسة.',
    icon: Landmark,
  },
  {
    key: 'person',
    label: 'شخص',
    hint: 'اسم شخص غير مرتبط بقسم أو جهة.',
    icon: UserRound,
  },
] as const

interface SavedDraft {
  values?: Partial<CreateDocumentFormValues>
  fieldValues?: FieldValueMap
}

function buildEmptyValues(canManageNumbering: boolean): CreateDocumentFormValues {
  return {
    title: '',
    documentTypeId: '',
    subjectSummary: '',
    registryNumberText: '',
    registryDate: todayIso(),
    destinationDepartmentId: '',
    movementTypeId: '',
    movementInstructions: '',
    documentDate: '',
    externalReferenceNumber: '',
    externalDocumentDate: '',
    mode: canManageNumbering ? 'register' : 'draft',
    addRecipient: false,
    recipientType: 'external',
    recipientOrganizationId: '',
    recipientDepartmentId: '',
    recipientPersonName: '',
  }
}

/** A draft is worth keeping once the user typed anything meaningful. */
function hasDraftContent(
  values: Partial<CreateDocumentFormValues> | undefined,
  fieldValues: FieldValueMap,
): boolean {
  const typed = [
    values?.title,
    values?.subjectSummary,
    values?.registryNumberText,
    values?.movementInstructions,
    values?.externalReferenceNumber,
    values?.recipientPersonName,
  ]
  return (
    typed.some((text) => Boolean(text?.trim())) ||
    Boolean(values?.destinationDepartmentId) ||
    Object.values(fieldValues).some((entry) => entry.trim())
  )
}

/**
 * Two creation paths sharing one form: save as DRAFT, or register immediately
 * with the number and date written in the creator's department register (the system
 * issues the central number itself) and an optional movement sent at once. An optional recipient
 * is attached as a RECEIVER participant right after the document is created.
 */
export function CreateDocumentDialog({ open, onOpenChange }: CreateDocumentDialogProps) {
  const canManageNumbering = useSessionStore((state) =>
    state.hasPermission(Permissions.NumberingManage),
  )
  const canAddRecipient = useSessionStore((state) =>
    state.hasPermission(Permissions.DocumentEdit),
  )

  const userId = useSessionStore((state) => state.user?.userId)
  // Unsent input survives an accidental close; it is dropped only after a successful submit.
  const draftKey = formDraftKey('document-create', userId)
  const [restored] = useState(() => readFormDraft<SavedDraft>(draftKey))
  const [saveDraft, setSaveDraft] = useState(true)
  const [restoredNoticeVisible, setRestoredNoticeVisible] = useState(() =>
    hasDraftContent(restored?.values, restored?.fieldValues ?? {}),
  )

  const typesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)
  const movementTypesQuery = useLookupEntries(LOOKUP_SLUGS.MovementTypes)

  const typeOptions = useMemo(
    () =>
      (typesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [typesQuery.data],
  )

  const createMutation = useCreateDocumentMutation()
  const draftMutation = useCreateDraftDocumentMutation()
  const addRecipientMutation = useAddCreationRecipientMutation()
  const pending =
    createMutation.isPending || draftMutation.isPending || addRecipientMutation.isPending

  const {
    register,
    reset,
    control,
    getValues,
    handleSubmit,
    setValue,
    setError,
    clearErrors,
    formState: { errors },
  } = useForm<CreateDocumentFormValues>({
    resolver: zodResolver(createDocumentSchema),
    defaultValues: { ...buildEmptyValues(canManageNumbering), ...restored?.values },
  })

  usePreselectLookupDefault(typesQuery.data, getValues('documentTypeId'), (value) => setValue('documentTypeId', value))
  usePreselectLookupDefault(movementTypesQuery.data, getValues('movementTypeId'), (value) =>
    setValue('movementTypeId', value),
  )

  const mode = useWatch({ control, name: 'mode' })
  const documentTypeId = useWatch({ control, name: 'documentTypeId' })

  // Dynamic fields follow the chosen type's published schema; a type without one shows no section.
  const schemaQuery = useDocumentTypeSchema(documentTypeId ? Number(documentTypeId) : undefined, {
    enabled: open,
  })
  const schemaFields = schemaQuery.data?.schema?.fields ?? []
  const [fieldValues, setFieldValues] = useState<FieldValueMap>(restored?.fieldValues ?? {})
  const [fieldErrors, setFieldErrors] = useState<FieldValueErrors>({})
  const watched = useWatch({ control })
  const draftHasContent = hasDraftContent(watched, fieldValues)
  const draftAutosave = useFormDraftAutosave(
    draftKey,
    { values: watched, fieldValues },
    saveDraft && draftHasContent,
  )
  useEffect(() => {
    if (saveDraft && !draftHasContent) clearFormDraft(draftKey)
  }, [draftKey, draftHasContent, saveDraft])
  const addRecipient = useWatch({ control, name: 'addRecipient' })
  const recipientType = useWatch({ control, name: 'recipientType' })

  const [recipientSearch, setRecipientSearch] = useState('')
  const debouncedRecipientSearch = useDebouncedValue(recipientSearch)

  const recipientOrganizationsQuery = useExternalOrganizations(
    {
      page: 1,
      pageSize: 20,
      search: debouncedRecipientSearch.trim() || undefined,
    },
    {
      enabled: open && canAddRecipient && addRecipient && recipientType === 'external',
    },
  )

  const recipientDepartmentsQuery = useDepartmentOptions({
    enabled: open && canAddRecipient && addRecipient && recipientType === 'department',
  })

  const recipientOrganizationOptions = useMemo(
    () =>
      (recipientOrganizationsQuery.data?.items ?? []).map((organization) => ({
        value: organization.externalOrganizationId,
        label: `${organization.nameAr} · ${organization.code}`,
      })),
    [recipientOrganizationsQuery.data],
  )

  const recipientDepartmentOptions = useMemo(
    () =>
      (recipientDepartmentsQuery.data ?? [])
        .filter((department) => department.isActive)
        .map((department) => ({
          value: department.departmentId,
          label: `${department.nameAr} · ${department.code}`,
        })),
    [recipientDepartmentsQuery.data],
  )
  const destinationQuery = useDepartmentOptions({ enabled: open && mode === 'register' })
  const destinationOptions = useMemo(
    () =>
      (destinationQuery.data ?? [])
        .filter((department) => department.isActive)
        .map((department) => ({
          value: department.departmentId,
          label: `${department.nameAr} · ${department.code}`,
        })),
    [destinationQuery.data],
  )
  const movementTypeOptions = useMemo(
    () =>
      (movementTypesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [movementTypesQuery.data],
  )
  const destinationDepartmentId = useWatch({ control, name: 'destinationDepartmentId' })
  const mutationError = mode === 'register' ? createMutation.error : draftMutation.error
  // A taken register number is surfaced on its own field; repeating it in the
  // form-level banner would show the same message twice.
  const registryNumberConflict =
    mutationError instanceof ApiError && mutationError.code === 'Numbering.RegistryNumberTaken'

  const changeMode = (nextMode: CreateDocumentMode) => {
    setValue('mode', nextMode)
    clearErrors(['registryNumberText', 'registryDate', 'movementTypeId'])
  }

  const discardRestoredDraft = () => {
    clearFormDraft(draftKey)
    reset(buildEmptyValues(canManageNumbering))
    setFieldValues({})
    setFieldErrors({})
    setRestoredNoticeVisible(false)
  }

  const onSubmit = handleSubmit(async (values) => {
    const dynamicErrors = validateFieldValues(schemaFields, fieldValues)
    setFieldErrors(dynamicErrors)
    if (Object.keys(dynamicErrors).length > 0) return

    const shared = {
      fieldValues: schemaFields.length > 0 ? toFieldValueRequests(schemaFields, fieldValues) : undefined,
      title: values.title.trim(),
      documentTypeId: Number(values.documentTypeId),
      subjectSummary: values.subjectSummary.trim() || null,
    }

    const recipientRequest =
      values.addRecipient && canAddRecipient
        ? {
            roleId: PARTICIPANT_ROLE.Receiver,
            isPrimary: true,
            notes: null,
            ...(values.recipientType === 'external'
              ? { externalOrganizationId: values.recipientOrganizationId }
              : values.recipientType === 'department'
                ? { internalDepartmentId: values.recipientDepartmentId }
                : { personName: values.recipientPersonName.trim() }),
          }
        : null

    try {
      const created =
        values.mode === 'register'
          ? await createMutation.mutateAsync({
              ...shared,
              departmentRegistryNumberText: values.registryNumberText.trim(),
              departmentRegistryDate: values.registryDate,
              movement: values.destinationDepartmentId
                ? {
                    destinationDepartmentId: values.destinationDepartmentId,
                    movementTypeId: Number(values.movementTypeId),
                    instructions: values.movementInstructions.trim() || null,
                  }
                : null,
            })
          : await draftMutation.mutateAsync({
              ...shared,
              documentDate: values.documentDate || null,
              externalReferenceNumber: values.externalReferenceNumber.trim() || null,
              externalDocumentDate: values.externalDocumentDate || null,
            })

      const baseMessage =
        values.mode === 'register'
          ? values.destinationDepartmentId
            ? 'تم تسجيل الوثيقة وإرسالها إلى القسم المحدد.'
            : 'تم تسجيل الوثيقة وكتابة سطر السجل.'
          : 'تم حفظ الوثيقة كمسودة.'

      if (!recipientRequest) {
        toast.success(baseMessage)
      } else {
        try {
          await addRecipientMutation.mutateAsync({
            documentId: created.documentId,
            request: recipientRequest,
          })
          toast.success(`${baseMessage} وأُضيف المستلم.`)
        } catch (recipientError) {
          // The document already exists; keep it and report only the recipient failure.
          toast.error(
            `أُنشئت الوثيقة، لكن تعذّر إضافة المستلم: ${toArabicErrorMessage(recipientError)}`,
          )
        }
      }

      setSaveDraft(false)
      draftAutosave.discard()
      setFieldValues({})
      onOpenChange(false)
    } catch (error) {
      if (error instanceof ApiError && error.code === 'Numbering.RegistryNumberTaken') {
        setError('registryNumberText', {
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
      description="احفظ الوثيقة كمسودة لتسجيلها لاحقًا، أو سجّلها فورًا في سجل قسمك (يصدر النظام رقمه الخاص تلقائيًا) مع إرسالها إن أردت. تُنسب الوثيقة إلى قسمك الأساسي تلقائيًا، ويمكنك إضافة مستلم اختياري الآن."
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
        {restoredNoticeVisible && (
          <div
            role="status"
            className="sm:col-span-2 flex flex-wrap items-center justify-between gap-2 rounded-lg bg-accent/10 px-3 py-2 text-sm text-foreground"
          >
            <span>استُعيدت بياناتك غير المرسلة من المرة السابقة. تُحفظ تلقائيًا حتى تُرسل الوثيقة.</span>
            <Button type="button" variant="ghost" size="sm" onClick={discardRestoredDraft} disabled={pending}>
              مسح وبدء من جديد
            </Button>
          </div>
        )}

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
            <span className="text-xs text-muted">بدون رقم؛ تُسجَّل لاحقًا من سجل الوثائق.</span>
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
              تسجيل فوري في السجل
            </span>
            <span className="text-xs text-muted">
              حالة «مسجل» برقم نظام تلقائي، مع رقمك وتاريخك في سجل القسم.
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
                  if (Object.values(fieldValues).some((entry) => entry.trim())) {
                    toast.info('أُفرغت الحقول الإضافية لأن نوع الوثيقة تغيّر.')
                  }
                  setFieldValues({})
                  setFieldErrors({})
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

        {schemaFields.length > 0 && (
          <fieldset className="col-span-full flex flex-col gap-4 rounded-xl border border-border p-4">
            <legend className="px-2 text-sm font-semibold text-foreground">
              حقول إضافية لهذا النوع
            </legend>
            <DynamicFieldInputs
              fields={schemaFields}
              values={fieldValues}
              errors={fieldErrors}
              disabled={pending}
              onChange={(key, value) => {
                setFieldValues((current) => ({ ...current, [key]: value }))
                setFieldErrors((current) => {
                  if (!(key in current)) return current
                  return Object.fromEntries(Object.entries(current).filter(([k]) => k !== key))
                })
              }}
            />
          </fieldset>
        )}

        {mode === 'register' ? (
          <>
            <FormField
              label="رقم السجل (في دفتر قسمك)"
              required
              error={errors.registryNumberText?.message}
            >
              <Input
                id="document-registry-number"
                dir="ltr"
                placeholder="1447/2026"
                startIcon={<Hash className="size-4" aria-hidden />}
                invalid={Boolean(errors.registryNumberText)}
                {...register('registryNumberText')}
              />
            </FormField>

            <FormField label="تاريخ السجل" required error={errors.registryDate?.message}>
              <Input
                id="document-registry-date"
                type="date"
                dir="ltr"
                invalid={Boolean(errors.registryDate)}
                {...register('registryDate')}
              />
            </FormField>

            <fieldset className="sm:col-span-2 grid gap-3 rounded-lg border border-border p-3 sm:grid-cols-2">
              <legend className="px-1 text-sm font-medium text-foreground">
                إرسال الوثيقة (اختياري)
              </legend>
              <FormField
                label="القسم المستلم (فارغ = تُفتح الحركة لاحقًا)"
                className="sm:col-span-2"
              >
                <Controller
                  control={control}
                  name="destinationDepartmentId"
                  render={({ field }) => (
                    <Select
                      value={field.value || undefined}
                      onValueChange={(value) => field.onChange(value === NO_DESTINATION ? '' : value)}
                      options={[{ value: NO_DESTINATION, label: 'بدون إرسال الآن' }, ...destinationOptions]}
                      disabled={pending || destinationQuery.isLoading}
                      placeholder={destinationQuery.isLoading ? 'جارٍ التحميل…' : 'بدون إرسال الآن'}
                      aria-label="القسم المستلم للحركة"
                    />
                  )}
                />
              </FormField>

              {destinationDepartmentId && (
                <>
                  <FormField label="نوع الحركة" required error={errors.movementTypeId?.message}>
                    <Controller
                      control={control}
                      name="movementTypeId"
                      render={({ field }) => (
                        <Select
                          value={field.value || undefined}
                          onValueChange={field.onChange}
                          options={movementTypeOptions}
                          disabled={pending || movementTypesQuery.isLoading}
                          invalid={Boolean(errors.movementTypeId)}
                          placeholder="اختر النوع…"
                          aria-label="نوع الحركة"
                        />
                      )}
                    />
                  </FormField>

                  <FormField
                    label="تعليمات (اختياري)"
                    error={errors.movementInstructions?.message}
                  >
                    <Input
                      id="document-movement-instructions"
                      invalid={Boolean(errors.movementInstructions)}
                      {...register('movementInstructions')}
                    />
                  </FormField>
                </>
              )}
            </fieldset>
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

        {canAddRecipient && (
          <div className="flex flex-col gap-3 rounded-lg border border-border bg-surface-muted/30 p-3 sm:col-span-2">
            <Controller
              control={control}
              name="addRecipient"
              render={({ field }) => (
                <Checkbox
                  checked={field.value}
                  onCheckedChange={(checked) => {
                    field.onChange(checked)
                    clearErrors([
                      'recipientOrganizationId',
                      'recipientDepartmentId',
                      'recipientPersonName',
                    ])
                  }}
                  label="إضافة مستلم للوثيقة (اختياري)"
                  description="يُسجَّل كمشارك أساسي بدور «مستلم» فور إنشاء الوثيقة."
                  disabled={pending}
                />
              )}
            />

            {addRecipient && (
              <div className="flex flex-col gap-3">
                <div
                  role="radiogroup"
                  aria-label="نوع المستلم"
                  className="grid gap-2 sm:grid-cols-3"
                >
                  {RECIPIENT_TARGET_OPTIONS.map((option) => {
                    const Icon = option.icon
                    return (
                      <button
                        key={option.key}
                        type="button"
                        role="radio"
                        aria-checked={recipientType === option.key}
                        onClick={() => {
                          setValue('recipientType', option.key, {
                            shouldValidate: false,
                          })
                          clearErrors([
                            'recipientOrganizationId',
                            'recipientDepartmentId',
                            'recipientPersonName',
                          ])
                        }}
                        disabled={pending}
                        className={modeCardClass(recipientType === option.key)}
                      >
                        <span className="flex items-center gap-1.5 text-sm font-medium text-foreground">
                          <Icon className="size-4" aria-hidden />
                          {option.label}
                        </span>
                        <span className="text-xs text-muted">{option.hint}</span>
                      </button>
                    )
                  })}
                </div>

                {recipientType === 'external' && (
                  <>
                    <FormField
                      label="البحث في الجهات الخارجية"
                      htmlFor="document-recipient-search"
                    >
                      <Input
                        id="document-recipient-search"
                        placeholder="ابحث بالاسم أو الرمز…"
                        startIcon={<Search className="size-4" aria-hidden />}
                        value={recipientSearch}
                        onChange={(event) => setRecipientSearch(event.target.value)}
                        disabled={pending}
                      />
                    </FormField>

                    <FormField
                      label="الجهة المستلمة"
                      required
                      error={errors.recipientOrganizationId?.message}
                    >
                      <Controller
                        control={control}
                        name="recipientOrganizationId"
                        render={({ field }) => (
                          <Select
                            value={field.value || undefined}
                            onValueChange={field.onChange}
                            options={recipientOrganizationOptions}
                            disabled={
                              pending || recipientOrganizationsQuery.isLoading
                            }
                            invalid={Boolean(errors.recipientOrganizationId)}
                            placeholder={
                              recipientOrganizationsQuery.isLoading
                                ? 'جارٍ التحميل…'
                                : 'اختر الجهة…'
                            }
                            aria-label="الجهة المستلمة"
                          />
                        )}
                      />
                    </FormField>

                    {recipientOrganizationsQuery.isSuccess &&
                      recipientOrganizationOptions.length === 0 && (
                        <p className="text-xs text-muted">
                          لا توجد جهات خارجية مطابقة. تُدار الجهات الخارجية من تبويب
                          «الجهات الخارجية» في سجل الوثائق.
                        </p>
                      )}
                  </>
                )}

                {recipientType === 'department' && (
                  <FormField
                    label="القسم المستلم"
                    required
                    error={errors.recipientDepartmentId?.message}
                  >
                    <Controller
                      control={control}
                      name="recipientDepartmentId"
                      render={({ field }) => (
                        <Select
                          value={field.value || undefined}
                          onValueChange={field.onChange}
                          options={recipientDepartmentOptions}
                          disabled={pending || recipientDepartmentsQuery.isLoading}
                          invalid={Boolean(errors.recipientDepartmentId)}
                          placeholder={
                            recipientDepartmentsQuery.isLoading
                              ? 'جارٍ التحميل…'
                              : 'اختر القسم…'
                          }
                          aria-label="القسم المستلم"
                        />
                      )}
                    />
                  </FormField>
                )}

                {recipientType === 'person' && (
                  <FormField
                    label="اسم المستلم"
                    required
                    error={errors.recipientPersonName?.message}
                    htmlFor="document-recipient-person"
                  >
                    <Input
                      id="document-recipient-person"
                      placeholder="مثال: أحمد محمد علي"
                      invalid={Boolean(errors.recipientPersonName)}
                      {...register('recipientPersonName')}
                    />
                  </FormField>
                )}
              </div>
            )}
          </div>
        )}

        {mutationError && !registryNumberConflict && (
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
