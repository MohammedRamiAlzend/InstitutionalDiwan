import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog } from '@/shared/ui'
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
      toast.success('تم إلغاء تصريح الوصول.')
      onOpenChange(false)
    } catch {
      // The mutation error is rendered below.
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="إلغاء تصريح الوصول"
      description={`سيُلغى تصريح «${granteeLabel}» ويبقى سجلًّا مع تاريخ الإلغاء واسم من ألغاه. لا يمكن التراجع عن الإلغاء.`}
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
            type="button"
            variant="destructive"
            onClick={() => void onConfirm()}
            loading={mutation.isPending}
          >
            تأكيد الإلغاء
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
