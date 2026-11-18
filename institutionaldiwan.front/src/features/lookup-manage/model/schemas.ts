import { z } from 'zod'
import { t } from '@/shared/lib'

/** Mirrors `CreateLookupEntryCommandValidator`: positive id, both labels required, ≤ 150 chars. */
export const lookupEntryFormSchema = z.object({
  id: z
    .string()
    .trim()
    .min(1, t('lookup.validation.id-required'))
    .regex(/^[0-9]+$/, t('lookup.validation.id-positive'))
    .refine(
      (value) => Number(value) > 0 && Number(value) <= 2147483647,
      t('lookup.validation.id-positive'),
    ),
  descAr: z
    .string()
    .trim()
    .min(1, t('lookup.validation.desc-ar-required'))
    .max(150, t('lookup.validation.desc-ar-max')),
  desc: z
    .string()
    .trim()
    .min(1, t('lookup.validation.desc-required'))
    .max(150, t('lookup.validation.desc-max')),
})

export type LookupEntryFormValues = z.infer<typeof lookupEntryFormSchema>
