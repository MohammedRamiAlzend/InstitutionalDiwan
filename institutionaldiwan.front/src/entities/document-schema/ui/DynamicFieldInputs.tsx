import { Fragment } from 'react'
import { cn } from '@/shared/lib'
import { FormField } from '@/shared/ui'
import { visibleFields, type FieldValueErrors, type FieldValueMap } from '../model/values'
import type { SchemaFieldResponse } from '../model/types'
import { FieldValueControl } from './FieldValueControl'

interface DynamicFieldInputsProps {
  fields: readonly SchemaFieldResponse[]
  values: FieldValueMap
  errors?: FieldValueErrors
  disabled?: boolean
  onChange: (fieldKey: string, value: string) => void
}

/**
 * Renders one control per visible schema field, in the order the server returned them, under the
 * section headings the schema defines. State lives in the caller (a key → text map), so the same
 * inputs serve the create dialog and the details panel.
 */
export function DynamicFieldInputs({
  fields,
  values,
  errors = {},
  disabled = false,
  onChange,
}: DynamicFieldInputsProps) {
  const shown = visibleFields(fields, values)

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {shown.map((field, index) => {
        const { behavior } = field
        const error = errors[field.key]
        const sectionStarts =
          behavior.sectionTitleAr !== null &&
          behavior.sectionTitleAr !== shown[index - 1]?.behavior.sectionTitleAr
        const hint = behavior.helpTextAr ?? (behavior.isReadOnly ? 'قيمة ثابتة لا تُعدَّل.' : undefined)

        return (
          <Fragment key={field.documentFieldId}>
            {sectionStarts && (
              <h3 className="border-b border-border pb-1 text-sm font-semibold text-foreground sm:col-span-2">
                {behavior.sectionTitleAr}
              </h3>
            )}
            <FormField
              label={field.labelAr}
              required={field.isRequired && !behavior.isReadOnly}
              error={error}
              hint={hint}
              className={cn(!behavior.isHalfWidth && 'sm:col-span-2')}
            >
              <FieldValueControl
                field={field}
                value={values[field.key] ?? ''}
                disabled={disabled || behavior.isReadOnly}
                invalid={Boolean(error)}
                onChange={(value) => onChange(field.key, value)}
              />
            </FormField>
          </Fragment>
        )
      })}
    </div>
  )
}
