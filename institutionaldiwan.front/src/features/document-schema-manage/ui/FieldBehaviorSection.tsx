import type { ReactNode } from 'react'
import { Controller, useWatch, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form'
import {
  FIELD_DATA_TYPE,
  FIELD_OPTION_SOURCE,
  FieldValueControl,
  type FieldDataType,
  type FieldOption,
  type SchemaFieldResponse,
} from '@/entities/document-schema'
import type { LabelKey } from '@/shared/config'
import { resolveLabel, t, useLabelsStore } from '@/shared/lib'
import { Checkbox, FormField, Input, Label, Select, Textarea } from '@/shared/ui'
import type { FieldSettingsFormValues } from '../model/schemas'
import { previewField } from '../model/toRequest'

interface FieldBehaviorSectionProps {
  control: Control<FieldSettingsFormValues>
  register: UseFormRegister<FieldSettingsFormValues>
  errors: FieldErrors<FieldSettingsFormValues>
  dataType: FieldDataType | undefined
  /** The draft's other fields, candidates for a visibility condition. */
  otherFields: readonly SchemaFieldResponse[]
}

const NO_CONDITION = '__none__'

function isConditionController(field: SchemaFieldResponse): boolean {
  return (
    field.dataType === FIELD_DATA_TYPE.Boolean ||
    field.dataType === FIELD_DATA_TYPE.Select ||
    field.dataType === FIELD_DATA_TYPE.MultiSelect
  )
}

function Group({ titleKey, children }: { titleKey: LabelKey; children: ReactNode }) {
  return (
    <fieldset className="flex flex-col gap-4 rounded-lg border border-border p-3">
      <legend className="px-1 text-xs font-semibold text-muted">
        <Label k={titleKey} />
      </legend>
      {children}
    </fieldset>
  )
}

/** The form behavior of a field: default and read-only, hints, limits, condition, table use and layout. */
export function FieldBehaviorSection({
  control,
  register,
  errors,
  dataType,
  otherFields,
}: FieldBehaviorSectionProps) {
  const values = useWatch({ control })
  const overrides = useLabelsStore((state) => state.overrides)
  const isText = dataType === FIELD_DATA_TYPE.Text || dataType === FIELD_DATA_TYPE.LongText
  const isDate = dataType === FIELD_DATA_TYPE.Date
  const isChoice = dataType === FIELD_DATA_TYPE.Select || dataType === FIELD_DATA_TYPE.MultiSelect
  const sourced = isChoice && values.optionSource !== String(FIELD_OPTION_SOURCE.Static)

  const yesNo = [
    { value: 'true', label: resolveLabel('schema.value.yes', overrides) },
    { value: 'false', label: resolveLabel('schema.value.no', overrides) },
  ]
  const controllers = otherFields.filter(isConditionController)
  const controller = controllers.find((field) => field.key === values.visibleWhenFieldKey)
  const controllerValues =
    controller?.dataType === FIELD_DATA_TYPE.Boolean
      ? yesNo
      : (controller?.options ?? []).map((option) => ({ value: option.key, label: option.labelAr }))

  const draftOptions = (values.options ?? []) as FieldOption[]

  return (
    <>
      <Group titleKey="schema.behavior.group-value">
        {dataType !== undefined && !sourced && (
          <FormField labelKey="schema.behavior.default-value" error={errors.defaultValue?.message}>
            <div className="flex flex-col gap-2">
              {!values.defaultToday && (
                <Controller
                  control={control}
                  name="defaultValue"
                  render={({ field }) => (
                    <FieldValueControl
                      field={previewField(dataType, draftOptions)}
                      value={field.value}
                      invalid={Boolean(errors.defaultValue)}
                      onChange={field.onChange}
                    />
                  )}
                />
              )}
              {isDate && (
                <Controller
                  control={control}
                  name="defaultToday"
                  render={({ field }) => (
                    <Checkbox checked={field.value} onCheckedChange={field.onChange} label={t('schema.behavior.default-today')} />
                  )}
                />
              )}
            </div>
          </FormField>
        )}

        {!sourced && (
          <Controller
            control={control}
            name="isReadOnly"
            render={({ field }) => (
              <Checkbox
                checked={field.value}
                onCheckedChange={field.onChange}
                label={t('schema.behavior.read-only')}
                description={t('schema.behavior.read-only-description')}
              />
            )}
          />
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField labelKey="schema.behavior.placeholder" error={errors.placeholderAr?.message}>
            <Input invalid={Boolean(errors.placeholderAr)} {...register('placeholderAr')} />
          </FormField>
          {isText && (
            <FormField labelKey="schema.behavior.min-length" error={errors.minLength?.message}>
              <Input dir="ltr" inputMode="numeric" invalid={Boolean(errors.minLength)} {...register('minLength')} />
            </FormField>
          )}
        </div>
        <FormField labelKey="schema.behavior.help-text" error={errors.helpTextAr?.message}>
          <Textarea rows={2} invalid={Boolean(errors.helpTextAr)} {...register('helpTextAr')} />
        </FormField>

        {isDate && (
          <div className="grid gap-4 sm:grid-cols-2">
            {(
              [
                ['minDate', 'minDateToday', 'schema.behavior.min-date'],
                ['maxDate', 'maxDateToday', 'schema.behavior.max-date'],
              ] as const
            ).map(([name, todayName, labelKey]) => (
              <FormField key={name} labelKey={labelKey} error={errors[name]?.message}>
                <div className="flex flex-col gap-2">
                  {!values[todayName] && (
                    <Input dir="ltr" type="date" invalid={Boolean(errors[name])} {...register(name)} />
                  )}
                  <Controller
                    control={control}
                    name={todayName}
                    render={({ field }) => (
                      <Checkbox checked={field.value} onCheckedChange={field.onChange} label={t('schema.behavior.today')} />
                    )}
                  />
                </div>
              </FormField>
            ))}
          </div>
        )}
      </Group>

      <Group titleKey="schema.behavior.group-condition">
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField labelKey="schema.behavior.visible-when-field" hint={controllers.length === 0 ? t('schema.behavior.visible-when-hint') : undefined}>
            <Controller
              control={control}
              name="visibleWhenFieldKey"
              render={({ field }) => (
                <Select
                  aria-label={t('schema.behavior.visible-when-field-aria')}
                  value={field.value || NO_CONDITION}
                  onValueChange={(next) => field.onChange(next === NO_CONDITION ? '' : next)}
                  options={[
                    { value: NO_CONDITION, label: resolveLabel('schema.behavior.always-visible', overrides) },
                    ...controllers.map((entry) => ({ value: entry.key, label: entry.labelAr })),
                  ]}
                />
              )}
            />
          </FormField>
          {controller && (
            <FormField labelKey="schema.behavior.equals" required error={errors.visibleWhenValue?.message}>
              <Controller
                control={control}
                name="visibleWhenValue"
                render={({ field }) => (
                  <Select
                    aria-label={t('schema.behavior.condition-value-aria')}
                    value={field.value || undefined}
                    onValueChange={field.onChange}
                    options={controllerValues}
                    invalid={Boolean(errors.visibleWhenValue)}
                  />
                )}
              />
            </FormField>
          )}
        </div>
      </Group>

      <Group titleKey="schema.behavior.group-table">
        <Controller
          control={control}
          name="isFilterable"
          render={({ field }) => (
            <Checkbox
              checked={field.value}
              onCheckedChange={field.onChange}
              label={t('schema.behavior.filterable')}
              description={t('schema.behavior.filterable-description')}
            />
          )}
        />
        <Controller
          control={control}
          name="showInTable"
          render={({ field }) => (
            <Checkbox
              checked={field.value}
              onCheckedChange={field.onChange}
              label={t('schema.behavior.show-in-table')}
              description={t('schema.behavior.show-in-table-description')}
            />
          )}
        />
        <Controller
          control={control}
          name="isHalfWidth"
          render={({ field }) => (
            <Checkbox checked={field.value} onCheckedChange={field.onChange} label={t('schema.behavior.half-width')} />
          )}
        />
        <FormField
          labelKey="schema.behavior.section-title"
          hint={t('schema.behavior.section-title-hint')}
          error={errors.sectionTitleAr?.message}
        >
          <Input invalid={Boolean(errors.sectionTitleAr)} {...register('sectionTitleAr')} />
        </FormField>
      </Group>
    </>
  )
}