import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Textarea } from '@/shared/ui'
import { useUnarchiveDocumentMutation } from '../api/archiveMutations'
import { unarchiveDocumentSchema, type UnarchiveDocumentFormValues } from '../model/schemas'

interface UnarchiveDocumentDialogProps {
  documentId: string
  documentTitle: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Restores an archived document to the status it held before; the reason is recorded in its history. */
export function UnarchiveDocumentDialog({
  documentId,
  documentTitle,
  open,
  onOpenChange,
}: UnarchiveDocumentDialogProps) {
  const mutation = useUnarchiveDocumentMutation(documentId)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<UnarchiveDocumentFormValues>({
    resolver: zodResolver(unarchiveDocumentSchema),
    defaultValues: { reason: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({ reason: values.reason.trim() })
      toast.success('أُلغيت أرشفة الوثيقة.')
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && mutation.isPending) return
        onOpenChange(nextOpen)
      }}
      title="إلغاء أرشفة الوثيقة"
      description={`ستعود «${documentTitle}» إلى حالتها قبل الأرشفة وتصبح قابلة للتعديل من جديد.`}
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
          <Button type="submit" form="unarchive-document-form" loading={mutation.isPending}>
            إلغاء الأرشفة
          </Button>
        </>
      }
    >
      <form id="unarchive-document-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField label="سبب إلغاء الأرشفة" required error={errors.reason?.message}>
          <Textarea
            rows={3}
            autoFocus
            placeholder="مثال: وردت مراسلة جديدة تتعلق بالمعاملة."
            invalid={Boolean(errors.reason)}
            {...register('reason')}
          />
        </FormField>

        {mutation.error && (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
