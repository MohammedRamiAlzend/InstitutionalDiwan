import { z } from 'zod'
import { FIELD_DATA_TYPE, FIELD_LIMITS, type FieldDataType } from '@/entities/document-schema'

const KEY_PATTERN = /^[a-z][a-z0-9_]*$/

/** Mirrors `DocumentFieldSettings.Create`: which settings apply depends on the field's data type. */
const optionSchema = z.object({
  key: z.string().trim().regex(KEY_PATTERN, 'مفتاح الخيار: أحرف إنجليزية صغيرة وأرقام وشرطة سفلية.'),
  labelAr: z.string().trim().min(1, 'عنوان الخيار مطلوب.'),
})

const settingsShape = z.object({
  labelAr: z
    .string()
    .trim()
    .min(1, 'عنوان الحقل مطلوب.')
    .max(FIELD_LIMITS.maxLabelLength, 'عنوان الحقل لا يتجاوز 200 حرف.'),
  isRequired: z.boolean(),
  displayOrder: z.string().trim().regex(/^\d{1,4}$/, 'ترتيب العرض رقم صحيح موجب.'),
  regexPattern: z.string().max(FIELD_LIMITS.maxRegexLength, 'النمط لا يتجاوز 500 حرف.'),
  regexErrorMessageAr: z
    .string()
    .trim()
    .max(FIELD_LIMITS.maxRegexMessageLength, 'الرسالة لا تتجاوز 500 حرف.'),
  maxLength: z.string().trim(),
  minNumber: z.string().trim(),
  maxNumber: z.string().trim(),
  options: z.array(optionSchema),
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
      if (!isText) ctx.addIssue({ code: 'custom', path: ['regexPattern'], message: 'النمط للحقول النصية فقط.' })
      else {
        try {
          new RegExp(values.regexPattern)
        } catch {
          ctx.addIssue({ code: 'custom', path: ['regexPattern'], message: 'النمط ليس تعبيرًا نمطيًا صالحًا.' })
        }
        if (!values.regexErrorMessageAr) {
          ctx.addIssue({
            code: 'custom',
            path: ['regexErrorMessageAr'],
            message: 'رسالة الخطأ بالعربية مطلوبة عند تحديد نمط.',
          })
        }
      }
    }

    if (values.maxLength) {
      const limit =
        dataType === FIELD_DATA_TYPE.Text ? FIELD_LIMITS.maxTextLength : FIELD_LIMITS.maxLongTextLength
      const n = Number(values.maxLength)
      if (!isText) ctx.addIssue({ code: 'custom', path: ['maxLength'], message: 'الطول الأقصى للحقول النصية فقط.' })
      else if (!Number.isInteger(n) || n < 1 || n > limit) {
        ctx.addIssue({ code: 'custom', path: ['maxLength'], message: `الطول الأقصى بين 1 و${limit}.` })
      }
    }

    const isNumber = dataType === FIELD_DATA_TYPE.Number
    for (const name of ['minNumber', 'maxNumber'] as const) {
      if (values[name] && (!isNumber || !isNumeric(values[name]))) {
        ctx.addIssue({
          code: 'custom',
          path: [name],
          message: isNumber ? 'أدخل رقمًا صحيحًا.' : 'الحدود للحقول الرقمية فقط.',
        })
      }
    }
    if (isNumeric(values.minNumber) && isNumeric(values.maxNumber) && Number(values.minNumber) > Number(values.maxNumber)) {
      ctx.addIssue({ code: 'custom', path: ['minNumber'], message: 'الحد الأدنى لا يتجاوز الحد الأعلى.' })
    }

    if (dataType === FIELD_DATA_TYPE.Select) {
      if (values.options.length === 0) {
        ctx.addIssue({ code: 'custom', path: ['options'], message: 'أضف خيارًا واحدًا على الأقل.' })
      }
      if (values.options.length > FIELD_LIMITS.maxOptions) {
        ctx.addIssue({ code: 'custom', path: ['options'], message: 'لا يزيد عدد الخيارات عن 100.' })
      }
      const keys = values.options.map((option) => option.key)
      if (new Set(keys).size !== keys.length) {
        ctx.addIssue({ code: 'custom', path: ['options'], message: 'مفاتيح الخيارات يجب ألا تتكرر.' })
      }
    }
  })
}

/** Identity of a brand-new field: the key and data type can never change afterwards. */
export const newFieldSchema = z.object({
  key: z
    .string()
    .trim()
    .max(FIELD_LIMITS.maxKeyLength, 'المفتاح لا يتجاوز 50 حرفًا.')
    .regex(KEY_PATTERN, 'المفتاح يبدأ بحرف إنجليزي صغير ويحوي أحرفًا صغيرة وأرقامًا وشرطة سفلية فقط.'),
  dataType: z.string().min(1, 'اختر نوع الحقل.'),
})

export type NewFieldFormValues = z.infer<typeof newFieldSchema>
