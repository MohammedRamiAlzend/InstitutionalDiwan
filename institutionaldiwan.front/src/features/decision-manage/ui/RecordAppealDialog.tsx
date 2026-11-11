import { useMemo } from 'react'
import { Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { toArabicErrorMessage } from '@/shared/api'
import { useDraftForm } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Select, Textarea } from '@/shared/ui'
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
  const departmentOptions = useMemo(() => {
    const options = (departmentsQuery.data ?? []).map((department) => ({
      value: department.departmentId,
      label: department.nameAr,
    }))
    if (owningDepartmentId && !options.some((option) => option.value === owningDepartmentId)) {
      options.unshift({ value: owningDepartmentId, label: 'الديوان المنشئ' })
    }
    return options
  }, [departmentsQuery.data, owningDepartmentId])

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
      toast.success('سُجّل الاعتراض.')
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
      title="تسجيل اعتراض"
      description="يسجّل الاعتراض على القرار الصادر. لا مهلة، ولا يمنع نقل الوثيقة."
      size="lg"
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
            إلغاء
          </Button>
          <Button type="submit" form="record-appeal-form" disabled={mutation.isPending}>
            {mutation.isPending ? 'جارٍ التسجيل…' : 'تسجيل الاعتراض'}
          </Button>
        </>
      }
    >
      <form id="record-appeal-form" className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <p className="text-sm text-muted">
          القرار <span dir="ltr" className="font-mono">{decisionNumber}</span>
        </p>
        <FormField
          guideId="document-details.record-appeal"
          label="تاريخ الاعتراض"
          required
          error={errors.appealDate?.message}
        >
          <Input {...register('appealDate')} type="date" dir="ltr" invalid={Boolean(errors.appealDate)} />
        </FormField>
        <FormField
          label="القسم المعترض"
          required
          error={errors.appellantDepartmentId?.message}
          hint="الافتراضي هو الديوان الذي أنشأ الوثيقة."
        >
          <Controller
            control={control}
            name="appellantDepartmentId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={departmentOptions}
                placeholder="اختر القسم"
                invalid={Boolean(errors.appellantDepartmentId)}
              />
            )}
          />
        </FormField>
        <FormField
          guideId="document-details.appeal-reference"
          label="الرقم المرجعي"
          error={errors.referenceNumber?.message}
        >
          <Input {...register('referenceNumber')} dir="ltr" invalid={Boolean(errors.referenceNumber)} />
        </FormField>
        <FormField label="ملاحظات" error={errors.notes?.message}>
          <Textarea {...register('notes')} invalid={Boolean(errors.notes)} />
        </FormField>
      </form>
    </Dialog>
  )
}
