import { useState } from 'react'
import { useForm, type UseFormRegisterReturn } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Eye, EyeOff, KeyRound } from 'lucide-react'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, FormField, Input } from '@/shared/ui'
import {
  changePasswordSchema,
  type ChangePasswordFormValues,
} from '../model/change-password.schema'
import { useChangePasswordMutation } from '../api/use-change-password'

interface PasswordFieldProps {
  id: string
  label: string
  autoComplete: string
  error?: string
  registration: UseFormRegisterReturn
}

function PasswordField({
  id,
  label,
  autoComplete,
  error,
  registration,
}: PasswordFieldProps) {
  const [visible, setVisible] = useState(false)

  return (
    <FormField label={label} required htmlFor={id} error={error}>
      <Input
        id={id}
        type={visible ? 'text' : 'password'}
        autoComplete={autoComplete}
        invalid={Boolean(error)}
        endIcon={
          <button
            type="button"
            onClick={() => setVisible((value) => !value)}
            className="cursor-pointer p-1 hover:text-foreground"
            aria-label={visible ? 'إخفاء كلمة المرور' : 'إظهار كلمة المرور'}
          >
            {visible ? (
              <EyeOff className="size-4" aria-hidden />
            ) : (
              <Eye className="size-4" aria-hidden />
            )}
          </button>
        }
        {...registration}
      />
    </FormField>
  )
}

interface ChangePasswordFormProps {
  onSuccess?: () => void
}

export function ChangePasswordForm({ onSuccess }: ChangePasswordFormProps) {
  const changePasswordMutation = useChangePasswordMutation()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    changePasswordMutation.reset()
    await changePasswordMutation.mutateAsync({
      oldPassword: values.currentPassword,
      newPassword: values.newPassword,
    })
    toast.success('تم تغيير كلمة المرور بنجاح.')
    onSuccess?.()
  })

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-5" noValidate>
      <PasswordField
        id="current-password"
        label="كلمة المرور الحالية"
        autoComplete="current-password"
        error={errors.currentPassword?.message}
        registration={register('currentPassword')}
      />

      <PasswordField
        id="new-password"
        label="كلمة المرور الجديدة"
        autoComplete="new-password"
        error={errors.newPassword?.message}
        registration={register('newPassword')}
      />

      <PasswordField
        id="confirm-password"
        label="تأكيد كلمة المرور الجديدة"
        autoComplete="new-password"
        error={errors.confirmPassword?.message}
        registration={register('confirmPassword')}
      />

      {changePasswordMutation.isError && (
        <p
          role="alert"
          className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
        >
          {toArabicErrorMessage(changePasswordMutation.error)}
        </p>
      )}

      <Button type="submit" size="lg" loading={changePasswordMutation.isPending}>
        حفظ كلمة المرور الجديدة
        <KeyRound className="size-4" aria-hidden />
      </Button>
    </form>
  )
}
