import { z } from 'zod'
import { t } from '@/shared/lib'

/**
 * Shared by the create and edit dialogs: the code field stays in the form but is
 * disabled on edit (the API treats it as immutable), so one schema covers both.
 */
export const roleFormSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, t('role.validation.code-required'))
    .max(100, t('role.validation.code-max')),
  nameAr: z
    .string()
    .trim()
    .min(1, t('role.validation.name-ar-required'))
    .max(150, t('role.validation.name-ar-max')),
  nameEn: z
    .string()
    .trim()
    .max(150, t('role.validation.name-en-max')),
  description: z
    .string()
    .trim()
    .max(500, t('role.validation.description-max')),
})

export type RoleFormValues = z.infer<typeof roleFormSchema>