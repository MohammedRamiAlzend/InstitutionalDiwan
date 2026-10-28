import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { LogIn, User } from 'lucide-react'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, FormField, Input, PasswordField } from '@/shared/ui'
import {
  loginSchema,
  type LoginFormValues,
  type LoginResponse,
} from '../model/login.schema'
import { useLoginMutation } from '../api/use-login'

interface LoginFormProps {
  onSuccess?: (session: LoginResponse) => void
}

export function LoginForm({ onSuccess }: LoginFormProps) {
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
    const session = await loginMutation.mutateAsync(values)
    onSuccess?.(session)
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

      <PasswordField
        id="password"
        label="كلمة المرور"
        autoComplete="current-password"
        placeholder="كلمة المرور"
        error={errors.password?.message}
        registration={register('password')}
      />

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
