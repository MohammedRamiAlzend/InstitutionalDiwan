import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { Button, Dialog, Label } from '@/shared/ui'
import { useRevokeDocumentAccessMutation } from '../api/accessGrantMutations'

interface RevokeAccessGrantDialogProps {
  documentId: string
  grantId: string
  /** Username shown in the confirmation copy. */
  granteeLabel: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * Revokes one grant. The endpoint records the caller and the timestamp only, so
 * the confirmation carries no reason field; the revoked row stays in the list.
 */
export function RevokeAccessGrantDialog({
  documentId,
  grantId,
  granteeLabel,
  open,
  onOpenChange,
}: RevokeAccessGrantDialogProps) {
  const mutation = useRevokeDocumentAccessMutation(documentId)

  const onConfirm = async () => {
    try {
      await mutation.mutateAsync(grantId)
      toast.success(t('docdetails.grant.toast-revoked'))
      onOpenChange(false)
    } catch {
      // The mutation error is rendered below.
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('docdetails.grant.revoke-title')}
      description={t('docdetails.grant.revoke-description', { name: granteeLabel })}
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
            type="button"
            variant="destructive"
            onClick={() => void onConfirm()}
            loading={mutation.isPending}
          >
            <Label k="docdetails.action.confirm-cancel" />
          </Button>
        </>
      }
    >
      {mutation.error && (
        <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          {toArabicErrorMessage(mutation.error)}
        </p>
      )}
    </Dialog>
  )
}
