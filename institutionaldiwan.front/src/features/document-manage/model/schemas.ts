import { z } from 'zod'
import { t } from '@/shared/lib'

/** Register number and date are required; the movement is optional (blank destination = none). */
export const registerDraftSchema = z
  .object({
    registryId: z.string(),
    registryNumberText: z
      .string()
      .trim()
      .min(1, t('docdetails.validation.registry-number-required'))
      .max(100, t('documents.create.validation.registry-number-max')),
    registryDate: z.string().min(1, t('docdetails.validation.registry-date-required')),
    destinationDepartmentId: z.string(),
    movementTypeId: z.string(),
    movementInstructions: z
      .string()
      .trim()
      .max(1000, t('documents.create.validation.instructions-max')),
  })
  .superRefine((values, ctx) => {
    if (values.destinationDepartmentId && !values.movementTypeId) {
      ctx.addIssue({ code: 'custom', path: ['movementTypeId'], message: t('documents.create.validation.movement-type-required') })
    }
  })

export type RegisterDraftFormValues = z.infer<typeof registerDraftSchema>

export const PARTICIPANT_TARGET_TYPES = ['external', 'department', 'person'] as const
export type ParticipantTargetKey = (typeof PARTICIPANT_TARGET_TYPES)[number]

/** The form picks one target kind; only its matching field is sent to the server. */
export const addParticipantSchema = z
  .object({
    roleId: z.string().min(1, t('docdetails.validation.role-required')),
    targetType: z.enum(PARTICIPANT_TARGET_TYPES),
    externalOrganizationId: z.string(),
    internalDepartmentId: z.string(),
    personName: z
      .string()
      .trim()
      .max(200, t('docdetails.validation.person-name-max')),
    isPrimary: z.boolean(),
    notes: z.string().trim().max(1000, t('docdetails.validation.notes-max')),
  })
  .superRefine((values, ctx) => {
    if (values.targetType === 'external' && !values.externalOrganizationId) {
      ctx.addIssue({
        code: 'custom',
        path: ['externalOrganizationId'],
        message: t('docdetails.validation.org-required'),
      })
    }
    if (values.targetType === 'department' && !values.internalDepartmentId) {
      ctx.addIssue({
        code: 'custom',
        path: ['internalDepartmentId'],
        message: t('docdetails.validation.dept-required'),
      })
    }
    if (values.targetType === 'person' && !values.personName.trim()) {
      ctx.addIssue({
        code: 'custom',
        path: ['personName'],
        message: t('docdetails.validation.person-name-required'),
      })
    }
  })

export type AddParticipantFormValues = z.infer<typeof addParticipantSchema>

export const cancelDocumentSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, t('docdetails.validation.cancel-reason-required'))
    .max(1000, t('docdetails.validation.cancel-reason-max')),
})

export type CancelDocumentFormValues = z.infer<typeof cancelDocumentSchema>