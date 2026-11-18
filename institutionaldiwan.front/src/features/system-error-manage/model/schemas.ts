import { z } from 'zod'
import { SYSTEM_ERROR_NOTES_MAX_LENGTH } from '@/entities/system-error'
import { t } from '@/shared/lib'

export const updateSystemErrorSchema = z.object({
  status: z.string().min(1),
  notes: z.string().trim().max(SYSTEM_ERROR_NOTES_MAX_LENGTH, t('system-error.validation.notes-max')),
})

export type UpdateSystemErrorFormValues = z.infer<typeof updateSystemErrorSchema>

export const purgeSystemErrorsSchema = z.object({
  days: z
    .string()
    .trim()
    .regex(/^\d+$/, t('system-error.validation.days-format'))
    .refine((value) => Number(value) >= 1 && Number(value) <= 3650, t('system-error.validation.days-range')),
})

export type PurgeSystemErrorsFormValues = z.infer<typeof purgeSystemErrorsSchema>
