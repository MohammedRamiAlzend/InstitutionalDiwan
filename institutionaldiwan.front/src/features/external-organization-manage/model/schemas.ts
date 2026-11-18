import { z } from 'zod'
import { t } from '@/shared/lib'

/** Mirrors `ExternalOrganizationContact` on the server: e-mail or phone, or blank. */
const PHONE_PATTERN = /^\+?[0-9][0-9\s\-()]{5,19}$/

function isContactInfo(value: string): boolean {
  if (!value) return true
  if (value.includes('@')) return z.email().safeParse(value).success
  return PHONE_PATTERN.test(value)
}

/** Matches the external organization EF limits and the server's FluentValidation rules. */
export const externalOrganizationFormSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, t('external-org.validation.code-required'))
    .max(50, t('external-org.validation.code-max')),
  nameAr: z
    .string()
    .trim()
    .min(1, t('external-org.validation.name-ar-required'))
    .max(200, t('external-org.validation.name-ar-max')),
  organizationTypeId: z.string().min(1, t('external-org.validation.type-required')),
  nameEn: z
    .string()
    .trim()
    .max(200, t('external-org.validation.name-en-max')),
  contactInfo: z
    .string()
    .trim()
    .max(254, t('external-org.validation.contact-max'))
    .refine(isContactInfo, t('external-org.validation.contact-format')),
})

export type ExternalOrganizationFormValues = z.infer<
  typeof externalOrganizationFormSchema
>
