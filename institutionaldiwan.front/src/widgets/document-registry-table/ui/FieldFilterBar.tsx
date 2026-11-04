import { useMemo, useState } from 'react'
import { SlidersHorizontal, X } from 'lucide-react'
import {
  FIELD_DATA_TYPE,
  decodeFieldFilter,
  encodeFieldFilter,
  fieldFilterOperatorLabel,
  useDocumentTypeSchema,
  type FieldFilterValue,
  type SchemaFieldResponse,
} from '@/entities/document-schema'
import { toArabicErrorMessage } from '@/shared/api'
import { formatDate } from '@/shared/lib'
import { Badge, Button, GuideChip } from '@/shared/ui'
import { FieldFilterDialog } from './FieldFilterDialog'

interface FieldFilterBarProps {
  documentTypeId: number | undefined
  /** Raw `key:operator:value` URL values. */
  raw: readonly string[]
  onChange: (raw: string[]) => void
}

function displayValue(field: SchemaFieldResponse | undefined, filter: FieldFilterValue): string {
  if (!field) return filter.value
  if (field.dataType === FIELD_DATA_TYPE.Boolean) return filter.value === 'true' ? 'نعم' : 'لا'
  if (field.dataType === FIELD_DATA_TYPE.Select)
    return field.options.find((option) => option.key === filter.value)?.labelAr ?? filter.value
  if (field.dataType === FIELD_DATA_TYPE.Date) return formatDate(filter.value)
  return filter.value
}

/** «تصفية بالحقول» button and the active field-filter chips of the document registry. */
export function FieldFilterBar({ documentTypeId, raw, onChange }: FieldFilterBarProps) {
  const [open, setOpen] = useState(false)
  const schemaQuery = useDocumentTypeSchema(documentTypeId)
  const fields = useMemo(
    () => schemaQuery.data?.schema?.fields ?? [],
    [schemaQuery.data],
  )
  const filters = useMemo(
    () => raw.map(decodeFieldFilter).filter((filter): filter is FieldFilterValue => filter !== null),
    [raw],
  )

  const hint =
    documentTypeId === undefined
      ? 'اختر نوع وثيقة أولًا.'
      : schemaQuery.isLoading
        ? 'جارٍ تحميل حقول النوع…'
        : fields.length === 0
          ? 'لا حقول إضافية لهذا النوع.'
          : undefined

  return (
    <>
      <GuideChip id="documents.field-filters" />
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

      {filters.map((filter) => {
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
              onClick={() =>
                onChange(raw.filter((item) => item !== encodeFieldFilter(filter)))
              }
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
          fields={fields}
          initial={filters}
          onApply={(next) => onChange(next.map(encodeFieldFilter))}
        />
      )}
    </>
  )
}
