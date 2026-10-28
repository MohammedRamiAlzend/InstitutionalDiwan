import { z } from 'zod'

/** Matches the employee EF limits and the server's FluentValidation rules. */
export const employeeFormSchema = z.object({
  fullNameAr: z
    .string()
    .trim()
    .min(1, 'اسم الموظف الكامل بالعربية مطلوب.')
    .max(200, 'اسم الموظف بالعربية يتجاوز الحد الأقصى للأحرف.'),
  departmentId: z.string().min(1, 'اختر قسم الموظف.'),
  employeeNumber: z
    .string()
    .trim()
    .max(50, 'الرقم الوظيفي يتجاوز الحد الأقصى للأحرف.'),
  fullNameEn: z
    .string()
    .trim()
    .max(200, 'اسم الموظف بالإنجليزية يتجاوز الحد الأقصى للأحرف.'),
  jobTitleAr: z
    .string()
    .trim()
    .max(150, 'المسمى الوظيفي يتجاوز الحد الأقصى للأحرف.'),
  email: z
    .string()
    .trim()
    .max(150, 'البريد الإلكتروني يتجاوز الحد الأقصى للأحرف.')
    .refine(
      (value) => value === '' || z.email().safeParse(value).success,
      'صيغة البريد الإلكتروني غير صحيحة.',
    ),
  phone: z
    .string()
    .trim()
    .max(50, 'رقم الهاتف يتجاوز الحد الأقصى للأحرف.'),
})

export type EmployeeFormValues = z.infer<typeof employeeFormSchema>
