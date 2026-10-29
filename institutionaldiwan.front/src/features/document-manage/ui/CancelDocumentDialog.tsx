import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Textarea } from '@/shared/ui'
import { useCancelDocumentMutation } from '../api/documentMutations'
import {
  cancelDocumentSchema,
  type CancelDocumentFormValues,
} from '../model/schemas'

interface CancelDocumentDialogProps {
  documentId: string
  documentTitle: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Cancels a document with a mandatory reason; blocked while movements are active. */
export function CancelDocumentDialog({
  documentId,
  documentTitle,
  open,
  onOpenChange,
}: CancelDocumentDialogProps) {
  const mutation = useCancelDocumentMutation(documentId)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CancelDocumentFormValues>({
    resolver: zodResolver(cancelDocumentSchema),
    defaultValues: { reason: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({ reason: values.reason.trim() })
      toast.success('تم إلغاء الوثيقة.')
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="إلغاء الوثيقة"
      description={`سيتم إلغاء «${documentTitle}» مع تسجيل السبب في سجل الحالة. لا يمكن إلغاء وثيقة لديها حركة نشطة.`}
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
            form="document-cancel-form"
            variant="destructive"
            loading={mutation.isPending}
          >
            تأكيد الإلغاء
          </Button>
        </>
      }
    >
      <form id="document-cancel-form" onSubmit={onSubmit} noValidate>
        <FormField label="سبب الإلغاء" required error={errors.reason?.message}>
          <Textarea
            rows={3}
            autoFocus
            placeholder="مثال: مُكرّرة؛ استُبدلت بنسخة مصححة."
            invalid={Boolean(errors.reason)}
            {...register('reason')}
          />
        </FormField>

        {mutation.error && (
          <p role="alert" className="mt-4 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
