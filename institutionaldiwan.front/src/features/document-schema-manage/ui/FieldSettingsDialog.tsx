import { useMemo, useState } from 'react'
import { Controller, useFieldArray, useForm, useWatch, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import {
  FIELD_DATA_TYPE,
  FIELD_DATA_TYPE_LABELS,
  type DocumentFieldResponse,
  type FieldDataType,
  type SchemaFieldResponse,
  type SchemaVersionResponse,
} from '@/entities/document-schema'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Checkbox, Dialog, FormField, Input, Select, Textarea } from '@/shared/ui'
import {
  useAddVersionFieldMutation,
  useCreateFieldMutation,
  useUpdateVersionFieldMutation,
} from '../api/schemaMutations'
import {
  newFieldSchema,
  settingsSchemaFor,
  type FieldSettingsFormValues,
} from '../model/schemas'
import { defaultSettingsValues, toSettingsRequest } from '../model/toRequest'

const NEW_FIELD = 'new'

const DATA_TYPE_OPTIONS = Object.values(FIELD_DATA_TYPE).map((value) => ({
  value: String(value),
  label: FIELD_DATA_TYPE_LABELS[value],
}))

interface FieldSettingsDialogProps {
  /** The draft being edited; the dialog is closed when `null`. */
  version: SchemaVersionResponse | null
  /** Field being edited; omit to add a field. */
  field?: SchemaFieldResponse
  /** Identities already defined for the type, used to offer fields not yet in this draft. */
  definedFields: DocumentFieldResponse[]
  onOpenChange: (open: boolean) => void
}

/** Adds a field to a draft (new identity or an existing one) or edits a draft field's settings. */
export function FieldSettingsDialog({
  version,
  field,
  definedFields,
  onOpenChange,
}: FieldSettingsDialogProps) {
  const isEdit = field !== undefined
  const versionId = version?.schemaVersionId ?? ''

  const createField = useCreateFieldMutation(version?.documentTypeId ?? 0)
  const addField = useAddVersionFieldMutation(versionId)
  const updateField = useUpdateVersionFieldMutation(versionId)

  const [source, setSource] = useState(NEW_FIELD)
  const [newKey, setNewKey] = useState('')
  const [newType, setNewType] = useState('')
  const [identityErrors, setIdentityErrors] = useState<{ key?: string; dataType?: string }>({})

  const available = useMemo(() => {
    const taken = new Set(version?.fields.map((entry) => entry.documentFieldId))
    return definedFields.filter((entry) => !taken.has(entry.documentFieldId))
  }, [version, definedFields])

  const existing = available.find((entry) => entry.documentFieldId === source)
  const dataType: FieldDataType | undefined = isEdit
    ? field.dataType
    : existing
      ? existing.dataType
      : newType
        ? (Number(newType) as FieldDataType)
        : undefined

  // react-hook-form reads the latest options each render, so the schema follows the chosen type.
  const resolver: Resolver<FieldSettingsFormValues> = (values, context, options) =>
    zodResolver(settingsSchemaFor(dataType ?? FIELD_DATA_TYPE.Text))(values, context, options)

  const nextOrder = (version?.fields.length ?? 0) + 1
  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FieldSettingsFormValues>({
    resolver,
    defaultValues: defaultSettingsValues(field, nextOrder),
  })
  const options = useFieldArray({ control, name: 'options' })
  const regexPattern = useWatch({ control, name: 'regexPattern' })

  const pending = createField.isPending || addField.isPending || updateField.isPending
  const error = createField.error ?? addField.error ?? updateField.error

  const close = () => {
    reset(defaultSettingsValues(undefined, nextOrder))
    setSource(NEW_FIELD)
    setNewKey('')
    setNewType('')
    setIdentityErrors({})
    createField.reset()
    addField.reset()
    updateField.reset()
    onOpenChange(false)
  }

  const onSubmit = handleSubmit(async (values) => {
    if (!version || dataType === undefined) {
      setIdentityErrors({ dataType: 'اختر نوع الحقل.' })
      return
    }
    const settings = toSettingsRequest(values, dataType)

    try {
      if (isEdit) {
        await updateField.mutateAsync({ documentFieldId: field.documentFieldId, settings })
        toast.success('حُدّثت إعدادات الحقل.')
        return close()
      }

      let documentFieldId = existing?.documentFieldId
      if (!documentFieldId) {
        const identity = newFieldSchema.safeParse({ key: newKey, dataType: newType })
        if (!identity.success) {
          const flat = identity.error.flatten().fieldErrors
          setIdentityErrors({ key: flat.key?.[0], dataType: flat.dataType?.[0] })
          return
        }
        const created = await createField.mutateAsync({
          key: identity.data.key,
          dataType: Number(identity.data.dataType) as FieldDataType,
        })
        documentFieldId = created.documentFieldId
        // The identity now exists even if adding it fails below, so retries must reuse it.
        setSource(documentFieldId)
      }

      await addField.mutateAsync({ documentFieldId, settings })
      toast.success('أُضيف الحقل إلى المسودة.')
      close()
    } catch {
      // The mutation error is rendered in the banner below.
    }
  })

  const isText = dataType === FIELD_DATA_TYPE.Text || dataType === FIELD_DATA_TYPE.LongText
  const sourceOptions = [
    { value: NEW_FIELD, label: 'حقل جديد' },
    ...available.map((entry) => ({ value: entry.documentFieldId, label: entry.key })),
  ]

  return (
    <Dialog
      open={version !== null}
      onOpenChange={(next) => {
        if (pending) return
        if (!next) close()
      }}
      title={isEdit ? `تعديل الحقل «${field.labelAr}»` : 'إضافة حقل إلى المسودة'}
      description={
        isEdit
          ? 'يتغير الحقل في هذه المسودة فقط، ولا يتأثر مفتاحه ولا نوعه.'
          : 'اختر حقلًا معرّفًا للنوع أو أنشئ حقلًا جديدًا. مفتاح الحقل ونوعه لا يتغيران بعد الإنشاء.'
      }
      size="lg"
      footer={
        <>
          <Button type="button" variant="outline" onClick={close} disabled={pending}>
            تراجع
          </Button>
          <Button type="submit" form="field-settings-form" loading={pending}>
            {isEdit ? 'حفظ' : 'إضافة'}
          </Button>
        </>
      }
    >
      <form id="field-settings-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        {!isEdit && (
          <>
            <FormField label="الحقل" required>
              <Select
                aria-label="مصدر الحقل"
                value={source}
                onValueChange={setSource}
                options={sourceOptions}
              />
            </FormField>
            {source === NEW_FIELD && (
              <div className="grid gap-4 sm:grid-cols-2">
                <FormField label="المفتاح" required error={identityErrors.key}>
                  <Input
                    dir="ltr"
                    value={newKey}
                    placeholder="contract_number"
                    invalid={Boolean(identityErrors.key)}
                    onChange={(event) => setNewKey(event.target.value)}
                  />
                </FormField>
                <FormField label="نوع الحقل" required error={identityErrors.dataType}>
                  <Select
                    aria-label="نوع الحقل"
                    value={newType}
                    onValueChange={setNewType}
                    options={DATA_TYPE_OPTIONS}
                    placeholder="اختر النوع"
                    invalid={Boolean(identityErrors.dataType)}
                  />
                </FormField>
              </div>
            )}
          </>
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label="العنوان بالعربية" required error={errors.labelAr?.message}>
            <Input invalid={Boolean(errors.labelAr)} {...register('labelAr')} />
          </FormField>
          <FormField label="ترتيب العرض" required error={errors.displayOrder?.message}>
            <Input
              dir="ltr"
              inputMode="numeric"
              invalid={Boolean(errors.displayOrder)}
              {...register('displayOrder')}
            />
          </FormField>
        </div>

        <Controller
          control={control}
          name="isRequired"
          render={({ field: control }) => (
            <Checkbox
              checked={control.value}
              onCheckedChange={control.onChange}
              label="حقل إجباري"
              description="يمنع تسجيل الوثيقة رسميًا قبل تعبئته."
            />
          )}
        />

        {isText && (
          <>
            <FormField label="الحد الأقصى للطول" error={errors.maxLength?.message}>
              <Input
                dir="ltr"
                inputMode="numeric"
                placeholder={dataType === FIELD_DATA_TYPE.Text ? '500 كحد أقصى' : '4000 كحد أقصى'}
                invalid={Boolean(errors.maxLength)}
                {...register('maxLength')}
              />
            </FormField>
            <FormField label="نمط التحقق (Regex)" error={errors.regexPattern?.message}>
              <Input
                dir="ltr"
                placeholder="^[0-9]{4}/[0-9]+$"
                invalid={Boolean(errors.regexPattern)}
                {...register('regexPattern')}
              />
            </FormField>
            {regexPattern.trim() && (
              <FormField
                label="رسالة الخطأ عند عدم المطابقة"
                required
                error={errors.regexErrorMessageAr?.message}
              >
                <Textarea
                  rows={2}
                  invalid={Boolean(errors.regexErrorMessageAr)}
                  {...register('regexErrorMessageAr')}
                />
              </FormField>
            )}
          </>
        )}

        {dataType === FIELD_DATA_TYPE.Number && (
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="الحد الأدنى" error={errors.minNumber?.message}>
              <Input dir="ltr" inputMode="decimal" invalid={Boolean(errors.minNumber)} {...register('minNumber')} />
            </FormField>
            <FormField label="الحد الأعلى" error={errors.maxNumber?.message}>
              <Input dir="ltr" inputMode="decimal" invalid={Boolean(errors.maxNumber)} {...register('maxNumber')} />
            </FormField>
          </div>
        )}

        {dataType === FIELD_DATA_TYPE.Select && (
          <FormField
            label="الخيارات"
            required
            error={
              typeof errors.options?.message === 'string'
                ? errors.options.message
                : (errors.options?.root?.message ?? undefined)
            }
          >
            <div className="flex flex-col gap-2">
              {options.fields.map((item, index) => (
                <div key={item.id} className="flex items-start gap-2">
                  <Input
                    dir="ltr"
                    aria-label={`مفتاح الخيار ${index + 1}`}
                    placeholder="key"
                    className="w-40"
                    invalid={Boolean(errors.options?.[index]?.key)}
                    {...register(`options.${index}.key`)}
                  />
                  <Input
                    aria-label={`عنوان الخيار ${index + 1}`}
                    placeholder="العنوان بالعربية"
                    invalid={Boolean(errors.options?.[index]?.labelAr)}
                    {...register(`options.${index}.labelAr`)}
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    aria-label={`حذف الخيار ${index + 1}`}
                    onClick={() => options.remove(index)}
                  >
                    <Trash2 className="size-4" aria-hidden />
                  </Button>
                </div>
              ))}
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="self-start"
                onClick={() => options.append({ key: '', labelAr: '' })}
              >
                <Plus className="size-4" aria-hidden />
                إضافة خيار
              </Button>
            </div>
          </FormField>
        )}

        {error && (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
