import { z } from 'zod'

const OPTIONAL_DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/

/** A date input is either blank or a local `yyyy-MM-dd` value. */
const optionalDate = z.string().refine(
  (value) => !value || OPTIONAL_DATE_PATTERN.test(value),
  'التاريخ غير صالح.',
)

const optionalInstructions = z
  .string()
  .trim()
  .max(1000, 'التعليمات تتجاوز الحد الأقصى للأحرف.')

/** Opening a movement: destination is mandatory; type, assignee, and dates are optional. */
export const openMovementSchema = z.object({
  destinationDepartmentId: z.string().min(1, 'اختر القسم الوجهة.'),
  movementTypeId: z.string().min(1, 'اختر نوع الحركة.'),
  assignedToUserId: z.string(),
  instructions: optionalInstructions,
  dueDate: optionalDate,
})

export type OpenMovementFormValues = z.infer<typeof openMovementSchema>

export const movementNotesSchema = z.object({
  notes: z.string().trim().max(1000, 'الملاحظات تتجاوز الحد الأقصى للأحرف.'),
})

export type MovementNotesFormValues = z.infer<typeof movementNotesSchema>

export const movementReasonSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, 'السبب مطلوب.')
    .max(1000, 'السبب يتجاوز الحد الأقصى للأحرف.'),
})

export type MovementReasonFormValues = z.infer<typeof movementReasonSchema>

export const redirectMovementSchema = z.object({
  newDestinationDepartmentId: z.string().min(1, 'اختر القسم الوجهة الجديد.'),
  assignedToUserId: z.string(),
  instructions: optionalInstructions,
  dueDate: optionalDate,
})

export type RedirectMovementFormValues = z.infer<typeof redirectMovementSchema>
