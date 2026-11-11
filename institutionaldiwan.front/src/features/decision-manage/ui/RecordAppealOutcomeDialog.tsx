import { Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { APPEAL_OUTCOME } from '@/shared/config'
import { useDraftForm } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Select } from '@/shared/ui'
import { useRecordAppealOutcomeMutation } from '../api/decisionMutations'
import { recordAppealOutcomeSchema, type RecordAppealOutcomeFormValues } from '../model/schemas'

interface RecordAppealOutcomeDialogProps {
  documentId: string
  decisionId: string
  appealId: string
  decisionNumber: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

function todayInput(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

const OUTCOME_OPTIONS = [
  { value: String(APPEAL_OUTCOME.Accepted), label: 'مقبول' },
  { value: String(APPEAL_OUTCOME.Rejected), label: 'مردود' },
]

/** Records the ruling. Accepted revokes the decision; rejected makes it final. */
export function RecordAppealOutcomeDialog({
  documentId,
  decisionId,
  appealId,
  decisionNumber,
  open,
  onOpenChange,
}: RecordAppealOutcomeDialogProps) {
  const mutation = useRecordAppealOutcomeMutation(documentId)
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<RecordAppealOutcomeFormValues>(
    { formId: `appeal-outcome:${appealId}`, enabled: open },
    {
      resolver: zodResolver(recordAppealOutcomeSchema),
      defaultValues: { outcomeDate: todayInput() },
    },
  )

  const onSubmit = handleSubmit(async (values) => {
    try {
      const outcomeId = Number(values.outcomeId)
      await mutation.mutateAsync({
        decisionId,
        appealId,
        outcomeId,
        outcomeDate: values.outcomeDate,
      })
      toast.success(outcomeId === APPEAL_OUTCOME.Accepted ? 'قُبل الاعتراض وفُسخ القرار.' : 'رُدّ الاعتراض وصار القرار نهائيًا.')
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
      title="نتيجة الاعتراض"
      description="القبول يفسخ القرار فيصبح إصدار قرار جديد ممكنًا. الرد يجعل القرار نهائيًا."
      size="md"
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
            إلغاء
          </Button>
          <Button type="submit" form="appeal-outcome-form" disabled={mutation.isPending}>
            {mutation.isPending ? 'جارٍ التسجيل…' : 'تسجيل النتيجة'}
          </Button>
        </>
      }
    >
      <form id="appeal-outcome-form" className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <p className="text-sm text-muted">
          القرار <span dir="ltr" className="font-mono">{decisionNumber}</span>
        </p>
        <FormField
          guideId="document-details.appeal-outcome"
          label="النتيجة"
          required
          error={errors.outcomeId?.message}
        >
          <Controller
            control={control}
            name="outcomeId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={OUTCOME_OPTIONS}
                placeholder="اختر النتيجة"
                invalid={Boolean(errors.outcomeId)}
              />
            )}
          />
        </FormField>
        <FormField label="تاريخ النتيجة" required error={errors.outcomeDate?.message}>
          <Input {...register('outcomeDate')} type="date" dir="ltr" invalid={Boolean(errors.outcomeDate)} />
        </FormField>
      </form>
    </Dialog>
  )
}
