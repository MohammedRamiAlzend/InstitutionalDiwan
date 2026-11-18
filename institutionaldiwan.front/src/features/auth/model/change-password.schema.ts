import { z } from 'zod'
import { t } from '@/shared/lib'

/** `POST /api/auth/change-password` request body. */
export interface ChangePasswordRequest {
  oldPassword: string
  newPassword: string
}

/** Mirrors `ChangePasswordCommandValidator`; the confirmation field is client-only. */
export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, t('auth.change-password.validation.current-required')),
    newPassword: z
      .string()
      .min(1, t('auth.change-password.validation.new-required'))
      .min(8, t('auth.change-password.validation.new-min'))
      .max(200, t('auth.change-password.validation.new-max')),
    confirmPassword: z.string().min(1, t('auth.change-password.validation.confirm-required')),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
    message: t('auth.change-password.validation.mismatch'),
  })
  .refine((values) => values.newPassword !== values.currentPassword, {
    path: ['newPassword'],
    message: t('auth.change-password.validation.same'),
  })

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>
