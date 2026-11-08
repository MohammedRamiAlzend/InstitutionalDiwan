import { formatDate } from '@/shared/lib'
import {
  FIELD_DATA_TYPE,
  FIELD_LIMITS,
  TODAY_TOKEN,
  type DocumentFieldValueResponse,
  type SchemaFieldResponse,
} from './types'

/** Form state of the dynamic fields: field key → the text the API exchanges (empty string = no value). */
export type FieldValueMap = Record<string, string>

/** Field key → Arabic error message. */
export type FieldValueErrors = Record<string, string>

/** `PUT /api/documents/{id}/fields` and `fieldValues` entry. */
export interface FieldValueRequest {
  fieldKey: string
  value: string | null
}

/** The API returns numbers with fixed decimals (`500.00000000`); show them as typed (`500`). */
export function normalizeNumberText(value: string): string {
  return /^-?\d+\.\d+$/.test(value) ? value.replace(/\.?0+$/, '') : value
}

/** Today as the user's own calendar shows it, in the `yyyy-MM-dd` form date inputs use. */
export function localToday(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

function resolveToken(text: string): string {
  return text === TODAY_TOKEN ? localToday() : text
}

/** A multi-select value travels as its option keys joined by commas. */
export function splitChoices(value: string | null | undefined): string[] {
  return (value ?? '')
    .split(',')
    .map((key) => key.trim())
    .filter(Boolean)
}

/** Seeds a form from stored values; dates are shown as `yyyy-MM-dd` for the date input. */
export function toFieldValueMap(fields: readonly DocumentFieldValueResponse[]): FieldValueMap {
  const map: FieldValueMap = {}
  for (const { field, value } of fields) {
    map[field.key] = !value
      ? ''
      : field.dataType === FIELD_DATA_TYPE.Date
        ? value.slice(0, 10)
        : field.dataType === FIELD_DATA_TYPE.Number
          ? normalizeNumberText(value)
          : value
  }
  return map
}

/** Shows a read-only field's fixed value where nothing is stored; the server writes it either way. */
export function withFixedValues(
  fields: readonly SchemaFieldResponse[],
  values: FieldValueMap,
): FieldValueMap {
  const next = { ...values }
  for (const { key, behavior } of fields) {
    if (behavior.isReadOnly && behavior.defaultValue && !next[key]?.trim()) {
      next[key] = resolveToken(behavior.defaultValue)
    }
  }
  return next
}

/**
 * The values of a new form: defaults fill only the keys the user has not touched, so a default the
 * user cleared (stored as an empty string) stays cleared. Computed on read, never stored, so an
 * untouched form is not mistaken for typed input.
 */
export function withFieldDefaults(
  fields: readonly SchemaFieldResponse[],
  values: FieldValueMap,
): FieldValueMap {
  const next = withFixedValues(fields, values)
  for (const { key, behavior } of fields) {
    if (behavior.defaultValue && !(key in next)) next[key] = resolveToken(behavior.defaultValue)
  }
  return next
}

/** The fields whose visibility condition holds, following chains of dependent fields. */
export function visibleFields(
  fields: readonly SchemaFieldResponse[],
  values: FieldValueMap,
): SchemaFieldResponse[] {
  const byKey = new Map(fields.map((field) => [field.key, field]))

  const isVisible = (field: SchemaFieldResponse, depth: number): boolean => {
    const { visibleWhenFieldKey: controllerKey, visibleWhenValue: expected } = field.behavior
    if (!controllerKey || expected === null) return true
    // The server refuses circles when the schema is edited; the bound only guards bad data.
    if (depth > fields.length) return false

    const controller = byKey.get(controllerKey)
    if (!controller) return true
    if (!isVisible(controller, depth + 1)) return false

    const raw = (values[controllerKey] ?? '').trim()
    return controller.dataType === FIELD_DATA_TYPE.MultiSelect
      ? splitChoices(raw).includes(expected)
      : raw === expected
  }

  return fields.filter((field) => isVisible(field, 0))
}

/**
 * Hidden fields are sent blank so `PUT` (which replaces all values) clears them; read-only fields
 * are left out because the server writes those itself. Blank → `null`.
 */
export function toFieldValueRequests(
  fields: readonly SchemaFieldResponse[],
  values: FieldValueMap,
): FieldValueRequest[] {
  const shown = new Set(visibleFields(fields, values).map((field) => field.key))
  return fields
    .filter((field) => !field.behavior.isReadOnly)
    .map((field) => ({
      fieldKey: field.key,
      value: shown.has(field.key) && values[field.key]?.trim() ? values[field.key].trim() : null,
    }))
}

/** One stored value as a person reads it: labels instead of keys, formatted dates and numbers. */
export function formatFieldValue(field: SchemaFieldResponse, value: string | null | undefined): string {
  if (!value) return '—'
  const labelOf = (key: string) => field.options.find((option) => option.key === key)?.labelAr ?? key
  switch (field.dataType) {
    case FIELD_DATA_TYPE.Boolean:
      return value === 'true' ? 'نعم' : 'لا'
    case FIELD_DATA_TYPE.Select:
      return labelOf(value)
    case FIELD_DATA_TYPE.MultiSelect:
      return splitChoices(value).map(labelOf).join('، ')
    case FIELD_DATA_TYPE.Date:
      return formatDate(value)
    case FIELD_DATA_TYPE.Number:
      return normalizeNumberText(value)
    default:
      return value
  }
}

/**
 * Client-side mirror of the backend value rules (required, type, length, range, pattern, option) so
 * most mistakes are caught before the request; the server stays the authority. Hidden and read-only
 * fields are the server's to fill, so they are not judged here.
 */
export function validateFieldValues(
  fields: readonly SchemaFieldResponse[],
  values: FieldValueMap,
): FieldValueErrors {
  const errors: FieldValueErrors = {}

  for (const field of visibleFields(fields, values)) {
    if (field.behavior.isReadOnly) continue
    const raw = (values[field.key] ?? '').trim()

    if (!raw) {
      // A boolean field always carries a value once touched; blank means "not answered".
      if (field.isRequired) errors[field.key] = 'هذا الحقل مطلوب.'
      continue
    }

    switch (field.dataType) {
      case FIELD_DATA_TYPE.Text:
      case FIELD_DATA_TYPE.LongText: {
        if (field.maxLength !== null && raw.length > field.maxLength) {
          errors[field.key] = `الحد الأقصى ${field.maxLength} حرفًا.`
        } else if (field.behavior.minLength !== null && raw.length < field.behavior.minLength) {
          errors[field.key] = `الحد الأدنى ${field.behavior.minLength} أحرف.`
        } else if (field.regexPattern) {
          try {
            if (!new RegExp(field.regexPattern).test(raw)) {
              errors[field.key] = field.regexErrorMessageAr ?? 'القيمة لا تطابق النمط المطلوب.'
            }
          } catch {
            // An unparsable pattern is the server's call (its regex dialect differs); skip locally.
          }
        }
        break
      }
      case FIELD_DATA_TYPE.Number: {
        const n = Number(raw)
        if (!Number.isFinite(n)) errors[field.key] = 'أدخل رقمًا صحيحًا.'
        else if (field.minNumber !== null && n < field.minNumber) {
          errors[field.key] = `القيمة لا تقل عن ${field.minNumber}.`
        } else if (field.maxNumber !== null && n > field.maxNumber) {
          errors[field.key] = `القيمة لا تزيد عن ${field.maxNumber}.`
        }
        break
      }
      case FIELD_DATA_TYPE.Date:
        if (Number.isNaN(Date.parse(raw))) errors[field.key] = 'أدخل تاريخًا صحيحًا.'
        else {
          // ISO dates order correctly as text.
          const day = raw.slice(0, 10)
          const { minDate, maxDate } = field.behavior
          if (minDate && day < resolveToken(minDate).slice(0, 10)) {
            errors[field.key] = `التاريخ لا يسبق ${formatDate(resolveToken(minDate))}.`
          } else if (maxDate && day > resolveToken(maxDate).slice(0, 10)) {
            errors[field.key] = `التاريخ لا يتجاوز ${formatDate(resolveToken(maxDate))}.`
          }
        }
        break
      case FIELD_DATA_TYPE.Boolean:
        if (raw !== 'true' && raw !== 'false') errors[field.key] = 'اختر نعم أو لا.'
        break
      case FIELD_DATA_TYPE.Select:
        if (!field.options.some((option) => option.key === raw)) {
          errors[field.key] = 'اختر أحد الخيارات.'
        }
        break
      case FIELD_DATA_TYPE.MultiSelect: {
        const keys = splitChoices(raw)
        if (keys.length > FIELD_LIMITS.maxSelections) {
          errors[field.key] = `لا تزيد الخيارات عن ${FIELD_LIMITS.maxSelections}.`
        } else if (keys.some((key) => !field.options.some((option) => option.key === key))) {
          errors[field.key] = 'أحد الخيارات لم يعد متاحًا.'
        }
        break
      }
    }
  }

  return errors
}
