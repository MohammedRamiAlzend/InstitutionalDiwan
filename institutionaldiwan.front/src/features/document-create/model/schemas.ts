import { z } from 'zod'

const documentFields = {
  documentTypeId: z.string().min(1, 'اختر نوع الوثيقة.'),
  registryId: z.string(),
  registryNumberText: z
    .string()
    .trim()
    .max(100, 'رقم السجل يتجاوز الحد الأقصى للأحرف.'),
  registryDate: z.string(),
  destinationDepartmentId: z.string(),
  movementTypeId: z.string(),
  movementInstructions: z
    .string()
    .trim()
    .max(1000, 'التعليمات تتجاوز الحد الأقصى للأحرف.'),
  documentDate: z.string(),
  externalReferenceNumber: z
    .string()
    .trim()
    .max(100, 'الرقم المرجعي الخارجي يتجاوز الحد الأقصى للأحرف.'),
  externalDocumentDate: z.string(),
  mode: z.enum(['draft', 'register']),
  persons: z.array(
    z.object({
      personId: z.string().nullable(),
      fullName: z.string().trim().min(1).max(300, 'الاسم يتجاوز الحد الأقصى للأحرف.'),
    }),
  ),
  addRecipient: z.boolean(),
  recipientType: z.enum(['external', 'department', 'person']),
  recipientOrganizationId: z.string(),
  recipientDepartmentId: z.string(),
  recipientPersonName: z
    .string()
    .trim()
    .max(200, 'اسم المستلم يتجاوز الحد الأقصى للأحرف.'),
}

/** Shared form for both `POST /api/documents` and `POST /api/documents/drafts`. */
export const createDocumentSchema = z.object(documentFields).superRefine((values, ctx) => {
  if (values.addRecipient) {
    if (values.recipientType === 'external' && !values.recipientOrganizationId) {
      ctx.addIssue({
        code: 'custom',
        path: ['recipientOrganizationId'],
        message: 'اختر الجهة الخارجية المستلمة.',
      })
    }
    if (values.recipientType === 'department' && !values.recipientDepartmentId) {
      ctx.addIssue({
        code: 'custom',
        path: ['recipientDepartmentId'],
        message: 'اختر القسم المستلم.',
      })
    }
    if (values.recipientType === 'person' && !values.recipientPersonName.trim()) {
      ctx.addIssue({
        code: 'custom',
        path: ['recipientPersonName'],
        message: 'اسم المستلم مطلوب.',
      })
    }
  }

  if (values.mode !== 'register') return

  if (!values.registryNumberText) {
    ctx.addIssue({
      code: 'custom',
      path: ['registryNumberText'],
      message: 'رقم السجل مطلوب عند التسجيل الفوري.',
    })
  }

  if (!values.registryDate) {
    ctx.addIssue({
      code: 'custom',
      path: ['registryDate'],
      message: 'تاريخ السجل مطلوب عند التسجيل الفوري.',
    })
  }

  if (values.destinationDepartmentId && !values.movementTypeId) {
    ctx.addIssue({
      code: 'custom',
      path: ['movementTypeId'],
      message: 'اختر نوع الحركة.',
    })
  }
})

export type CreateDocumentFormValues = z.infer<typeof createDocumentSchema>
export type CreateDocumentMode = CreateDocumentFormValues['mode']
