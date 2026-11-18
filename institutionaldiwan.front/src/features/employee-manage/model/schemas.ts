import { z } from 'zod'
import { t } from '@/shared/lib'

/** Matches the employee EF limits and the server's FluentValidation rules. */
export const employeeFormSchema = z.object({
  fullNameAr: z
    .string()
    .trim()
    .min(1, t('employee.validation.name-ar-required'))
    .max(200, t('employee.validation.name-ar-max')),
  departmentId: z.string().min(1, t('employee.validation.department-required')),
  employeeNumber: z
    .string()
    .trim()
    .max(50, t('employee.validation.number-max')),
  fullNameEn: z
    .string()
    .trim()
    .max(200, t('employee.validation.name-en-max')),
  jobTitleAr: z
    .string()
    .trim()
    .max(150, t('employee.validation.job-title-max')),
  email: z
    .string()
    .trim()
    .max(150, t('employee.validation.email-max'))
    .refine(
      (value) => value === '' || z.email().safeParse(value).success,
      t('employee.validation.email-format'),
    ),
  phone: z
    .string()
    .trim()
    .max(50, t('employee.validation.phone-max')),
})

export type EmployeeFormValues = z.infer<typeof employeeFormSchema>