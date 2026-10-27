import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Eye, EyeOff, LogIn, User } from 'lucide-react'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, FormField, Input } from '@/shared/ui'
import { loginSchema, type LoginFormValues } from '../model/login.schema'
import { useLoginMutation } from '../api/use-login'

interface LoginFormProps {
  onSuccess?: () => void
}

export function LoginForm({ onSuccess }: LoginFormProps) {
  const [showPassword, setShowPassword] = useState(false)
  const loginMutation = useLoginMutation()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { username: '', password: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    await loginMutation.mutateAsync(values)
    onSuccess?.()
  })

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-5" noValidate>
      <FormField
        label="اسم المستخدم"
        required
        error={errors.username?.message}
      >
        <Input
          id="username"
          autoComplete="username"
          placeholder="اسم المستخدم"
          startIcon={<User className="size-4" aria-hidden />}
          invalid={Boolean(errors.username)}
          {...register('username')}
        />
      </FormField>

      <FormField
        label="كلمة المرور"
        required
        error={errors.password?.message}
      >
        <Input
          id="password"
          type={showPassword ? 'text' : 'password'}
          autoComplete="current-password"
          placeholder="كلمة المرور"
          invalid={Boolean(errors.password)}
          endIcon={
            <button
              type="button"
              onClick={() => setShowPassword((value) => !value)}
              className="cursor-pointer p-1 hover:text-foreground"
              aria-label={
                showPassword ? 'إخفاء كلمة المرور' : 'إظهار كلمة المرور'
              }
            >
              {showPassword ? (
                <EyeOff className="size-4" aria-hidden />
              ) : (
                <Eye className="size-4" aria-hidden />
              )}
            </button>
          }
          {...register('password')}
        />
      </FormField>

      {loginMutation.isError && (
        <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          {toArabicErrorMessage(loginMutation.error)}
        </p>
      )}

      <Button type="submit" size="lg" loading={loginMutation.isPending}>
        تسجيل الدخول
        <LogIn className="size-4 rtl:-scale-x-100" aria-hidden />
      </Button>
    </form>
  )
}
