import { z } from 'zod'

/** `POST /api/auth/change-password` request body. */
export interface ChangePasswordRequest {
  oldPassword: string
  newPassword: string
}

/** Mirrors `ChangePasswordCommandValidator`; the confirmation field is client-only. */
export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'كلمة المرور الحالية مطلوبة.'),
    newPassword: z
      .string()
      .min(1, 'كلمة المرور الجديدة مطلوبة.')
      .min(8, 'كلمة المرور الجديدة قصيرة جدًا، الحد الأدنى 8 أحرف.')
      .max(200, 'كلمة المرور الجديدة تتجاوز الحد الأقصى للأحرف.'),
    confirmPassword: z.string().min(1, 'تأكيد كلمة المرور مطلوب.'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'كلمتا المرور غير متطابقتين.',
  })
  .refine((values) => values.newPassword !== values.currentPassword, {
    path: ['newPassword'],
    message: 'يجب أن تختلف كلمة المرور الجديدة عن الحالية.',
  })

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>
