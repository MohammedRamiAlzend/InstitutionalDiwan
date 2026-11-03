import { Badge, type BadgeVariant } from '@/shared/ui'
import { FIELD_DATA_TYPE_LABELS, SCHEMA_STATUS_LABELS } from '../model/labels'
import { SCHEMA_STATUS, type FieldDataType, type SchemaStatus } from '../model/types'

const STATUS_VARIANTS: Record<SchemaStatus, BadgeVariant> = {
  [SCHEMA_STATUS.Draft]: 'warning',
  [SCHEMA_STATUS.Published]: 'success',
  [SCHEMA_STATUS.Retired]: 'neutral',
}

/** Draft / published / retired schema version status. */
export function SchemaStatusBadge({ status }: { status: SchemaStatus }) {
  return <Badge variant={STATUS_VARIANTS[status] ?? 'outline'}>{SCHEMA_STATUS_LABELS[status]}</Badge>
}

/** The data type of a dynamic field (text, number, date, ...). */
export function FieldTypeBadge({ dataType }: { dataType: FieldDataType }) {
  return <Badge variant="outline">{FIELD_DATA_TYPE_LABELS[dataType]}</Badge>
}
