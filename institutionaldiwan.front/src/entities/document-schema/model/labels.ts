import type { LabelKey } from '@/shared/config'
import { FIELD_DATA_TYPE, SCHEMA_STATUS, type FieldDataType, type SchemaStatus } from './types'

export const FIELD_DATA_TYPE_LABEL_KEYS: Record<FieldDataType, LabelKey> = {
  [FIELD_DATA_TYPE.Text]: 'schema.data-type.text',
  [FIELD_DATA_TYPE.LongText]: 'schema.data-type.long-text',
  [FIELD_DATA_TYPE.Number]: 'schema.data-type.number',
  [FIELD_DATA_TYPE.Date]: 'schema.data-type.date',
  [FIELD_DATA_TYPE.Boolean]: 'schema.data-type.boolean',
  [FIELD_DATA_TYPE.Select]: 'schema.data-type.select',
  [FIELD_DATA_TYPE.MultiSelect]: 'schema.data-type.multi-select',
}

export const SCHEMA_STATUS_LABEL_KEYS: Record<SchemaStatus, LabelKey> = {
  [SCHEMA_STATUS.Draft]: 'schema.status.draft',
  [SCHEMA_STATUS.Published]: 'schema.status.published',
  [SCHEMA_STATUS.Retired]: 'schema.status.retired',
}