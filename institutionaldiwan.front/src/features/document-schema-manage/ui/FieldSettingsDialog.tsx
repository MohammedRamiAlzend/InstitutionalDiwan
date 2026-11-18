import { useMemo, useState } from 'react'
import { Controller, useFieldArray, useForm, useWatch, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import {
  FIELD_DATA_TYPE,
  FIELD_DATA_TYPE_LABEL_KEYS,
  FIELD_OPTION_SOURCE,
  type DocumentFieldResponse,
  type FieldDataType,
  type SchemaFieldResponse,
  type SchemaVersionResponse,
} from '@/entities/document-schema'
import { toArabicErrorMessage } from '@/shared/api'
import { FEATURES, LOOKUP_CATALOG_META, isLookupCatalogEnabled } from '@/shared/config'
import { resolveLabel, t, useLabelsStore } from '@/shared/lib'
import { Button, Checkbox, Dialog, FormField, Input, Label, Select, Textarea } from '@/shared/ui'
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
import { FieldBehaviorSection } from './FieldBehaviorSection'

const NEW_FIELD = 'new'

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
  const overrides = useLabelsStore((state) => state.overrides)

  const dataTypeOptions = useMemo(
    () =>
      Object.values(FIELD_DATA_TYPE).map((value) => ({
        value: String(value),
        label: resolveLabel(FIELD_DATA_TYPE_LABEL_KEYS[value], overrides),
      })),
    [overrides],
  )
  const optionSourceOptions = useMemo(
    () => [
      { value: String(FIELD_OPTION_SOURCE.Static), label: resolveLabel('schema.option-source.static', overrides) },
      { value: String(FIELD_OPTION_SOURCE.Departments), label: resolveLabel('schema.option-source.departments', overrides) },
      ...(FEATURES.externalOrganizations
        ? [{ value: String(FIELD_OPTION_SOURCE.ExternalOrganizations), label: resolveLabel('schema.option-source.external-organizations', overrides) }]
        : []),
      { value: String(FIELD_OPTION_SOURCE.Lookup), label: resolveLabel('schema.option-source.lookup', overrides) },
    ],
    [overrides],
  )
  const lookupOptions = useMemo(
    () =>
      Object.entries(LOOKUP_CATALOG_META)
        .filter(([slug]) => isLookupCatalogEnabled(slug))
        .map(([value, meta]) => ({
          value,
          label: meta.labelKey ? resolveLabel(meta.labelKey, overrides) : value,
        })),
    [overrides],
  )

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
  const optionSource = useWatch({ control, name: 'optionSource' })

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
      setIdentityErrors({ dataType: t('schema.validation.field-type-required') })
      return
    }
    const settings = toSettingsRequest(values, dataType)

    try {
      if (isEdit) {
        await updateField.mutateAsync({ documentFieldId: field.documentFieldId, settings })
        toast.success(t('schema.field-settings.toast-updated'))
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
          // A field belongs to the form the draft belongs to (creation form or one department's).
          ...(version?.departmentId ? { departmentId: version.departmentId } : {}),
        })
        documentFieldId = created.documentFieldId
        // The identity now exists even if adding it fails below, so retries must reuse it.
        setSource(documentFieldId)
      }

      await addField.mutateAsync({ documentFieldId, settings })
      toast.success(t('schema.field-settings.toast-added'))
      close()
    } catch {
      // The mutation error is rendered in the banner below.
    }
  })

  const isText = dataType === FIELD_DATA_TYPE.Text || dataType === FIELD_DATA_TYPE.LongText
  const isChoice = dataType === FIELD_DATA_TYPE.Select || dataType === FIELD_DATA_TYPE.MultiSelect
  const isStaticChoice = isChoice && optionSource === String(FIELD_OPTION_SOURCE.Static)
  const otherFields = (version?.fields ?? []).filter(
    (entry) => entry.documentFieldId !== field?.documentFieldId,
  )
  const sourceOptions = [
    { value: NEW_FIELD, label: resolveLabel('schema.field-settings.new-field', overrides) },
    ...available.map((entry) => ({ value: entry.documentFieldId, label: entry.key })),
  ]

  return (
    <Dialog
      open={version !== null}
      onOpenChange={(next) => {
        if (pending) return
        if (!next) close()
      }}
      title={isEdit ? t('schema.field-settings.edit-title', { label: field.labelAr }) : t('schema.field-settings.create-title')}
      description={
        isEdit
          ? t('schema.field-settings.edit-description')
          : t('schema.field-settings.create-description')
      }
      size="lg"
      footer={
        <>
          <Button type="button" variant="outline" onClick={close} disabled={pending}>
            <Label k="schema.action.back" />
          </Button>
          <Button type="submit" form="field-settings-form" loading={pending}>
            <Label k={isEdit ? 'common.action.save' : 'schema.field-settings.add'} />
          </Button>
        </>
      }
    >
      <form id="field-settings-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        {!isEdit && (
          <>
            <FormField labelKey="schema.field-settings.field" required>
              <Select
                aria-label={t('schema.field-settings.source-aria')}
                value={source}
                onValueChange={setSource}
                options={sourceOptions}
              />
            </FormField>
            {source === NEW_FIELD && (
              <div className="grid gap-4 sm:grid-cols-2">
                <FormField labelKey="schema.field-settings.key" required error={identityErrors.key}>
                  <Input
                    dir="ltr"
                    value={newKey}
                    placeholder="contract_number"
                    invalid={Boolean(identityErrors.key)}
                    onChange={(event) => setNewKey(event.target.value)}
                  />
                </FormField>
                <FormField labelKey="schema.field-settings.type" required error={identityErrors.dataType}>
                  <Select
                    aria-label={t('schema.field-settings.type')}
                    value={newType}
                    onValueChange={setNewType}
                    options={dataTypeOptions}
                    placeholder={t('schema.field-settings.type-placeholder')}
                    invalid={Boolean(identityErrors.dataType)}
                  />
                </FormField>
              </div>
            )}
          </>
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField labelKey="schema.field-settings.label-ar" required error={errors.labelAr?.message}>
            <Input invalid={Boolean(errors.labelAr)} {...register('labelAr')} />
          </FormField>
          <FormField labelKey="schema.field-settings.display-order" required error={errors.displayOrder?.message}>
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
              label={t('schema.field-settings.required')}
              description={t('schema.field-settings.required-description')}
            />
          )}
        />

        {isText && (
          <>
            <FormField labelKey="schema.field-settings.max-length" error={errors.maxLength?.message}>
              <Input
                dir="ltr"
                inputMode="numeric"
                placeholder={dataType === FIELD_DATA_TYPE.Text ? t('schema.field-settings.max-length-placeholder-text') : t('schema.field-settings.max-length-placeholder-long-text')}
                invalid={Boolean(errors.maxLength)}
                {...register('maxLength')}
              />
            </FormField>
            <FormField labelKey="schema.field-settings.regex-pattern" error={errors.regexPattern?.message}>
              <Input
                dir="ltr"
                placeholder="^[0-9]{4}/[0-9]+$"
                invalid={Boolean(errors.regexPattern)}
                {...register('regexPattern')}
              />
            </FormField>
            {regexPattern.trim() && (
              <FormField
                labelKey="schema.field-settings.regex-message"
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
            <FormField labelKey="schema.field-settings.min-number" error={errors.minNumber?.message}>
              <Input dir="ltr" inputMode="decimal" invalid={Boolean(errors.minNumber)} {...register('minNumber')} />
            </FormField>
            <FormField labelKey="schema.field-settings.max-number" error={errors.maxNumber?.message}>
              <Input dir="ltr" inputMode="decimal" invalid={Boolean(errors.maxNumber)} {...register('maxNumber')} />
            </FormField>
          </div>
        )}

        {isChoice && (
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField labelKey="schema.field-settings.option-source" required>
              <Controller
                control={control}
                name="optionSource"
                render={({ field: control }) => (
                  <Select
                    aria-label={t('schema.field-settings.option-source')}
                    value={control.value}
                    onValueChange={control.onChange}
                    options={optionSourceOptions}
                  />
                )}
              />
            </FormField>
            {optionSource === String(FIELD_OPTION_SOURCE.Lookup) && (
              <FormField labelKey="schema.field-settings.lookup-catalog" required error={errors.lookupCatalog?.message}>
                <Controller
                  control={control}
                  name="lookupCatalog"
                  render={({ field: control }) => (
                    <Select
                      aria-label={t('schema.field-settings.lookup-catalog')}
                      value={control.value || undefined}
                      onValueChange={control.onChange}
                      options={lookupOptions}
                      invalid={Boolean(errors.lookupCatalog)}
                    />
                  )}
                />
              </FormField>
            )}
          </div>
        )}

        {isStaticChoice && (
          <FormField
            labelKey="schema.field-settings.options"
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
                    aria-label={t('schema.field-settings.option-key-aria', { index: index + 1 })}
                    placeholder="key"
                    className="w-40"
                    invalid={Boolean(errors.options?.[index]?.key)}
                    {...register(`options.${index}.key`)}
                  />
                  <Input
                    aria-label={t('schema.field-settings.option-label-aria', { index: index + 1 })}
                    placeholder={t('schema.field-settings.option-label-placeholder')}
                    invalid={Boolean(errors.options?.[index]?.labelAr)}
                    {...register(`options.${index}.labelAr`)}
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    aria-label={t('schema.field-settings.option-remove-aria', { index: index + 1 })}
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
                <Label k="schema.field-settings.add-option" />
              </Button>
            </div>
          </FormField>
        )}

        <FieldBehaviorSection
          control={control}
          register={register}
          errors={errors}
          dataType={dataType}
          otherFields={otherFields}
        />

        {error && (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
