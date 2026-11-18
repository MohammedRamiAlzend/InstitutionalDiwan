import { useMemo } from 'react'
import { Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { toArabicErrorMessage } from '@/shared/api'
import { useDraftForm, t, useLabel } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Label, Select, Textarea } from '@/shared/ui'
import { useRecordAppealMutation } from '../api/decisionMutations'
import { recordAppealSchema, type RecordAppealFormValues } from '../model/schemas'

interface RecordAppealDialogProps {
  documentId: string
  decisionId: string
  decisionNumber: string
  owningDepartmentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

function todayInput(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

/** Records an appeal. The server accepts the issuing department or the appellant department. */
export function RecordAppealDialog({
  documentId,
  decisionId,
  decisionNumber,
  owningDepartmentId,
  open,
  onOpenChange,
}: RecordAppealDialogProps) {
  const mutation = useRecordAppealMutation(documentId)
  const departmentsQuery = useDepartmentOptions({ enabled: open })
  const owningDepartmentLabel = useLabel('decisions.appeal.owning-department')
  const departmentOptions = useMemo(() => {
    const options = (departmentsQuery.data ?? []).map((department) => ({
      value: department.departmentId,
      label: department.nameAr,
    }))
    if (owningDepartmentId && !options.some((option) => option.value === owningDepartmentId)) {
      options.unshift({ value: owningDepartmentId, label: owningDepartmentLabel })
    }
    return options
  }, [departmentsQuery.data, owningDepartmentId, owningDepartmentLabel])

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<RecordAppealFormValues>(
    { formId: `record-appeal:${decisionId}`, enabled: open },
    {
      resolver: zodResolver(recordAppealSchema),
      defaultValues: {
        appealDate: todayInput(),
        appellantDepartmentId: owningDepartmentId,
        referenceNumber: '',
        notes: '',
      },
    },
  )

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        decisionId,
        appealDate: values.appealDate,
        appellantDepartmentId: values.appellantDepartmentId,
        referenceNumber: values.referenceNumber.trim() || null,
        notes: values.notes.trim() || null,
      })
      toast.success(t('decisions.appeal.toast'))
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
      title={t('decisions.appeal.title')}
      description={t('decisions.appeal.description')}
      size="lg"
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
            <Label k="common.action.cancel" />
          </Button>
          <Button type="submit" form="record-appeal-form" disabled={mutation.isPending}>
            {mutation.isPending ? t('decisions.appeal.submitting') : <Label k="decisions.appeal.submit" />}
          </Button>
        </>
      }
    >
      <form id="record-appeal-form" className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <p className="text-sm text-muted">
          <Label k="decisions.decision-prefix" /> <span dir="ltr" className="font-mono">{decisionNumber}</span>
        </p>
        <FormField
          guideId="document-details.record-appeal"
          labelKey="decisions.appeal.field-date"
          required
          error={errors.appealDate?.message}
        >
          <Input {...register('appealDate')} type="date" dir="ltr" invalid={Boolean(errors.appealDate)} />
        </FormField>
        <FormField
          labelKey="decisions.appeal.field-department"
          required
          error={errors.appellantDepartmentId?.message}
          hint={t('decisions.appeal.department-hint')}
        >
          <Controller
            control={control}
            name="appellantDepartmentId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={departmentOptions}
                placeholder={t('decisions.appeal.department-placeholder')}
                invalid={Boolean(errors.appellantDepartmentId)}
              />
            )}
          />
        </FormField>
        <FormField
          guideId="document-details.appeal-reference"
          labelKey="decisions.appeal.field-reference"
          error={errors.referenceNumber?.message}
        >
          <Input {...register('referenceNumber')} dir="ltr" invalid={Boolean(errors.referenceNumber)} />
        </FormField>
        <FormField labelKey="decisions.appeal.field-notes" error={errors.notes?.message}>
          <Textarea {...register('notes')} invalid={Boolean(errors.notes)} />
        </FormField>
      </form>
    </Dialog>
  )
}
