import { useState } from 'react'
import { Plus, Trash2 } from 'lucide-react'
import { resolveLabel, t, useLabelsStore } from '@/shared/lib'
import { Button, Dialog, Input, Label, Select } from '@/shared/ui'
import {
  MAX_FIELD_FILTERS,
  fieldFilterOperatorLabel,
  operatorsFor,
  validateFieldFilterValue,
  type FieldFilterOperator,
  type FieldFilterValue,
} from '../model/filters'
import { FIELD_DATA_TYPE, type SchemaFieldResponse } from '../model/types'

interface FieldFilterDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  fields: readonly SchemaFieldResponse[]
  initial: readonly FieldFilterValue[]
  onApply: (filters: FieldFilterValue[]) => void
}

interface Row {
  key: string
  operator: FieldFilterOperator
  value: string
}

const BOOLEAN_OPTION_KEYS = [
  { value: 'true', key: 'schema.value.yes' },
  { value: 'false', key: 'schema.value.no' },
] as const

function blankRow(field: SchemaFieldResponse): Row {
  return {
    key: field.key,
    operator: operatorsFor(field.dataType)[0],
    value: field.dataType === FIELD_DATA_TYPE.Boolean ? 'true' : '',
  }
}

/** Editor for up to five dynamic-field filters; nothing is applied until «تطبيق». */
export function FieldFilterDialog({
  open,
  onOpenChange,
  fields,
  initial,
  onApply,
}: FieldFilterDialogProps) {
  const [rows, setRows] = useState<Row[]>(() => initial.map((filter) => ({ ...filter })))
  const [submitted, setSubmitted] = useState(false)
  const overrides = useLabelsStore((state) => state.overrides)
  const booleanOptions = BOOLEAN_OPTION_KEYS.map((option) => ({
    value: option.value,
    label: resolveLabel(option.key, overrides),
  }))

  const fieldByKey = new Map(fields.map((field) => [field.key, field]))
  const fieldOptions = fields.map((field) => ({ value: field.key, label: field.labelAr }))

  const patch = (index: number, change: Partial<Row>) =>
    setRows((current) => current.map((row, i) => (i === index ? { ...row, ...change } : row)))

  const changeField = (index: number, key: string) => {
    const field = fieldByKey.get(key)
    if (field) setRows((current) => current.map((row, i) => (i === index ? blankRow(field) : row)))
  }

  const errorFor = (row: Row): string | null => {
    const field = fieldByKey.get(row.key)
    return field ? validateFieldFilterValue(field.dataType, row.value) : t('schema.filter-dialog.field-required')
  }

  const apply = () => {
    setSubmitted(true)
    if (rows.some((row) => errorFor(row) !== null)) return
    onApply(rows.map((row) => ({ ...row, value: row.value.trim() })))
    onOpenChange(false)
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      size="lg"
      title={t('schema.filter-dialog.title')}
      description={t('schema.filter-dialog.description')}
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            <Label k="common.action.cancel" />
          </Button>
          <Button type="button" onClick={apply}>
            <Label k="schema.filter-dialog.apply" />
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-3">
        {rows.length === 0 && (
          <p className="text-sm text-muted">
            <Label k="schema.filter-dialog.empty" />
          </p>
        )}

        {rows.map((row, index) => {
          const field = fieldByKey.get(row.key)
          const error = submitted ? errorFor(row) : null
          return (
            <div
              key={index}
              className="grid gap-2 rounded-lg border border-border p-3 sm:grid-cols-[1fr_11rem_1fr_auto]"
            >
              <Select
                aria-label={t('schema.filter-dialog.field-aria')}
                value={row.key}
                onValueChange={(key) => changeField(index, key)}
                options={fieldOptions}
              />
              <Select
                aria-label={t('schema.filter-dialog.operator-aria')}
                value={row.operator}
                onValueChange={(operator) =>
                  patch(index, { operator: operator as FieldFilterOperator })
                }
                options={(field ? operatorsFor(field.dataType) : []).map((operator) => ({
                  value: operator,
                  label: field ? fieldFilterOperatorLabel(operator, field.dataType) : operator,
                }))}
              />
              {field?.dataType === FIELD_DATA_TYPE.Boolean ? (
                <Select
                  aria-label={t('schema.filter-dialog.value-aria')}
                  value={row.value}
                  onValueChange={(value) => patch(index, { value })}
                  options={booleanOptions}
                />
              ) : field?.dataType === FIELD_DATA_TYPE.Select ||
                field?.dataType === FIELD_DATA_TYPE.MultiSelect ? (
                <Select
                  aria-label={t('schema.filter-dialog.value-aria')}
                  value={row.value || undefined}
                  onValueChange={(value) => patch(index, { value })}
                  options={field.options.map((option) => ({
                    value: option.key,
                    label: option.labelAr,
                  }))}
                  invalid={Boolean(error)}
                />
              ) : (
                <Input
                  aria-label={t('schema.filter-dialog.value-aria')}
                  dir={
                    field?.dataType === FIELD_DATA_TYPE.Number ||
                    field?.dataType === FIELD_DATA_TYPE.Date
                      ? 'ltr'
                      : undefined
                  }
                  type={field?.dataType === FIELD_DATA_TYPE.Date ? 'date' : 'text'}
                  inputMode={field?.dataType === FIELD_DATA_TYPE.Number ? 'decimal' : undefined}
                  value={row.value}
                  onChange={(event) => patch(index, { value: event.target.value })}
                  invalid={Boolean(error)}
                />
              )}
              <Button
                type="button"
                variant="ghost"
                size="sm"
                aria-label={t('schema.filter-dialog.remove-aria')}
                onClick={() => setRows((current) => current.filter((_, i) => i !== index))}
              >
                <Trash2 className="size-4" aria-hidden />
              </Button>
              {error && (
                <p role="alert" className="text-xs text-danger sm:col-span-4">
                  {error}
                </p>
              )}
            </div>
          )
        })}

        <div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={rows.length >= MAX_FIELD_FILTERS || fields.length === 0}
            onClick={() => setRows((current) => [...current, blankRow(fields[0])])}
          >
            <Plus className="size-4" aria-hidden />
            <Label k="schema.filter-dialog.add" />
          </Button>
          {rows.length >= MAX_FIELD_FILTERS && (
            <span className="ms-2 text-xs text-muted">
              {t('schema.filter-dialog.max-hint', { max: MAX_FIELD_FILTERS })}
            </span>
          )}
        </div>
      </div>
    </Dialog>
  )
}
