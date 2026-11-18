import { zodResolver } from '@hookform/resolvers/zod'
import { Hash } from 'lucide-react'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { formatNumber, t, useDraftForm } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Label } from '@/shared/ui'
import { useCreateCheckpointMutation } from '../api/createCheckpoint'
import { checkpointSchema, type CheckpointFormValues } from '../model/checkpoint.schema'

interface CreateCheckpointDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Prefills the upper bound when the action starts from one row. */
  toAuditLogId?: number
}

/** Seals every entry since the previous anchor; blocked when nothing new exists. */
export function CreateCheckpointDialog({
  open,
  onOpenChange,
  toAuditLogId,
}: CreateCheckpointDialogProps) {
  const mutation = useCreateCheckpointMutation()

  const {
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<CheckpointFormValues>({ formId: 'audit-checkpoint' }, {
    resolver: zodResolver(checkpointSchema),
    defaultValues: { toAuditLogId: toAuditLogId ? String(toAuditLogId) : '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      const checkpoint = await mutation.mutateAsync({
        toAuditLogId: values.toAuditLogId ? Number(values.toAuditLogId) : null,
      })
      toast.success(
        t('audit.checkpoint.toast-created', {
          count: formatNumber(checkpoint.recordCount),
          to: formatNumber(checkpoint.toAuditLogId),
        }),
      )
      draft.discard()
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('audit.checkpoint.dialog.title')}
      description={t('audit.checkpoint.dialog.description')}
      size="sm"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            <Label k="audit.checkpoint.back" />
          </Button>
          <Button type="submit" form="create-checkpoint-form" loading={mutation.isPending}>
            <Label k="audit.checkpoint.dialog.submit" />
          </Button>
        </>
      }
    >
      <form
        id="create-checkpoint-form"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <FormField
          labelKey="audit.checkpoint.field.to"
          error={errors.toAuditLogId?.message}
        >
          <Input
            type="number"
            inputMode="numeric"
            dir="ltr"
            min={1}
            placeholder={t('audit.checkpoint.field.to-placeholder')}
            startIcon={<Hash className="size-4" aria-hidden />}
            invalid={Boolean(errors.toAuditLogId)}
            {...register('toAuditLogId')}
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
