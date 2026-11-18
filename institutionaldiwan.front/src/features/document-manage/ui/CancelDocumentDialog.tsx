import { t, useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Label, Textarea } from '@/shared/ui'
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
    draft,
  } = useDraftForm<CancelDocumentFormValues>({ formId: `cancel-document:${documentId}` }, {
    resolver: zodResolver(cancelDocumentSchema),
    defaultValues: { reason: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({ reason: values.reason.trim() })
      toast.success(t('docdetails.cancel.toast'))
      draft.discard()
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('docdetails.action.cancel')}
      description={t('docdetails.cancel.description', { title: documentTitle })}
      size="sm"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            <Label k="docdetails.action.back" />
          </Button>
          <Button
            type="submit"
            form="document-cancel-form"
            variant="destructive"
            loading={mutation.isPending}
          >
            <Label k="docdetails.action.confirm-cancel" />
          </Button>
        </>
      }
    >
      <form id="document-cancel-form" onSubmit={onSubmit} noValidate>
        <FormField labelKey="docdetails.cancel.field-reason" required error={errors.reason?.message}>
          <Textarea
            rows={3}
            autoFocus
            placeholder={t('docdetails.cancel.reason-placeholder')}
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
