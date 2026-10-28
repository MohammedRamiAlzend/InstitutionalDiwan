import { z } from 'zod'

/**
 * Shared by the create and edit dialogs: the code field stays in the form but is
 * disabled on edit (the API treats it as immutable), so one schema covers both.
 */
export const roleFormSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, 'رمز الدور مطلوب.')
    .max(100, 'رمز الدور يتجاوز الحد الأقصى للأحرف.'),
  nameAr: z
    .string()
    .trim()
    .min(1, 'الاسم العربي للدور مطلوب.')
    .max(150, 'الاسم العربي يتجاوز الحد الأقصى للأحرف.'),
  nameEn: z
    .string()
    .trim()
    .max(150, 'الاسم الإنجليزي يتجاوز الحد الأقصى للأحرف.'),
  description: z
    .string()
    .trim()
    .max(500, 'الوصف يتجاوز الحد الأقصى للأحرف.'),
})

export type RoleFormValues = z.infer<typeof roleFormSchema>
