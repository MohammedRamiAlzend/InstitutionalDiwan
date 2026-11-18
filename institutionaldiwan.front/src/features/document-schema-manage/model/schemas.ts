import { z } from 'zod'
import {
  FIELD_DATA_TYPE,
  FIELD_LIMITS,
  FIELD_OPTION_SOURCE,
  type FieldDataType,
} from '@/entities/document-schema'
import { t } from '@/shared/lib'

const KEY_PATTERN = /^[a-z][a-z0-9_]*$/

/** Mirrors `DocumentFieldSettings.Create`: which settings apply depends on the field's data type. */
const optionSchema = z.object({
  key: z.string().trim().regex(KEY_PATTERN, t('schema.validation.option-key-format')),
  labelAr: z.string().trim().min(1, t('schema.validation.option-label-required')),
})

const settingsShape = z.object({
  labelAr: z
    .string()
    .trim()
    .min(1, t('schema.validation.label-required'))
    .max(FIELD_LIMITS.maxLabelLength, t('schema.validation.label-max')),
  isRequired: z.boolean(),
  displayOrder: z.string().trim().regex(/^\d{1,4}$/, t('schema.validation.display-order')),
  regexPattern: z.string().max(FIELD_LIMITS.maxRegexLength, t('schema.validation.regex-max')),
  regexErrorMessageAr: z
    .string()
    .trim()
    .max(FIELD_LIMITS.maxRegexMessageLength, t('schema.validation.regex-message-max')),
  maxLength: z.string().trim(),
  minNumber: z.string().trim(),
  maxNumber: z.string().trim(),
  options: z.array(optionSchema),
  defaultValue: z.string(),
  defaultToday: z.boolean(),
  isReadOnly: z.boolean(),
  isFilterable: z.boolean(),
  showInTable: z.boolean(),
  placeholderAr: z.string().trim().max(FIELD_LIMITS.maxPlaceholderLength, t('schema.validation.placeholder-max')),
  helpTextAr: z.string().trim().max(FIELD_LIMITS.maxHelpTextLength, t('schema.validation.help-text-max')),
  minLength: z.string().trim(),
  minDate: z.string(),
  maxDate: z.string(),
  minDateToday: z.boolean(),
  maxDateToday: z.boolean(),
  visibleWhenFieldKey: z.string(),
  visibleWhenValue: z.string(),
  isHalfWidth: z.boolean(),
  sectionTitleAr: z.string().trim().max(FIELD_LIMITS.maxSectionTitleLength, t('schema.validation.section-title-max')),
  optionSource: z.string(),
  lookupCatalog: z.string(),
})

export type FieldSettingsFormValues = z.infer<typeof settingsShape>

function isNumeric(value: string) {
  return value !== '' && Number.isFinite(Number(value))
}

/** The settings schema for one data type; limits and optional parts follow the backend rules. */
export function settingsSchemaFor(dataType: FieldDataType) {
  return settingsShape.superRefine((values, ctx) => {
    const isText = dataType === FIELD_DATA_TYPE.Text || dataType === FIELD_DATA_TYPE.LongText

    if (values.regexPattern.trim()) {
      if (!isText) ctx.addIssue({ code: 'custom', path: ['regexPattern'], message: t('schema.validation.regex-text-only') })
      else {
        try {
          new RegExp(values.regexPattern)
        } catch {
          ctx.addIssue({ code: 'custom', path: ['regexPattern'], message: t('schema.validation.regex-invalid') })
        }
        if (!values.regexErrorMessageAr) {
          ctx.addIssue({
            code: 'custom',
            path: ['regexErrorMessageAr'],
            message: t('schema.validation.regex-message-required'),
          })
        }
      }
    }

    if (values.maxLength) {
      const limit =
        dataType === FIELD_DATA_TYPE.Text ? FIELD_LIMITS.maxTextLength : FIELD_LIMITS.maxLongTextLength
      const n = Number(values.maxLength)
      if (!isText) ctx.addIssue({ code: 'custom', path: ['maxLength'], message: t('schema.validation.max-length-text-only') })
      else if (!Number.isInteger(n) || n < 1 || n > limit) {
        ctx.addIssue({ code: 'custom', path: ['maxLength'], message: t('schema.validation.max-length-range', { limit }) })
      }
    }

    const isNumber = dataType === FIELD_DATA_TYPE.Number
    for (const name of ['minNumber', 'maxNumber'] as const) {
      if (values[name] && (!isNumber || !isNumeric(values[name]))) {
        ctx.addIssue({
          code: 'custom',
          path: [name],
          message: isNumber ? t('schema.validation.number-invalid') : t('schema.validation.bounds-number-only'),
        })
      }
    }
    if (isNumeric(values.minNumber) && isNumeric(values.maxNumber) && Number(values.minNumber) > Number(values.maxNumber)) {
      ctx.addIssue({ code: 'custom', path: ['minNumber'], message: t('schema.validation.min-gt-max') })
    }

    const isChoice = dataType === FIELD_DATA_TYPE.Select || dataType === FIELD_DATA_TYPE.MultiSelect
    const isStatic = !isChoice || values.optionSource === String(FIELD_OPTION_SOURCE.Static)

    if (isChoice && !isStatic) {
      if (values.optionSource === String(FIELD_OPTION_SOURCE.Lookup) && !values.lookupCatalog) {
        ctx.addIssue({ code: 'custom', path: ['lookupCatalog'], message: t('schema.validation.lookup-required') })
      }
    } else if (isChoice) {
      if (values.options.length === 0) {
        ctx.addIssue({ code: 'custom', path: ['options'], message: t('schema.validation.options-min') })
      }
      if (values.options.length > FIELD_LIMITS.maxOptions) {
        ctx.addIssue({ code: 'custom', path: ['options'], message: t('schema.validation.options-max') })
      }
      const keys = values.options.map((option) => option.key)
      if (new Set(keys).size !== keys.length) {
        ctx.addIssue({ code: 'custom', path: ['options'], message: t('schema.validation.options-duplicate') })
      }
    }

    const hasDefault = values.defaultToday || values.defaultValue.trim() !== ''
    if (values.isReadOnly && isStatic && !hasDefault) {
      ctx.addIssue({ code: 'custom', path: ['defaultValue'], message: t('schema.validation.readonly-default') })
    }

    if (values.minLength) {
      const n = Number(values.minLength)
      const limit = values.maxLength
        ? Number(values.maxLength)
        : dataType === FIELD_DATA_TYPE.Text
          ? FIELD_LIMITS.maxTextLength
          : FIELD_LIMITS.maxLongTextLength
      if (!Number.isInteger(n) || n < 1 || n > limit) {
        ctx.addIssue({ code: 'custom', path: ['minLength'], message: t('schema.validation.min-length-range', { limit }) })
      }
    }

    if (!values.minDateToday && !values.maxDateToday && values.minDate && values.maxDate && values.minDate > values.maxDate) {
      ctx.addIssue({ code: 'custom', path: ['minDate'], message: t('schema.validation.date-order') })
    }

    if (values.visibleWhenFieldKey && !values.visibleWhenValue) {
      ctx.addIssue({ code: 'custom', path: ['visibleWhenValue'], message: t('schema.validation.condition-value-required') })
    }
  })
}

/** Identity of a brand-new field: the key and data type can never change afterwards. */
export const newFieldSchema = z.object({
  key: z
    .string()
    .trim()
    .max(FIELD_LIMITS.maxKeyLength, t('schema.validation.key-max'))
    .regex(KEY_PATTERN, t('schema.validation.key-format')),
  dataType: z.string().min(1, t('schema.validation.field-type-required')),
})

export type NewFieldFormValues = z.infer<typeof newFieldSchema>