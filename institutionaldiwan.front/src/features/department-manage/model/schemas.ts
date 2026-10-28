import { z } from 'zod'

/** Matches the department EF limits and the server's FluentValidation rules. */
export const departmentFormSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, 'رمز القسم مطلوب.')
    .max(50, 'رمز القسم يتجاوز الحد الأقصى للأحرف.'),
  nameAr: z
    .string()
    .trim()
    .min(1, 'اسم القسم بالعربية مطلوب.')
    .max(200, 'اسم القسم بالعربية يتجاوز الحد الأقصى للأحرف.'),
  nameEn: z
    .string()
    .trim()
    .max(200, 'اسم القسم بالإنجليزية يتجاوز الحد الأقصى للأحرف.'),
  departmentTypeId: z.string().min(1, 'اختر نوع القسم.'),
  parentDepartmentId: z.string(),
  managerEmployeeId: z.string(),
  canSendExternal: z.boolean(),
  canReceiveExternal: z.boolean(),
  notes: z
    .string()
    .trim()
    .max(500, 'الملاحظات تتجاوز الحد الأقصى للأحرف.'),
})

export type DepartmentFormValues = z.infer<typeof departmentFormSchema>
