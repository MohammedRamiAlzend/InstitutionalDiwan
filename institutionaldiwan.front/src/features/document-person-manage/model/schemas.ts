import { z } from 'zod'

const personEntry = z.object({
  personId: z.string().nullable(),
  fullName: z.string().trim().min(1).max(300, 'الاسم يتجاوز الحد الأقصى للأحرف.'),
})

export const addDocumentPersonsSchema = z.object({
  persons: z.array(personEntry).min(1, 'أضف اسمًا واحدًا على الأقل.'),
  roleId: z.string().min(1, 'اختر الصفة.'),
})

export type AddDocumentPersonsFormValues = z.infer<typeof addDocumentPersonsSchema>

export const changeDocumentPersonRoleSchema = z.object({
  roleId: z.string().min(1, 'اختر الصفة.'),
})

export type ChangeDocumentPersonRoleFormValues = z.infer<typeof changeDocumentPersonRoleSchema>
