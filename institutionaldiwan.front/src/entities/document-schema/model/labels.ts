import { FIELD_DATA_TYPE, SCHEMA_STATUS, type FieldDataType, type SchemaStatus } from './types'

export const FIELD_DATA_TYPE_LABELS: Record<FieldDataType, string> = {
  [FIELD_DATA_TYPE.Text]: 'نص',
  [FIELD_DATA_TYPE.LongText]: 'نص طويل',
  [FIELD_DATA_TYPE.Number]: 'رقم',
  [FIELD_DATA_TYPE.Date]: 'تاريخ',
  [FIELD_DATA_TYPE.Boolean]: 'نعم / لا',
  [FIELD_DATA_TYPE.Select]: 'قائمة اختيار',
}

export const SCHEMA_STATUS_LABELS: Record<SchemaStatus, string> = {
  [SCHEMA_STATUS.Draft]: 'مسودة',
  [SCHEMA_STATUS.Published]: 'منشورة',
  [SCHEMA_STATUS.Retired]: 'متقاعدة',
}
