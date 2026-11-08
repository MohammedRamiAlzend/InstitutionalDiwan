import { useMemo, useState } from 'react'
import { Checkbox, Input, Select, Textarea } from '@/shared/ui'
import { splitChoices } from '../model/values'
import { FIELD_DATA_TYPE, type SchemaFieldResponse } from '../model/types'

interface FieldValueControlProps {
  field: SchemaFieldResponse
  value: string
  disabled?: boolean
  invalid?: boolean
  onChange: (value: string) => void
}

const YES_NO = [
  { value: 'true', label: 'نعم' },
  { value: 'false', label: 'لا' },
]

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
          aria-label={`البحث في خيارات ${field.labelAr}`}
          placeholder="ابحث في الخيارات…"
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
        {shown.length === 0 && <p className="text-xs text-muted">لا توجد خيارات.</p>}
      </div>
    </div>
  )
}

/** The input for one field's data type, over the text form the API exchanges. */
export function FieldValueControl(props: FieldValueControlProps) {
  const { field, value, disabled = false, invalid = false, onChange } = props
  const placeholder = field.behavior.placeholderAr ?? undefined

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
          placeholder={placeholder ?? 'غير محدد'}
          options={YES_NO}
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
          placeholder={placeholder ?? 'اختر…'}
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
