import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { useDraftForm, t } from '@/shared/lib'
import { Button, Dialog, FormField, Label, Textarea } from '@/shared/ui'
import { useCancelDecisionMutation } from '../api/decisionMutations'
import { cancelDecisionSchema, type CancelDecisionFormValues } from '../model/schemas'

interface CancelDecisionDialogProps {
  documentId: string
  decisionId: string
  decisionNumber: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Voids a decision entered by mistake. The server accepts this from a system administrator only. */
export function CancelDecisionDialog({
  documentId,
  decisionId,
  decisionNumber,
  open,
  onOpenChange,
}: CancelDecisionDialogProps) {
  const mutation = useCancelDecisionMutation(documentId)
  const {
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<CancelDecisionFormValues>(
    { formId: `cancel-decision:${decisionId}`, enabled: open },
    { resolver: zodResolver(cancelDecisionSchema), defaultValues: { reason: '' } },
  )

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({ decisionId, reason: values.reason.trim() })
      toast.success(t('decisions.cancel.toast'))
      draft.discard()
      onOpenChange(false)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('decisions.cancel.title')}
      description={t('decisions.cancel.description', { number: decisionNumber })}
      size="md"
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
            <Label k="decisions.cancel.dismiss" />
          </Button>
          <Button type="submit" form="cancel-decision-form" variant="destructive" disabled={mutation.isPending}>
            {mutation.isPending ? t('decisions.cancel.submitting') : <Label k="decisions.cancel.submit" />}
          </Button>
        </>
      }
    >
      <form id="cancel-decision-form" className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <FormField labelKey="decisions.cancel.field-reason" required error={errors.reason?.message} guideId="decisions.cancel-reason">
          <Textarea {...register('reason')} invalid={Boolean(errors.reason)} />
        </FormField>
      </form>
    </Dialog>
  )
}
