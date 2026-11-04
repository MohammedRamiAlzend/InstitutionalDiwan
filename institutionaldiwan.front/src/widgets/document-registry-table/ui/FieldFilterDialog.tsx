import { useState } from 'react'
import { Plus, Trash2 } from 'lucide-react'
import {
  FIELD_DATA_TYPE,
  MAX_FIELD_FILTERS,
  fieldFilterOperatorLabel,
  operatorsFor,
  validateFieldFilterValue,
  type FieldFilterOperator,
  type FieldFilterValue,
  type SchemaFieldResponse,
} from '@/entities/document-schema'
import { Button, Dialog, Input, Select } from '@/shared/ui'

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

const BOOLEAN_OPTIONS = [
  { value: 'true', label: 'نعم' },
  { value: 'false', label: 'لا' },
]

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
    return field ? validateFieldFilterValue(field.dataType, row.value) : 'اختر حقلًا.'
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
      title="تصفية بقيم الحقول"
      description="تُجمع المرشحات بـ «و»: تظهر الوثائق التي تحقق كلها. الوثائق التي لا تحمل قيمة لحقل ما لا تطابق مرشحه."
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            إلغاء
          </Button>
          <Button type="button" onClick={apply}>
            تطبيق
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-3">
        {rows.length === 0 && (
          <p className="text-sm text-muted">لا توجد مرشحات. أضف مرشحًا للبدء.</p>
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
                aria-label="الحقل"
                value={row.key}
                onValueChange={(key) => changeField(index, key)}
                options={fieldOptions}
              />
              <Select
                aria-label="المعامل"
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
                  aria-label="القيمة"
                  value={row.value}
                  onValueChange={(value) => patch(index, { value })}
                  options={BOOLEAN_OPTIONS}
                />
              ) : field?.dataType === FIELD_DATA_TYPE.Select ? (
                <Select
                  aria-label="القيمة"
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
                  aria-label="القيمة"
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
                aria-label="حذف المرشح"
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
            إضافة مرشح
          </Button>
          {rows.length >= MAX_FIELD_FILTERS && (
            <span className="ms-2 text-xs text-muted">الحد الأقصى {MAX_FIELD_FILTERS} مرشحات.</span>
          )}
        </div>
      </div>
    </Dialog>
  )
}
