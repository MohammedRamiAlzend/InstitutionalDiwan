import { useMemo, useState } from 'react'
import { SlidersHorizontal, X } from 'lucide-react'
import { toArabicErrorMessage } from '@/shared/api'
import { cn, formatDate } from '@/shared/lib'
import { Badge, Button, Input, Select } from '@/shared/ui'
import {
  FIELD_FILTER_OPERATOR,
  MAX_FIELD_FILTERS,
  decodeFieldFilter,
  encodeFieldFilter,
  fieldFilterOperatorLabel,
  validateFieldFilterValue,
  type FieldFilterOperator,
  type FieldFilterValue,
} from '../model/filters'
import { useDocumentTypeSchema } from '../model/queries'
import { FIELD_DATA_TYPE, type SchemaFieldResponse } from '../model/types'
import { FieldFilterDialog } from './FieldFilterDialog'

interface FieldFilterBarProps {
  documentTypeId: number | undefined
  /**
   * An explicit field list (for example one department record form version) instead of the document
   * type's creation form. Every field is then offered in the dialog; the bar still shows only the
   * ones marked filterable.
   */
  fields?: readonly SchemaFieldResponse[]
  /** Raw `key:operator:value` URL values. */
  raw: readonly string[]
  onChange: (raw: string[]) => void
}

const ALL = 'all'

function isChoiceType(field: SchemaFieldResponse): boolean {
  return (
    field.dataType === FIELD_DATA_TYPE.Boolean ||
    field.dataType === FIELD_DATA_TYPE.Select ||
    field.dataType === FIELD_DATA_TYPE.MultiSelect
  )
}

/** The operator a filter on the bar itself uses; other operators live in the dialog. */
function quickOperator(field: SchemaFieldResponse): FieldFilterOperator {
  return field.dataType === FIELD_DATA_TYPE.Text || field.dataType === FIELD_DATA_TYPE.LongText
    ? FIELD_FILTER_OPERATOR.Contains
    : FIELD_FILTER_OPERATOR.Eq
}

function displayValue(field: SchemaFieldResponse | undefined, filter: FieldFilterValue): string {
  if (!field) return filter.value
  if (field.dataType === FIELD_DATA_TYPE.Boolean) return filter.value === 'true' ? 'نعم' : 'لا'
  if (field.dataType === FIELD_DATA_TYPE.Select || field.dataType === FIELD_DATA_TYPE.MultiSelect)
    return field.options.find((option) => option.key === filter.value)?.labelAr ?? filter.value
  if (field.dataType === FIELD_DATA_TYPE.Date) return formatDate(filter.value)
  return filter.value
}

interface QuickFilterProps {
  field: SchemaFieldResponse
  value: string
  disabled: boolean
  onCommit: (value: string) => void
}

/** A filter for one field on the bar: a list for choices, otherwise an input applied on Enter or blur. */
function QuickFilter({ field, value, disabled, onCommit }: QuickFilterProps) {
  const [invalid, setInvalid] = useState(false)

  if (isChoiceType(field)) {
    const options =
      field.dataType === FIELD_DATA_TYPE.Boolean
        ? [
            { value: 'true', label: 'نعم' },
            { value: 'false', label: 'لا' },
          ]
        : field.options.map((option) => ({ value: option.key, label: option.labelAr }))
    return (
      <Select
        aria-label={`تصفية حسب ${field.labelAr}`}
        value={value || ALL}
        disabled={disabled}
        onValueChange={(next) => onCommit(next === ALL ? '' : next)}
        options={[{ value: ALL, label: `${field.labelAr}: الكل` }, ...options]}
        className="w-44"
      />
    )
  }

  const isDate = field.dataType === FIELD_DATA_TYPE.Date
  const isNumber = field.dataType === FIELD_DATA_TYPE.Number

  const commit = (text: string) => {
    const trimmed = text.trim()
    const bad = trimmed !== '' && validateFieldFilterValue(field.dataType, trimmed) !== null
    setInvalid(bad)
    if (!bad && trimmed !== value) onCommit(trimmed)
  }

  return (
    <Input
      // Remounts when the URL changes the value, so back/forward and "clear" show through.
      key={value}
      aria-label={`تصفية حسب ${field.labelAr}`}
      title={field.labelAr}
      placeholder={isDate ? undefined : field.labelAr}
      type={isDate ? 'date' : 'text'}
      dir={isDate || isNumber ? 'ltr' : undefined}
      inputMode={isNumber ? 'decimal' : undefined}
      defaultValue={value}
      disabled={disabled}
      invalid={invalid}
      className={cn('h-10', isDate ? 'w-40' : 'w-44')}
      onChange={isDate ? (event) => commit(event.target.value) : undefined}
      onBlur={isDate ? undefined : (event) => commit(event.target.value)}
      onKeyDown={
        isDate
          ? undefined
          : (event) => {
              if (event.key === 'Enter') {
                event.preventDefault()
                commit(event.currentTarget.value)
              }
            }
      }
    />
  )
}

/**
 * Dynamic-field filters of a document table: the fields marked filterable sit on the bar, and
 * «تصفية بالحقول» opens every operator. All of it is the table's URL state.
 */
export function FieldFilterBar({ documentTypeId, fields: explicitFields, raw, onChange }: FieldFilterBarProps) {
  const [open, setOpen] = useState(false)
  const schemaQuery = useDocumentTypeSchema(explicitFields ? undefined : documentTypeId)
  const fields = useMemo(
    () => explicitFields ?? schemaQuery.data?.schema?.fields ?? [],
    [explicitFields, schemaQuery.data],
  )
  const filters = useMemo(
    () => raw.map(decodeFieldFilter).filter((filter): filter is FieldFilterValue => filter !== null),
    [raw],
  )

  const filterable = fields.filter((field) => field.behavior.isFilterable)
  // A shared link may filter on a field no longer marked filterable; it must stay editable.
  const dialogFields = fields.filter(
    (field) =>
      explicitFields !== undefined ||
      field.behavior.isFilterable ||
      filters.some((filter) => filter.key === field.key),
  )

  const quickFilterOf = (field: SchemaFieldResponse) =>
    filters.find((filter) => filter.key === field.key && filter.operator === quickOperator(field))

  const onBar = new Set(filterable.map((field) => quickFilterOf(field)).filter(Boolean))
  const chips = filters.filter((filter) => !onBar.has(filter))

  const setQuick = (field: SchemaFieldResponse, value: string) => {
    const current = quickFilterOf(field)
    const rest = current ? raw.filter((item) => item !== encodeFieldFilter(current)) : [...raw]
    onChange(
      value
        ? [...rest, encodeFieldFilter({ key: field.key, operator: quickOperator(field), value })]
        : rest,
    )
  }

  const hint =
    documentTypeId === undefined && explicitFields === undefined
      ? 'اختر نوع وثيقة أولًا.'
      : schemaQuery.isLoading
        ? 'جارٍ تحميل حقول النوع…'
        : dialogFields.length === 0
          ? explicitFields !== undefined
            ? 'لا حقول قابلة للتصفية في هذا النموذج.'
            : 'لا حقول قابلة للتصفية لهذا النوع.'
          : undefined

  return (
    <>
      <Button
        type="button"
        variant="outline"
        size="sm"
        disabled={hint !== undefined}
        title={hint}
        onClick={() => setOpen(true)}
      >
        <SlidersHorizontal className="size-4" aria-hidden />
        تصفية بالحقول{filters.length > 0 ? ` (${filters.length})` : ''}
      </Button>

      {filterable.map((field) => {
        const active = quickFilterOf(field)
        return (
          <QuickFilter
            key={field.key}
            field={field}
            value={active?.value ?? ''}
            disabled={!active && filters.length >= MAX_FIELD_FILTERS}
            onCommit={(value) => setQuick(field, value)}
          />
        )
      })}

      {chips.map((filter) => {
        const field = fields.find((candidate) => candidate.key === filter.key)
        return (
          <Badge key={encodeFieldFilter(filter)} variant="outline" className="gap-1">
            <span>
              {field?.labelAr ?? filter.key}{' '}
              {field ? fieldFilterOperatorLabel(filter.operator, field.dataType) : filter.operator}{' '}
              <span dir="ltr">{displayValue(field, filter)}</span>
            </span>
            <button
              type="button"
              aria-label="إزالة المرشح"
              className="cursor-pointer"
              onClick={() => onChange(raw.filter((item) => item !== encodeFieldFilter(filter)))}
            >
              <X className="size-3" aria-hidden />
            </button>
          </Badge>
        )
      })}

      {schemaQuery.isError && (
        <span className="text-xs text-danger">{toArabicErrorMessage(schemaQuery.error)}</span>
      )}

      {open && (
        <FieldFilterDialog
          open
          onOpenChange={setOpen}
          fields={dialogFields}
          initial={filters}
          onApply={(next) => onChange(next.map(encodeFieldFilter))}
        />
      )}
    </>
  )
}
