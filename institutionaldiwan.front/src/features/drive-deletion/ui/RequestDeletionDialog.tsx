import { useState } from 'react'
import { toast } from 'sonner'
import { useRequestDriveDeletionMutation } from '../api/driveDeletionMutations'
import { deletionReasonSchema } from '../model/schemas'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { Button, Dialog, FormField, Label, Textarea } from '@/shared/ui'

export function RequestDeletionDialog({
  itemId,
  open,
  onOpenChange,
}: {
  itemId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const mutation = useRequestDriveDeletionMutation(itemId)
  const [reason, setReason] = useState('')
  const submit = async () => {
    const parsed = deletionReasonSchema.safeParse({ reason })
    if (!parsed.success) {
      toast.error(parsed.error.issues[0]?.message ?? t('drive.validation.reason-required'))
      return
    }
    try {
      await mutation.mutateAsync(parsed.data.reason)
      toast.success(t('drive.toast.deletion-requested'))
      setReason('')
      onOpenChange(false)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('drive.dialog.deletion-title')}
      description={t('drive.confirm.request-deletion-description')}
      footer={
        <>
          <Button variant="outline" type="button" onClick={() => onOpenChange(false)}>
            <Label k="common.action.cancel" />
          </Button>
          <Button type="button" onClick={() => void submit()} loading={mutation.isPending}>
            <Label k="drive.action.request-deletion" />
          </Button>
        </>
      }
    >
      <FormField labelKey="drive.dialog.deletion-reason" required>
        <Textarea value={reason} onChange={(event) => setReason(event.target.value)} maxLength={500} rows={4} />
      </FormField>
    </Dialog>
  )
}
