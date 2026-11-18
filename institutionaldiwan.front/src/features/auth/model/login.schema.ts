import { z } from 'zod'
import type { PermissionCode } from '@/shared/config'
import { t } from '@/shared/lib'

/** `POST /api/auth/login` request body. */
export interface LoginRequest {
  username: string
  password: string
}

/** `POST /api/auth/login` 200 payload (camelCase JSON). */
export interface LoginResponse {
  accessToken: string
  expiresAtUtc: string
  userId: string
  username: string
  mustChangePassword: boolean
  roles: string[]
  permissions: PermissionCode[]
}

export const loginSchema = z.object({
  username: z.string().trim().min(1, t('auth.login.validation.username-required')),
  password: z.string().min(1, t('auth.login.validation.password-required')),
})

export type LoginFormValues = z.infer<typeof loginSchema>
