import { useLabel } from '@/shared/lib'
import { Badge, type BadgeVariant } from '@/shared/ui'
import { FIELD_DATA_TYPE_LABEL_KEYS, SCHEMA_STATUS_LABEL_KEYS } from '../model/labels'
import { SCHEMA_STATUS, type FieldDataType, type SchemaStatus } from '../model/types'

const STATUS_VARIANTS: Record<SchemaStatus, BadgeVariant> = {
  [SCHEMA_STATUS.Draft]: 'warning',
  [SCHEMA_STATUS.Published]: 'success',
  [SCHEMA_STATUS.Retired]: 'neutral',
}

/** Draft / published / retired schema version status. */
export function SchemaStatusBadge({ status }: { status: SchemaStatus }) {
  const label = useLabel(SCHEMA_STATUS_LABEL_KEYS[status])
  return <Badge variant={STATUS_VARIANTS[status] ?? 'outline'}>{label}</Badge>
}

/** The data type of a dynamic field (text, number, date, ...). */
export function FieldTypeBadge({ dataType }: { dataType: FieldDataType }) {
  const label = useLabel(FIELD_DATA_TYPE_LABEL_KEYS[dataType])
  return <Badge variant="outline">{label}</Badge>
}