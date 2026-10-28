import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { KeyRound, Wand2 } from 'lucide-react'
import { toast } from 'sonner'
import type { UserResponse } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, PasswordField } from '@/shared/ui'
import {
  resetUserPasswordSchema,
  type ResetUserPasswordFormValues,
} from '../model/schemas'
import { useResetUserPasswordMutation } from '../api/userMutations'

const TEMPORARY_PASSWORD_ALPHABET =
  'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%*?'

function generateTemporaryPassword(length = 12): string {
  const values = new Uint32Array(length)
  crypto.getRandomValues(values)
  return Array.from(
    values,
    (value) => TEMPORARY_PASSWORD_ALPHABET[value % TEMPORARY_PASSWORD_ALPHABET.length],
  ).join('')
}

interface ResetPasswordDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user: UserResponse
}

/**
 * `POST /api/users/{id}/reset-password` — the administrator sets a temporary password for a user
 * who forgot theirs; the account is flagged to change it at the next sign-in.
 */
export function ResetPasswordDialog({ open, onOpenChange, user }: ResetPasswordDialogProps) {
  const resetMutation = useResetUserPasswordMutation()

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<ResetUserPasswordFormValues>({
    resolver: zodResolver(resetUserPasswordSchema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    resetMutation.reset()
    try {
      await resetMutation.mutateAsync({
        userId: user.userId,
        request: { newPassword: values.newPassword },
      })
      toast.success(
        'تمت إعادة تعيين كلمة المرور. سيُطلب من المستخدم تعيين كلمة مرور جديدة عند أول تسجيل دخول.',
      )
      reset({ newPassword: '', confirmPassword: '' })
      onOpenChange(false)
    } catch {
      // The Arabic error catalog renders the failure below.
    }
  })

  const fillGeneratedPassword = () => {
    const password = generateTemporaryPassword()
    setValue('newPassword', password, { shouldValidate: true })
    setValue('confirmPassword', password, { shouldValidate: true })
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="إعادة تعيين كلمة المرور"
      description={`عيّن كلمة مرور مؤقتة للمستخدم «${user.username}» ثم سلّمها له بطريقة آمنة.`}
      size="md"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={resetMutation.isPending}
          >
            إلغاء
          </Button>
          <Button
            type="submit"
            form="reset-password-form"
            loading={resetMutation.isPending}
          >
            إعادة تعيين كلمة المرور
          </Button>
        </>
      }
    >
      <form
        id="reset-password-form"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <p
          role="status"
          className="rounded-lg bg-warning/10 px-3 py-2 text-xs text-warning"
        >
          سيتوقف عمل كلمة المرور القديمة فورًا، وسيُطلب من المستخدم تعيين كلمة مرور جديدة عند
          أول تسجيل دخول.
        </p>

        <div className="flex justify-end">
          <Button type="button" variant="ghost" size="sm" onClick={fillGeneratedPassword}>
            <Wand2 className="size-4" aria-hidden />
            توليد كلمة مرور مؤقتة
          </Button>
        </div>

        <PasswordField
          id="reset-new-password"
          label="كلمة المرور المؤقتة"
          autoComplete="new-password"
          error={errors.newPassword?.message}
          registration={register('newPassword')}
        />

        <PasswordField
          id="reset-confirm-password"
          label="تأكيد كلمة المرور المؤقتة"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          registration={register('confirmPassword')}
        />

        {resetMutation.isError && (
          <p
            role="alert"
            className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {toArabicErrorMessage(resetMutation.error)}
          </p>
        )}
      </form>

      <p className="flex items-center gap-1 text-xs text-muted">
        <KeyRound className="size-3.5" aria-hidden />
        الكلمة المؤقتة لا تُخزَّن في السجلات؛ احتفظ بها فقط حتى تسليمها للمستخدم.
      </p>
    </Dialog>
  )
}
