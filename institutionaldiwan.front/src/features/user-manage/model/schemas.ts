import { z } from 'zod'
import { t } from '@/shared/lib'

const userFields = {
  username: z
    .string()
    .trim()
    .min(3, t('user.validation.username-min'))
    .max(100, t('user.validation.username-max')),
  primaryDepartmentId: z.string().min(1, t('user.validation.department-required')),
  employeeId: z.string(),
  scopeTypeId: z.string().min(1, t('user.validation.scope-required')),
  mustChangePassword: z.boolean(),
}

/** `POST /api/users` — the password is required on creation only. */
export const createUserFormSchema = z.object({
  ...userFields,
  password: z
    .string()
    .min(8, t('user.validation.password-min'))
    .max(200, t('user.validation.password-max')),
})

/** `PUT /api/users/{id}` — the username and password are immutable here. */
export const updateUserFormSchema = z.object({
  ...userFields,
  password: z.string(),
})

export type UserFormValues = z.infer<typeof createUserFormSchema>

/** `POST /api/users/{id}/reset-password` — the owner must change it at the next sign-in. */
export const resetUserPasswordSchema = z
  .object({
    newPassword: z
      .string()
      .min(1, t('user.validation.temp-required'))
      .min(8, t('user.validation.temp-min'))
      .max(200, t('user.validation.temp-max')),
    confirmPassword: z.string().min(1, t('user.validation.confirm-required')),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
    message: t('user.validation.password-mismatch'),
  })

export type ResetUserPasswordFormValues = z.infer<typeof resetUserPasswordSchema>

export const departmentAssignmentSchema = z
  .object({
    departmentId: z.string().min(1, t('user.validation.assign-department-required')),
    assignmentTypeId: z.string().min(1, t('user.validation.assign-type-required')),
    includeSubtree: z.boolean(),
    startDate: z.string().min(1, t('user.validation.start-required')),
    endDate: z.string(),
  })
  .superRefine((values, ctx) => {
    if (values.endDate && values.endDate < values.startDate) {
      ctx.addIssue({
        code: 'custom',
        path: ['endDate'],
        message: t('user.validation.end-before-start'),
      })
    }
  })

export type DepartmentAssignmentFormValues = z.infer<typeof departmentAssignmentSchema>