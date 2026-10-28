import { z } from 'zod'

/** Matches the EF limits and the server's FluentValidation rules. */
export const institutionSettingsSchema = z.object({
  institutionNameAr: z
    .string()
    .trim()
    .min(1, 'اسم المؤسسة بالعربية مطلوب.')
    .max(200, 'اسم المؤسسة بالعربية يتجاوز الحد الأقصى للأحرف.'),
  institutionNameEn: z
    .string()
    .trim()
    .max(200, 'اسم المؤسسة بالإنجليزية يتجاوز الحد الأقصى للأحرف.'),
  logoPath: z
    .string()
    .trim()
    .max(500, 'مسار الشعار يتجاوز الحد الأقصى للأحرف.'),
  defaultTimezone: z
    .string()
    .trim()
    .min(1, 'المنطقة الزمنية الافتراضية مطلوبة.')
    .max(100, 'المنطقة الزمنية تتجاوز الحد الأقصى للأحرف.'),
  numberingModeId: z.string().min(1, 'اختر نمط الترقيم.'),
  numberingResetPolicyId: z.string().min(1, 'اختر سياسة تصفير الترقيم.'),
  centralDiwanDepartmentId: z.string().min(1, 'اختر قسم الديوان المركزي.'),
  responsibleEmployeeId: z.string(),
  centralDiwanTransferReason: z
    .string()
    .trim()
    .max(1000, 'سبب النقل يتجاوز الحد الأقصى للأحرف.'),
})

export type InstitutionSettingsFormValues = z.infer<typeof institutionSettingsSchema>
