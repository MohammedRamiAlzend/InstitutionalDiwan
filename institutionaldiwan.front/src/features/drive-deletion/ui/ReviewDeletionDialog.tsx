import { useState } from 'react'
import { toast } from 'sonner'
import { useReviewDriveDeletionMutation } from '../api/driveDeletionMutations'
import { reviewNoteSchema } from '../model/schemas'
import type { DriveDeletionRequestResponse } from '@/entities/drive-item'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { Button, Dialog, FormField, Label, Textarea } from '@/shared/ui'

export function ReviewDeletionDialog({
  request,
  action,
  open,
  onOpenChange,
}: {
  request: DriveDeletionRequestResponse
  action: 'approve' | 'reject'
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const mutation = useReviewDriveDeletionMutation(action)
  const [note, setNote] = useState('')
  const submit = async () => {
    const parsed = reviewNoteSchema.safeParse({ note })
    if (!parsed.success) return
    try {
      await mutation.mutateAsync({ requestId: request.requestId, note: parsed.data.note })
      toast.success(t(action === 'approve' ? 'drive.toast.deletion-approved' : 'drive.toast.deletion-rejected'))
      setNote('')
      onOpenChange(false)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('drive.dialog.review-title')}
      description={request.reason}
      footer={
        <>
          <Button variant="outline" type="button" onClick={() => onOpenChange(false)}>
            <Label k="common.action.cancel" />
          </Button>
          <Button type="button" variant={action === 'reject' ? 'destructive' : 'primary'} onClick={() => void submit()} loading={mutation.isPending}>
            <Label k={action === 'approve' ? 'common.action.confirm' : 'drive.toast.deletion-rejected'} />
          </Button>
        </>
      }
    >
      <FormField labelKey="drive.dialog.review-note">
        <Textarea value={note} onChange={(event) => setNote(event.target.value)} maxLength={500} rows={4} />
      </FormField>
    </Dialog>
  )
}
