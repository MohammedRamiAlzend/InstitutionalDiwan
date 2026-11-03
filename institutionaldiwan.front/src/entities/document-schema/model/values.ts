import {
  FIELD_DATA_TYPE,
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

/** Every field is sent so `PUT` (which replaces all values) clears blanked ones; blank → `null`. */
export function toFieldValueRequests(
  fields: readonly SchemaFieldResponse[],
  values: FieldValueMap,
): FieldValueRequest[] {
  return fields.map((field) => ({
    fieldKey: field.key,
    value: values[field.key]?.trim() ? values[field.key].trim() : null,
  }))
}

/**
 * Client-side mirror of the backend value rules (required, type, length, range, pattern, option) so
 * most mistakes are caught before the request; the server stays the authority.
 */
export function validateFieldValues(
  fields: readonly SchemaFieldResponse[],
  values: FieldValueMap,
): FieldValueErrors {
  const errors: FieldValueErrors = {}

  for (const field of fields) {
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
        break
      case FIELD_DATA_TYPE.Boolean:
        if (raw !== 'true' && raw !== 'false') errors[field.key] = 'اختر نعم أو لا.'
        break
      case FIELD_DATA_TYPE.Select:
        if (!field.options.some((option) => option.key === raw)) {
          errors[field.key] = 'اختر أحد الخيارات.'
        }
        break
    }
  }

  return errors
}
