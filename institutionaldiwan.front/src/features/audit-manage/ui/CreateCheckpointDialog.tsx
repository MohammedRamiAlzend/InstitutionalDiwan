import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Hash } from 'lucide-react'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { formatNumber } from '@/shared/lib'
import { Button, Dialog, FormField, Input } from '@/shared/ui'
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
  } = useForm<CheckpointFormValues>({
    resolver: zodResolver(checkpointSchema),
    defaultValues: { toAuditLogId: toAuditLogId ? String(toAuditLogId) : '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      const checkpoint = await mutation.mutateAsync({
        toAuditLogId: values.toAuditLogId ? Number(values.toAuditLogId) : null,
      })
      toast.success(
        `تم إنشاء نقطة تحقق تضم ${formatNumber(checkpoint.recordCount)} سجلًا (حتى السجل #${formatNumber(checkpoint.toAuditLogId)}).`,
      )
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="إنشاء نقطة تحقق"
      description="تجمع نقطة التحقق كل السجلات الجديدة منذ آخر نقطة في نقطة مرجعية غير قابلة للتعديل. لا يمكن إنشاؤها إن لم تصدر سجلات جديدة."
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
          <Button type="submit" form="create-checkpoint-form" loading={mutation.isPending}>
            إنشاء نقطة التحقق
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
          label="معرّف آخر سجل (اختياري)"
          error={errors.toAuditLogId?.message}
        >
          <Input
            type="number"
            inputMode="numeric"
            dir="ltr"
            min={1}
            placeholder="مثال: 120"
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
