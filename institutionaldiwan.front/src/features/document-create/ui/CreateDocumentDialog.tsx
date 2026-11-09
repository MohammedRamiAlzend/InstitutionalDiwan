import { useEffect, useMemo, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { BookOpen, FilePlus2, FileText, Hash, Save, Send, Users } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import {
  DynamicFieldInputs,
  toFieldValueRequests,
  useDocumentTypeSchema,
  validateFieldValues,
  withFieldDefaults,
  type FieldValueErrors,
  type FieldValueMap,
} from '@/entities/document-schema'
import { PersonNamesInput } from '@/entities/person'
import { LOOKUP_SLUGS, useLookupEntries, usePreselectLookupDefault } from '@/entities/lookup'
import { DepartmentRegistrySelect, useDepartmentRegistryOptions } from '@/entities/numbering-registry'
import { useMyPrimaryDepartmentId, useSessionStore } from '@/entities/user'
import { ApiError, toArabicErrorMessage } from '@/shared/api'
import { FEATURES, Permissions } from '@/shared/config'
import {
  clearFormDraft,
  formDraftKey,
  readFormDraft,
  useFormDraftAutosave,
} from '@/shared/lib'
import {
  Badge,
  Button,
  Dialog,
  FormField,
  Input,
  Select,
} from '@/shared/ui'
import {
  createDocumentSchema,
  type CreateDocumentFormValues,
  type CreateDocumentMode,
} from '../model/schemas'
import {
  useCreateDocumentMutation,
  useCreateDraftDocumentMutation,
} from '../api/documentMutations'
import { ChoiceCard } from './ChoiceCard'
import { FormSection } from './FormSection'

interface CreateDocumentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const NO_DESTINATION = '__none__'

const todayIso = () => new Date().toISOString().slice(0, 10)

interface SavedDraft {
  values?: Partial<CreateDocumentFormValues>
  fieldValues?: FieldValueMap
}

function buildEmptyValues(canManageNumbering: boolean): CreateDocumentFormValues {
  return {
    documentTypeId: '',
    registryId: '',
    registryNumberText: '',
    registryDate: todayIso(),
    destinationDepartmentId: '',
    movementTypeId: '',
    movementInstructions: '',
    mode: !FEATURES.drafts || canManageNumbering ? 'register' : 'draft',
    persons: [],
  }
}

/** A draft is worth keeping once the user typed anything meaningful. */
function hasDraftContent(
  values: (Partial<Omit<CreateDocumentFormValues, 'persons'>> & { persons?: readonly unknown[] }) | undefined,
  fieldValues: FieldValueMap,
): boolean {
  const typed = [
    values?.registryNumberText,
    values?.movementInstructions,
  ]
  return (
    typed.some((text) => Boolean(text?.trim())) ||
    Boolean(values?.destinationDepartmentId) ||
    Boolean(values?.persons?.length) ||
    Object.values(fieldValues).some((entry) => entry.trim())
  )
}

/**
 * Two creation paths sharing one form: save as DRAFT, or register immediately
 * with the number and date written in the creator's department register (the system
 * issues the central number itself) and an optional movement sent at once.
 */
export function CreateDocumentDialog({ open, onOpenChange }: CreateDocumentDialogProps) {
  const canManageNumbering = useSessionStore((state) =>
    state.hasPermission(Permissions.NumberingManage),
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
  const personRolesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentPersonRoles)
  const defaultPersonRole = personRolesQuery.data?.find((entry) => entry.isDefault)?.descAr
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
  const pending =
    createMutation.isPending || draftMutation.isPending

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
    defaultValues: {
      ...buildEmptyValues(canManageNumbering),
      ...restored?.values,
      // An unsent form saved while drafts were available must not come back as one.
      ...(FEATURES.drafts ? {} : { mode: 'register' as const }),
    },
  })

  usePreselectLookupDefault(typesQuery.data, getValues('documentTypeId'), (value) => setValue('documentTypeId', value))
  usePreselectLookupDefault(movementTypesQuery.data, getValues('movementTypeId'), (value) =>
    setValue('movementTypeId', value),
  )

  const mode = useWatch({ control, name: 'mode' })
  const documentTypeId = useWatch({ control, name: 'documentTypeId' })

  // Without NUMBERING_VIEW the registers cannot be listed, so the server picks the best match.
  const canViewNumbering = useSessionStore((state) =>
    state.hasPermission(Permissions.NumberingView),
  )
  const { departmentId: ownDepartmentId } = useMyPrimaryDepartmentId()
  const registries = useDepartmentRegistryOptions(ownDepartmentId, {
    enabled: open && mode === 'register' && canViewNumbering,
  })

  // Dynamic fields follow the chosen type's published schema; a type without one shows no section.
  const schemaQuery = useDocumentTypeSchema(documentTypeId ? Number(documentTypeId) : undefined, {
    enabled: open,
  })
  const schemaFields = schemaQuery.data?.schema?.fields ?? []
  const [fieldValues, setFieldValues] = useState<FieldValueMap>(restored?.fieldValues ?? {})
  const [fieldErrors, setFieldErrors] = useState<FieldValueErrors>({})
  const effectiveFieldValues = useMemo(
    () => withFieldDefaults(schemaQuery.data?.schema?.fields ?? [], fieldValues),
    [schemaQuery.data, fieldValues],
  )
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
    clearErrors(['registryId', 'registryNumberText', 'registryDate', 'movementTypeId'])
  }

  const discardRestoredDraft = () => {
    clearFormDraft(draftKey)
    reset(buildEmptyValues(canManageNumbering))
    setFieldValues({})
    setFieldErrors({})
    setRestoredNoticeVisible(false)
  }

  const onSubmit = handleSubmit(async (values) => {
    if (values.mode === 'register' && registries.options.length > 1 && !values.registryId) {
      setError('registryId', { message: 'اختر السجل الذي سُجّل فيه الرقم.' })
      return
    }

    const dynamicErrors = validateFieldValues(schemaFields, effectiveFieldValues)
    setFieldErrors(dynamicErrors)
    if (Object.keys(dynamicErrors).length > 0) return

    const shared = {
      fieldValues: schemaFields.length > 0 ? toFieldValueRequests(schemaFields, effectiveFieldValues) : undefined,
      documentTypeId: Number(values.documentTypeId),
      persons:
        values.persons.length > 0
          ? values.persons.map((person) =>
              person.personId ? { personId: person.personId } : { fullName: person.fullName },
            )
          : undefined,
    }

    try {
      if (values.mode === 'register') {
        await createMutation.mutateAsync({
          ...shared,
          departmentRegistryNumberText: values.registryNumberText.trim(),
          departmentRegistryDate: values.registryDate,
          departmentRegistryId: values.registryId || null,
          movement: values.destinationDepartmentId
            ? {
                destinationDepartmentId: values.destinationDepartmentId,
                movementTypeId: Number(values.movementTypeId),
                instructions: values.movementInstructions.trim() || null,
              }
            : null,
        })
      } else {
        await draftMutation.mutateAsync(shared)
      }

      toast.success(
        values.mode === 'register'
          ? values.destinationDepartmentId
            ? 'تم تسجيل الوثيقة وإرسالها إلى القسم المحدد.'
            : 'تم تسجيل الوثيقة وكتابة سطر السجل.'
          : 'تم حفظ الوثيقة كمسودة.',
      )

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

  const SubmitIcon = mode === 'register' ? FilePlus2 : Save
  const optionalBadge = <Badge variant="outline">اختياري</Badge>

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="وثيقة جديدة"
      description={`${FEATURES.drafts ? 'اختر طريقة الحفظ ثم عبّئ البيانات.' : 'عبّئ البيانات لتسجيل الوثيقة.'} تُنسب الوثيقة إلى قسمك الأساسي تلقائيًا.`}
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
            <SubmitIcon className="size-4" aria-hidden />
            {mode === 'register' ? 'تسجيل الوثيقة' : 'حفظ كمسودة'}
          </Button>
        </>
      }
    >
      <form
        id="document-create-form"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        {restoredNoticeVisible && (
          <div
            role="status"
            className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-accent/10 px-3 py-2 text-sm text-foreground"
          >
            <span>استُعيدت بياناتك غير المرسلة من المرة السابقة. تُحفظ تلقائيًا حتى تُرسل الوثيقة.</span>
            <Button type="button" variant="ghost" size="sm" onClick={discardRestoredDraft} disabled={pending}>
              مسح وبدء من جديد
            </Button>
          </div>
        )}

        {FEATURES.drafts && (
        <div role="radiogroup" aria-label="طريقة إنشاء الوثيقة" className="grid gap-3 sm:grid-cols-2">
          <ChoiceCard
            icon={Save}
            title="حفظ كمسودة"
            hint="بدون رقم؛ تُسجَّل لاحقًا من سجل الوثائق."
            selected={mode === 'draft'}
            onSelect={() => changeMode('draft')}
            disabled={pending}
          />
          <ChoiceCard
            icon={FilePlus2}
            title="تسجيل فوري في السجل"
            hint="برقم نظام تلقائي، مع رقمك وتاريخك في سجل القسم."
            selected={mode === 'register'}
            onSelect={() => changeMode('register')}
            disabled={pending}
          />
        </div>
        )}

        {mode === 'register' && (
          <FormSection icon={BookOpen} title="سجل القسم" hint="الرقم والتاريخ كما كُتبا في دفتر قسمك.">
            {canViewNumbering && (
              <FormField
                label="السجل"
                required={registries.options.length > 1}
                error={errors.registryId?.message}
                className="sm:col-span-2"
              >
                <Controller
                  control={control}
                  name="registryId"
                  render={({ field }) => (
                    <DepartmentRegistrySelect
                      registries={registries}
                      value={field.value}
                      onChange={field.onChange}
                      disabled={pending}
                      invalid={Boolean(errors.registryId)}
                    />
                  )}
                />
              </FormField>
            )}
  
            <FormField label="رقم السجل" required error={errors.registryNumberText?.message}>
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
          </FormSection>
        )}

        <FormSection icon={FileText} title="بيانات الوثيقة">
          <FormField
            label="نوع الوثيقة"
            required
            error={errors.documentTypeId?.message}
            className="sm:col-span-2"
          >
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
            <div className="flex flex-col gap-4 border-t border-border pt-4 sm:col-span-2">
              <DynamicFieldInputs
                fields={schemaFields}
                values={effectiveFieldValues}
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
            </div>
          )}
        </FormSection>

        <FormSection
          icon={Users}
          title="الأشخاص المرتبطون"
          hint={
            defaultPersonRole
              ? `يُسجَّل كل اسم بصفة «${defaultPersonRole}»، ويمكن تغييرها من صفحة الوثيقة.`
              : 'يمكن إضافة أكثر من شخص.'
          }
          aside={optionalBadge}
        >
          <FormField label="الأسماء" error={errors.persons?.message} className="sm:col-span-2">
            <Controller
              control={control}
              name="persons"
              render={({ field }) => (
                <PersonNamesInput
                  id="document-persons"
                  value={field.value ?? []}
                  onChange={field.onChange}
                  disabled={pending}
                  invalid={Boolean(errors.persons)}
                />
              )}
            />
          </FormField>
        </FormSection>

        {mode === 'register' && (
          <>
            <FormSection
              icon={Send}
              title="إرسال الوثيقة"
              hint="اتركه فارغًا لفتح الحركة لاحقًا."
              aside={optionalBadge}
            >
              <FormField label="القسم المستلم" className="sm:col-span-2">
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

                  <FormField label="تعليمات" error={errors.movementInstructions?.message}>
                    <Input
                      id="document-movement-instructions"
                      invalid={Boolean(errors.movementInstructions)}
                      {...register('movementInstructions')}
                    />
                  </FormField>
                </>
              )}
            </FormSection>
          </>
        )}

        {mutationError && !registryNumberConflict && (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(mutationError)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
