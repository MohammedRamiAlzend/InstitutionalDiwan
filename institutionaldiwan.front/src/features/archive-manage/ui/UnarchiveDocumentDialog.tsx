import { t, useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Label, Textarea } from '@/shared/ui'
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
    draft,
  } = useDraftForm<UnarchiveDocumentFormValues>({ formId: `unarchive-document:${documentId}` }, {
    resolver: zodResolver(unarchiveDocumentSchema),
    defaultValues: { reason: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({ reason: values.reason.trim() })
      toast.success(t('archive.unarchive.toast'))
      draft.discard()
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
      title={t('archive.unarchive.title')}
      description={t('archive.unarchive.description', { title: documentTitle })}
      size="sm"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            <Label k="archive.dismiss" />
          </Button>
          <Button type="submit" form="unarchive-document-form" loading={mutation.isPending}>
            <Label k="archive.unarchive.submit" />
          </Button>
        </>
      }
    >
      <form id="unarchive-document-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField labelKey="archive.unarchive.field-reason" required error={errors.reason?.message}>
          <Textarea
            rows={3}
            autoFocus
            placeholder={t('archive.unarchive.reason-placeholder')}
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
