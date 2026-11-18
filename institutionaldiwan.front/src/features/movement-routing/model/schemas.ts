import { z } from 'zod'
import { t } from '@/shared/lib'

const OPTIONAL_DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/

/** A date input is either blank or a local `yyyy-MM-dd` value. */
const optionalDate = z.string().refine(
  (value) => !value || OPTIONAL_DATE_PATTERN.test(value),
  t('movements.validation.date-invalid'),
)

const optionalInstructions = z
  .string()
  .trim()
  .max(1000, t('movements.validation.instructions-max'))

/** Opening a movement: destination is mandatory; type, assignee, and dates are optional. */
export const openMovementSchema = z.object({
  destinationDepartmentId: z.string().min(1, t('movements.validation.destination-required')),
  movementTypeId: z.string().min(1, t('movements.validation.movement-type-required')),
  assignedToUserId: z.string(),
  instructions: optionalInstructions,
  dueDate: optionalDate,
})

export type OpenMovementFormValues = z.infer<typeof openMovementSchema>

export const movementNotesSchema = z.object({
  notes: z.string().trim().max(1000, t('movements.validation.notes-max')),
})

export type MovementNotesFormValues = z.infer<typeof movementNotesSchema>

export const receiveMovementSchema = z.object({
  registryId: z.string(),
  registryDate: z
    .string()
    .regex(OPTIONAL_DATE_PATTERN, t('movements.validation.registry-date-required')),
  registryNumberText: z.string().trim().max(100, t('movements.validation.registry-number-max')),
  notes: z.string().trim().max(1000, t('movements.validation.notes-max')),
})

export type ReceiveMovementFormValues = z.infer<typeof receiveMovementSchema>

export const movementReasonSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, t('movements.validation.reason-required'))
    .max(1000, t('movements.validation.reason-max')),
})

export type MovementReasonFormValues = z.infer<typeof movementReasonSchema>

/** Redirecting: only the new destination and optional instructions are asked for. */
export const redirectMovementSchema = z.object({
  newDestinationDepartmentId: z.string().min(1, t('movements.validation.new-destination-required')),
  instructions: optionalInstructions,
})

export type RedirectMovementFormValues = z.infer<typeof redirectMovementSchema>
