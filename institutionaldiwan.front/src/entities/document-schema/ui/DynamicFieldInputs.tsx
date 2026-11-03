import { FormField, Input, Select, Textarea } from '@/shared/ui'
import type { FieldValueErrors, FieldValueMap } from '../model/values'
import { FIELD_DATA_TYPE, type SchemaFieldResponse } from '../model/types'

interface DynamicFieldInputsProps {
  fields: readonly SchemaFieldResponse[]
  values: FieldValueMap
  errors?: FieldValueErrors
  disabled?: boolean
  onChange: (fieldKey: string, value: string) => void
}

const YES_NO = [
  { value: 'true', label: 'نعم' },
  { value: 'false', label: 'لا' },
]

/**
 * Renders one control per schema field, in the order the server returned them. State lives in the
 * caller (a key → text map), so the same inputs serve the create dialog and the details panel.
 */
export function DynamicFieldInputs({
  fields,
  values,
  errors = {},
  disabled = false,
  onChange,
}: DynamicFieldInputsProps) {
  return (
    <>
      {fields.map((field) => {
        const value = values[field.key] ?? ''
        const error = errors[field.key]

        return (
          <FormField key={field.documentFieldId} label={field.labelAr} required={field.isRequired} error={error}>
            {field.dataType === FIELD_DATA_TYPE.LongText ? (
              <Textarea
                rows={3}
                value={value}
                maxLength={field.maxLength ?? undefined}
                disabled={disabled}
                invalid={Boolean(error)}
                onChange={(event) => onChange(field.key, event.target.value)}
              />
            ) : field.dataType === FIELD_DATA_TYPE.Number ? (
              <Input
                dir="ltr"
                inputMode="decimal"
                value={value}
                disabled={disabled}
                invalid={Boolean(error)}
                onChange={(event) => onChange(field.key, event.target.value)}
              />
            ) : field.dataType === FIELD_DATA_TYPE.Date ? (
              <Input
                dir="ltr"
                type="date"
                value={value}
                disabled={disabled}
                invalid={Boolean(error)}
                onChange={(event) => onChange(field.key, event.target.value)}
              />
            ) : field.dataType === FIELD_DATA_TYPE.Boolean ? (
              <Select
                aria-label={field.labelAr}
                value={value || undefined}
                disabled={disabled}
                invalid={Boolean(error)}
                placeholder="غير محدد"
                options={YES_NO}
                onValueChange={(next) => onChange(field.key, next)}
              />
            ) : field.dataType === FIELD_DATA_TYPE.Select ? (
              <Select
                aria-label={field.labelAr}
                value={value || undefined}
                disabled={disabled}
                invalid={Boolean(error)}
                placeholder="اختر…"
                options={field.options.map((option) => ({ value: option.key, label: option.labelAr }))}
                onValueChange={(next) => onChange(field.key, next)}
              />
            ) : (
              <Input
                value={value}
                maxLength={field.maxLength ?? undefined}
                disabled={disabled}
                invalid={Boolean(error)}
                onChange={(event) => onChange(field.key, event.target.value)}
              />
            )}
          </FormField>
        )
      })}
    </>
  )
}

