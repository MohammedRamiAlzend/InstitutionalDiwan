import { z } from 'zod'
import { t } from '@/shared/lib'

/** Matches the department EF limits and the server's FluentValidation rules. */
export const departmentFormSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, t('department.validation.code-required'))
    .max(50, t('department.validation.code-max')),
  nameAr: z
    .string()
    .trim()
    .min(1, t('department.validation.name-ar-required'))
    .max(200, t('department.validation.name-ar-max')),
  nameEn: z
    .string()
    .trim()
    .max(200, t('department.validation.name-en-max')),
  departmentTypeId: z.string().min(1, t('department.validation.type-required')),
  parentDepartmentId: z.string(),
  managerEmployeeId: z.string(),
  canSendExternal: z.boolean(),
  canReceiveExternal: z.boolean(),
  notes: z
    .string()
    .trim()
    .max(500, t('department.validation.notes-max')),
})

export type DepartmentFormValues = z.infer<typeof departmentFormSchema>
