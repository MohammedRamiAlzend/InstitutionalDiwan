import { t, useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { DocumentNumberResponse } from '@/entities/document'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Label, Textarea } from '@/shared/ui'
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
    draft,
  } = useDraftForm<CancelNumberFormValues>({ formId: `cancel-number:${number.documentNumberId}` }, {
    resolver: zodResolver(cancelNumberSchema),
    defaultValues: { reason: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        documentNumberId: number.documentNumberId,
        reason: values.reason.trim(),
      })
      toast.success(t('numbering.cancel.toast'))
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
      title={t('numbering.cancel.title')}
      description={t('numbering.cancel.description', { number: number.officialNumberText })}
      size="sm"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            <Label k="numbering.action.back" />
          </Button>
          <Button
            type="submit"
            form="cancel-number-form"
            variant="destructive"
            loading={mutation.isPending}
          >
            <Label k="numbering.cancel.submit" />
          </Button>
        </>
      }
    >
      <form id="cancel-number-form" onSubmit={onSubmit} noValidate>
        <FormField labelKey="numbering.cancel.reason" required error={errors.reason?.message}>
          <Textarea
            rows={3}
            placeholder={t('numbering.cancel.reason-placeholder')}
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
