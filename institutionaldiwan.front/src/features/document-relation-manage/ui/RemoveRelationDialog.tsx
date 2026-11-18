import { t, useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Label, Textarea } from '@/shared/ui'
import { useRemoveRelationMutation } from '../api/relationMutations'
import { removeRelationSchema, type RemoveRelationFormValues } from '../model/schemas'

interface RemoveRelationDialogProps {
  documentId: string
  /** The link to remove; `null` keeps the dialog closed. */
  relation: { relationId: string; relatedTitle: string } | null
  onOpenChange: (open: boolean) => void
}

/** Removes a link after a mandatory reason; the removal stays in the permanent relation history. */
export function RemoveRelationDialog({
  documentId,
  relation,
  onOpenChange,
}: RemoveRelationDialogProps) {
  const mutation = useRemoveRelationMutation(documentId)

  const {
    register,
    handleSubmit,
    draft,
    formState: { errors },
  } = useDraftForm<RemoveRelationFormValues>({ formId: `remove-relation:${relation?.relationId ?? 'none'}`, enabled: relation !== null }, {
    resolver: zodResolver(removeRelationSchema),
    defaultValues: { reason: '' },
  })

  const close = () => {
    mutation.reset()
    onOpenChange(false)
  }

  const onSubmit = handleSubmit(async (values) => {
    if (!relation) return
    try {
      await mutation.mutateAsync({ relationId: relation.relationId, reason: values.reason.trim() })
      toast.success(t('relation.remove.toast'))
      draft.discard()
      close()
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={relation !== null}
      onOpenChange={(nextOpen) => {
        if (mutation.isPending) return
        if (nextOpen) onOpenChange(true)
        else close()
      }}
      title={t('relation.grid.row.remove')}
      description={t('relation.remove.description', { title: relation?.relatedTitle ?? '' })}
      size="sm"
      footer={
        <>
          <Button type="button" variant="outline" onClick={close} disabled={mutation.isPending}>
            <Label k="relation.action.back" />
          </Button>
          <Button
            type="submit"
            form="remove-relation-form"
            variant="destructive"
            loading={mutation.isPending}
          >
            <Label k="relation.remove.submit" />
          </Button>
        </>
      }
    >
      <form id="remove-relation-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField labelKey="relation.remove.field-reason" required error={errors.reason?.message}>
          <Textarea
            rows={3}
            placeholder={t('relation.remove.reason-placeholder')}
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
