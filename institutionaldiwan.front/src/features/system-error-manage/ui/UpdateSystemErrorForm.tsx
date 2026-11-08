import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import {
  SYSTEM_ERROR_STATUS_LABELS,
  type SystemErrorDetailsResponse,
  type SystemErrorStatus,
} from '@/entities/system-error'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, FormField, Select, Textarea } from '@/shared/ui'
import { useUpdateSystemErrorMutation } from '../api/systemErrorMutations'
import { updateSystemErrorSchema, type UpdateSystemErrorFormValues } from '../model/schemas'

const STATUS_OPTIONS = Object.entries(SYSTEM_ERROR_STATUS_LABELS).map(([value, label]) => ({ value, label }))

/** Where the developer stands with one error: status and free notes. */
export function UpdateSystemErrorForm({ error }: { error: SystemErrorDetailsResponse }) {
  const mutation = useUpdateSystemErrorMutation(error.summary.systemErrorId)
  const {
    control,
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<UpdateSystemErrorFormValues>({
    resolver: zodResolver(updateSystemErrorSchema),
    defaultValues: { status: String(error.summary.status), notes: error.notes ?? '' },
  })

  const onSubmit = handleSubmit((values) =>
    mutation.mutate(
      { status: Number(values.status) as SystemErrorStatus, notes: values.notes || null },
      { onSuccess: () => toast.success('حُفظت حالة الخطأ.') },
    ),
  )

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-3">
      <FormField label="الحالة" required>
        <Controller
          control={control}
          name="status"
          render={({ field }) => (
            <Select aria-label="حالة الخطأ" value={field.value} onValueChange={field.onChange} options={STATUS_OPTIONS} />
          )}
        />
      </FormField>
      <FormField label="ملاحظات المطور" error={errors.notes?.message}>
        <Textarea rows={3} invalid={Boolean(errors.notes)} {...register('notes')} />
      </FormField>
      {mutation.error && (
        <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          {toArabicErrorMessage(mutation.error)}
        </p>
      )}
      <div>
        <Button type="submit" loading={mutation.isPending}>
          حفظ
        </Button>
      </div>
    </form>
  )
}
