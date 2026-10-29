import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { DocumentNumberResponse } from '@/entities/document'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Textarea } from '@/shared/ui'
import { useCancelOfficialNumberMutation } from '../api/numberMutations'
import { cancelNumberSchema, type CancelNumberFormValues } from '../model/schemas'

interface CancelNumberDialogProps {
  documentId: string
  number: DocumentNumberResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Cancels one active official number; the record and its reason stay in history. */
export function CancelNumberDialog({
  documentId,
  number,
  open,
  onOpenChange,
}: CancelNumberDialogProps) {
  const mutation = useCancelOfficialNumberMutation(documentId)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CancelNumberFormValues>({
    resolver: zodResolver(cancelNumberSchema),
    defaultValues: { reason: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        documentNumberId: number.documentNumberId,
        reason: values.reason.trim(),
      })
      toast.success('تم إلغاء الرقم الرسمي.')
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="إلغاء الرقم الرسمي"
      description={`سيُعلَّم الرقم «${number.officialNumberText}» ملغى مع سبب الإلغاء واسم المنفّذ، ويبقى في سجل الأرقام.`}
      size="sm"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            تراجع
          </Button>
          <Button
            type="submit"
            form="cancel-number-form"
            variant="destructive"
            loading={mutation.isPending}
          >
            تأكيد الإلغاء
          </Button>
        </>
      }
    >
      <form id="cancel-number-form" onSubmit={onSubmit} noValidate>
        <FormField label="سبب الإلغاء" required error={errors.reason?.message}>
          <Textarea
            rows={3}
            placeholder="مثال: قيد مكرر؛ استُبدل بنسخة مصححة."
            invalid={Boolean(errors.reason)}
            {...register('reason')}
          />
        </FormField>

        {mutation.error && (
          <p
            role="alert"
            className="mt-4 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
