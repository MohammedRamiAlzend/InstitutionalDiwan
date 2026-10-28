import { z } from 'zod'

const userFields = {
  username: z
    .string()
    .trim()
    .min(3, 'اسم المستخدم يجب أن يكون 3 أحرف على الأقل.')
    .max(100, 'اسم المستخدم يتجاوز الحد الأقصى للأحرف.'),
  primaryDepartmentId: z.string().min(1, 'اختر القسم الأساسي.'),
  employeeId: z.string(),
  scopeTypeId: z.string().min(1, 'اختر نطاق المستخدم.'),
  mustChangePassword: z.boolean(),
}

/** `POST /api/users` — the password is required on creation only. */
export const createUserFormSchema = z.object({
  ...userFields,
  password: z
    .string()
    .min(8, 'كلمة المرور يجب أن تكون 8 أحرف على الأقل.')
    .max(200, 'كلمة المرور تتجاوز الحد الأقصى للأحرف.'),
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
      .min(1, 'كلمة المرور المؤقتة مطلوبة.')
      .min(8, 'كلمة المرور المؤقتة قصيرة جدًا، الحد الأدنى 8 أحرف.')
      .max(200, 'كلمة المرور المؤقتة تتجاوز الحد الأقصى للأحرف.'),
    confirmPassword: z.string().min(1, 'تأكيد كلمة المرور مطلوب.'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'كلمتا المرور غير متطابقتين.',
  })

export type ResetUserPasswordFormValues = z.infer<typeof resetUserPasswordSchema>

export const departmentAssignmentSchema = z
  .object({
    departmentId: z.string().min(1, 'اختر القسم.'),
    assignmentTypeId: z.string().min(1, 'اختر نوع الإسناد.'),
    includeSubtree: z.boolean(),
    startDate: z.string().min(1, 'تاريخ البدء مطلوب.'),
    endDate: z.string(),
  })
  .superRefine((values, ctx) => {
    if (values.endDate && values.endDate < values.startDate) {
      ctx.addIssue({
        code: 'custom',
        path: ['endDate'],
        message: 'تاريخ الانتهاء لا يمكن أن يسبق تاريخ البدء.',
      })
    }
  })

export type DepartmentAssignmentFormValues = z.infer<typeof departmentAssignmentSchema>
