import type { LabelKey } from '@/shared/config'
import { t } from '@/shared/lib'
import { FIELD_DATA_TYPE, type FieldDataType } from './types'

/** Operators of the registry's `fieldFilters=key:operator:value` parameter. */
export const FIELD_FILTER_OPERATOR = {
  Eq: 'eq',
  Contains: 'contains',
  Gt: 'gt',
  Gte: 'gte',
  Lt: 'lt',
  Lte: 'lte',
} as const

export type FieldFilterOperator =
  (typeof FIELD_FILTER_OPERATOR)[keyof typeof FIELD_FILTER_OPERATOR]

/** Mirrors `DocumentFieldFilterParser.MaxFilters` on the server. */
export const MAX_FIELD_FILTERS = 5

/** One registry filter on a dynamic field; `value` is the wire text (numbers invariant, dates ISO). */
export interface FieldFilterValue {
  key: string
  operator: FieldFilterOperator
  value: string
}

const { Eq, Contains, Gt, Gte, Lt, Lte } = FIELD_FILTER_OPERATOR

/** Operators the server accepts for each data type (see `DocumentFieldFilterParser`). */
export function operatorsFor(dataType: FieldDataType): readonly FieldFilterOperator[] {
  switch (dataType) {
    case FIELD_DATA_TYPE.Text:
    case FIELD_DATA_TYPE.LongText:
      return [Contains, Eq]
    case FIELD_DATA_TYPE.Number:
    case FIELD_DATA_TYPE.Date:
      return [Eq, Gte, Lte, Gt, Lt]
    default:
      return [Eq]
  }
}

const NUMBER_LABEL_KEYS: Record<FieldFilterOperator, LabelKey> = {
  eq: 'schema.filter-op.eq',
  contains: 'schema.filter-op.contains',
  gt: 'schema.filter-op.gt',
  gte: 'schema.filter-op.gte',
  lt: 'schema.filter-op.lt',
  lte: 'schema.filter-op.lte',
}

const DATE_LABEL_KEYS: Record<FieldFilterOperator, LabelKey> = {
  ...NUMBER_LABEL_KEYS,
  eq: 'schema.filter-op.date-eq',
  gt: 'schema.filter-op.date-gt',
  gte: 'schema.filter-op.date-gte',
  lt: 'schema.filter-op.date-lt',
  lte: 'schema.filter-op.date-lte',
}

export function fieldFilterOperatorLabel(
  operator: FieldFilterOperator,
  dataType: FieldDataType,
): string {
  return t((dataType === FIELD_DATA_TYPE.Date ? DATE_LABEL_KEYS : NUMBER_LABEL_KEYS)[operator])
}

/** The text sent as one `fieldFilters` value. */
export function encodeFieldFilter(filter: FieldFilterValue): string {
  return `${filter.key}:${filter.operator}:${filter.value}`
}

const OPERATORS = new Set<string>(Object.values(FIELD_FILTER_OPERATOR))

/** Splits at the first two colons so values such as ISO times keep theirs; null when malformed. */
export function decodeFieldFilter(raw: string): FieldFilterValue | null {
  const first = raw.indexOf(':')
  const second = first < 0 ? -1 : raw.indexOf(':', first + 1)
  if (first <= 0 || second < 0) return null
  const operator = raw.slice(first + 1, second)
  const value = raw.slice(second + 1)
  if (!OPERATORS.has(operator) || !value) return null
  return { key: raw.slice(0, first), operator: operator as FieldFilterOperator, value }
}

/** Arabic message when `value` cannot be sent for this data type; null when it is acceptable. */
export function validateFieldFilterValue(dataType: FieldDataType, value: string): string | null {
  const text = value.trim()
  if (!text) return t('schema.filter-validation.value-required')
  if (text.length > 500) return t('schema.filter-validation.value-max')
  if (dataType === FIELD_DATA_TYPE.Number && !/^[-+]?\d+(\.\d+)?$/.test(text))
    return t('schema.filter-validation.number-invalid')
  if (dataType === FIELD_DATA_TYPE.Date && Number.isNaN(Date.parse(text)))
    return t('schema.filter-validation.date-invalid')
  return null
}
