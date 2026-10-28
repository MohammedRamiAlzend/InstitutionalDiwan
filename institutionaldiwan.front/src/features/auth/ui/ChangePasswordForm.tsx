import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { KeyRound } from 'lucide-react'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, PasswordField } from '@/shared/ui'
import {
  changePasswordSchema,
  type ChangePasswordFormValues,
} from '../model/change-password.schema'
import { useChangePasswordMutation } from '../api/use-change-password'

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
