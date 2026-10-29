import { z } from 'zod'

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
    .min(1, 'رمز الجهة الخارجية مطلوب.')
    .max(50, 'رمز الجهة الخارجية يتجاوز الحد الأقصى للأحرف.'),
  nameAr: z
    .string()
    .trim()
    .min(1, 'اسم الجهة بالعربية مطلوب.')
    .max(200, 'اسم الجهة بالعربية يتجاوز الحد الأقصى للأحرف.'),
  organizationTypeId: z.string().min(1, 'اختر نوع الجهة.'),
  nameEn: z
    .string()
    .trim()
    .max(200, 'اسم الجهة بالإنجليزية يتجاوز الحد الأقصى للأحرف.'),
  contactInfo: z
    .string()
    .trim()
    .max(254, 'بيانات الاتصال تتجاوز الحد الأقصى للأحرف.')
    .refine(isContactInfo, 'بيانات الاتصال يجب أن تكون بريدًا إلكترونيًا أو رقم هاتف.'),
})

export type ExternalOrganizationFormValues = z.infer<
  typeof externalOrganizationFormSchema
>
