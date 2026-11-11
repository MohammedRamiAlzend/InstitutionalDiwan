import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { useDraftForm } from '@/shared/lib'
import { Button, Dialog, FormField, Textarea } from '@/shared/ui'
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
      toast.success('أُلغي إدخال القرار.')
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
      title="إلغاء إدخال القرار"
      description={`يلغي القرار ${decisionNumber} لأنه أُدخل خطأ. هذا الإجراء لمدير النظام، ولا يُنشئ حركة إعادة.`}
      size="md"
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
            تراجع
          </Button>
          <Button type="submit" form="cancel-decision-form" variant="destructive" disabled={mutation.isPending}>
            {mutation.isPending ? 'جارٍ الإلغاء…' : 'إلغاء الإدخال'}
          </Button>
        </>
      }
    >
      <form id="cancel-decision-form" className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <FormField label="السبب" required error={errors.reason?.message} guideId="decisions.cancel-reason">
          <Textarea {...register('reason')} invalid={Boolean(errors.reason)} />
        </FormField>
      </form>
    </Dialog>
  )
}
