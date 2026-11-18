import { useMemo, useState } from 'react'
import { resolveLabel, t, useLabelsStore } from '@/shared/lib'
import { Checkbox, Input, Label, Select, Textarea } from '@/shared/ui'
import { splitChoices } from '../model/values'
import { FIELD_DATA_TYPE, type SchemaFieldResponse } from '../model/types'

interface FieldValueControlProps {
  field: SchemaFieldResponse
  value: string
  disabled?: boolean
  invalid?: boolean
  onChange: (value: string) => void
}

/** Long lists (a source such as the departments) get a search box. */
const SEARCH_THRESHOLD = 8

function MultiSelectControl({ field, value, disabled, onChange }: FieldValueControlProps) {
  const [search, setSearch] = useState('')
  const selected = useMemo(() => new Set(splitChoices(value)), [value])
  // A deactivated choice stays listed only while it is still selected.
  const options = field.options.filter(
    (option) => option.isActive !== false || selected.has(option.key),
  )
  const term = search.trim()
  const shown = term ? options.filter((option) => option.labelAr.includes(term)) : options

  const toggle = (key: string, checked: boolean) => {
    const next = new Set(selected)
    if (checked) next.add(key)
    else next.delete(key)
    onChange([...next].join(','))
  }

  return (
    <div className="flex flex-col gap-2 rounded-lg border border-border p-2">
      {options.length > SEARCH_THRESHOLD && (
        <Input
          aria-label={t('schema.field.search-options-aria', { label: field.labelAr })}
          placeholder={t('schema.field.search-options-placeholder')}
          value={search}
          disabled={disabled}
          onChange={(event) => setSearch(event.target.value)}
        />
      )}
      <div className="flex max-h-44 flex-col gap-2 overflow-y-auto">
        {shown.map((option) => (
          <Checkbox
            key={option.key}
            label={option.labelAr}
            checked={selected.has(option.key)}
            disabled={disabled}
            onCheckedChange={(checked) => toggle(option.key, checked)}
          />
        ))}
        {shown.length === 0 && (
          <p className="text-xs text-muted">
            <Label k="schema.field.no-options" />
          </p>
        )}
      </div>
    </div>
  )
}

/** The input for one field's data type, over the text form the API exchanges. */
export function FieldValueControl(props: FieldValueControlProps) {
  const { field, value, disabled = false, invalid = false, onChange } = props
  const placeholder = field.behavior.placeholderAr ?? undefined
  const overrides = useLabelsStore((state) => state.overrides)
  const yesNo = [
    { value: 'true', label: resolveLabel('schema.value.yes', overrides) },
    { value: 'false', label: resolveLabel('schema.value.no', overrides) },
  ]

  switch (field.dataType) {
    case FIELD_DATA_TYPE.LongText:
      return (
        <Textarea
          rows={3}
          value={value}
          placeholder={placeholder}
          maxLength={field.maxLength ?? undefined}
          disabled={disabled}
          invalid={invalid}
          onChange={(event) => onChange(event.target.value)}
        />
      )
    case FIELD_DATA_TYPE.Number:
      return (
        <Input
          dir="ltr"
          inputMode="decimal"
          value={value}
          placeholder={placeholder}
          disabled={disabled}
          invalid={invalid}
          onChange={(event) => onChange(event.target.value)}
        />
      )
    case FIELD_DATA_TYPE.Date:
      return (
        <Input
          dir="ltr"
          type="date"
          value={value}
          disabled={disabled}
          invalid={invalid}
          onChange={(event) => onChange(event.target.value)}
        />
      )
    case FIELD_DATA_TYPE.Boolean:
      return (
        <Select
          aria-label={field.labelAr}
          value={value || undefined}
          disabled={disabled}
          invalid={invalid}
          placeholder={placeholder ?? t('schema.field.boolean-placeholder')}
          options={yesNo}
          onValueChange={onChange}
        />
      )
    case FIELD_DATA_TYPE.Select:
      return (
        <Select
          aria-label={field.labelAr}
          value={value || undefined}
          disabled={disabled}
          invalid={invalid}
          placeholder={placeholder ?? t('schema.field.select-placeholder')}
          options={field.options
            .filter((option) => option.isActive !== false || option.key === value)
            .map((option) => ({ value: option.key, label: option.labelAr }))}
          onValueChange={onChange}
        />
      )
    case FIELD_DATA_TYPE.MultiSelect:
      return <MultiSelectControl {...props} />
    default:
      return (
        <Input
          value={value}
          placeholder={placeholder}
          maxLength={field.maxLength ?? undefined}
          disabled={disabled}
          invalid={invalid}
          onChange={(event) => onChange(event.target.value)}
        />
      )
  }
}
