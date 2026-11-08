import { z } from 'zod'
import { SYSTEM_ERROR_NOTES_MAX_LENGTH } from '@/entities/system-error'

export const updateSystemErrorSchema = z.object({
  status: z.string().min(1),
  notes: z.string().trim().max(SYSTEM_ERROR_NOTES_MAX_LENGTH, 'الملاحظات لا تتجاوز 4000 حرف.'),
})

export type UpdateSystemErrorFormValues = z.infer<typeof updateSystemErrorSchema>

export const purgeSystemErrorsSchema = z.object({
  days: z
    .string()
    .trim()
    .regex(/^\d+$/, 'أدخل عدد أيام صحيحًا.')
    .refine((value) => Number(value) >= 1 && Number(value) <= 3650, 'عدد الأيام بين 1 و3650.'),
})

export type PurgeSystemErrorsFormValues = z.infer<typeof purgeSystemErrorsSchema>
