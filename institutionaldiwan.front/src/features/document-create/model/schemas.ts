import { z } from 'zod'
import { t } from '@/shared/lib'

const documentFields = {
  documentTypeId: z.string().min(1, t('documents.create.validation.type-required')),
  registryId: z.string(),
  registryNumberText: z
    .string()
    .trim()
    .max(100, t('documents.create.validation.registry-number-max')),
  registryDate: z.string(),
  destinationDepartmentId: z.string(),
  movementTypeId: z.string(),
  movementInstructions: z
    .string()
    .trim()
    .max(1000, t('documents.create.validation.instructions-max')),
  mode: z.enum(['draft', 'register']),
  persons: z.array(
    z.object({
      personId: z.string().nullable(),
      fullName: z.string().trim().min(1).max(300, t('documents.create.validation.name-max')),
    }),
  ),
}

/** Shared form for both `POST /api/documents` and `POST /api/documents/drafts`. */
export const createDocumentSchema = z.object(documentFields).superRefine((values, ctx) => {
  if (values.mode !== 'register') return

  if (!values.registryNumberText) {
    ctx.addIssue({
      code: 'custom',
      path: ['registryNumberText'],
      message: t('documents.create.validation.registry-number-required'),
    })
  }

  if (!values.registryDate) {
    ctx.addIssue({
      code: 'custom',
      path: ['registryDate'],
      message: t('documents.create.validation.registry-date-required'),
    })
  }

  if (values.destinationDepartmentId && !values.movementTypeId) {
    ctx.addIssue({
      code: 'custom',
      path: ['movementTypeId'],
      message: t('documents.create.validation.movement-type-required'),
    })
  }
})

export type CreateDocumentFormValues = z.infer<typeof createDocumentSchema>
export type CreateDocumentMode = CreateDocumentFormValues['mode']
