import { useMemo } from 'react'
import { Controller, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { useDecisionTypes } from '@/entities/decision-type'
import { useDocumentFiles } from '@/entities/document-file'
import { toArabicErrorMessage } from '@/shared/api'
import { DECISION_EFFECT, DOCUMENT_FILE_TYPE, decisionEffectLabel } from '@/shared/config'
import { useDraftForm } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Select, Textarea } from '@/shared/ui'
import { useIssueDecisionMutation } from '../api/decisionMutations'
import { issueDecisionSchema, type IssueDecisionFormValues } from '../model/schemas'
import { JoinTargetPicker } from './JoinTargetPicker'

interface IssueDecisionDialogProps {
  documentId: string
  documentTypeId: number
  open: boolean
  onOpenChange: (open: boolean) => void
}

function todayInput(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

/** Issues a decision. A unification decision also names the principal file. */
export function IssueDecisionDialog({
  documentId,
  documentTypeId,
  open,
  onOpenChange,
}: IssueDecisionDialogProps) {
  const mutation = useIssueDecisionMutation(documentId)
  const typesQuery = useDecisionTypes(
    { documentTypeId, isActive: true },
    { enabled: open },
  )
  const filesQuery = useDocumentFiles(documentId, { enabled: open })
  const departmentsQuery = useDepartmentOptions({ enabled: open })

  const offerable = useMemo(() => typesQuery.data ?? [], [typesQuery.data])

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
    draft,
  } = useDraftForm<IssueDecisionFormValues>(
    { formId: `issue-decision:${documentId}`, enabled: open },
    {
      resolver: zodResolver(issueDecisionSchema),
      defaultValues: {
        decisionTypeId: '',
        decisionNumber: '',
        decisionDate: todayInput(),
        summary: '',
        documentFileId: '',
        joinTargetDocumentId: '',
      },
    },
  )

  const selectedTypeId = useWatch({ control, name: 'decisionTypeId' })
  const selectedType = offerable.find((type) => type.decisionTypeId === selectedTypeId)
  const isJoin = selectedType?.effectId === DECISION_EFFECT.Join
  const destinationName = departmentsQuery.data?.find(
    (department) => department.departmentId === selectedType?.destinationDepartmentId,
  )?.nameAr

  const typeOptions = offerable.map((type) => ({
    value: type.decisionTypeId,
    label: `${type.nameAr} · ${decisionEffectLabel(type.effectId)}`,
  }))

  const fileOptions = (filesQuery.data ?? [])
    .filter((file) => file.fileTypeId === DOCUMENT_FILE_TYPE.Decision && !file.isArchived)
    .map((file) => ({ value: file.fileId, label: file.title }))

  const onSubmit = handleSubmit(async (values) => {
    if (isJoin && !values.joinTargetDocumentId) {
      setError('joinTargetDocumentId', { message: 'اختر الملف الرئيسي.' })
      return
    }
    try {
      await mutation.mutateAsync({
        decisionTypeId: values.decisionTypeId,
        decisionNumber: values.decisionNumber.trim(),
        decisionDate: values.decisionDate,
        summary: values.summary.trim() || null,
        documentFileId: values.documentFileId || null,
        joinTargetDocumentId: isJoin ? values.joinTargetDocumentId : null,
      })
      toast.success(
        isJoin
          ? 'صدر قرار التوحيد وارتبط الملف بالملف الرئيسي. لم يُغيَّر الرقم ولا المكان.'
          : 'صدر القرار. الأثر مقترح ولم يُنفَّذ بعد.',
      )
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
      title="إصدار قرار"
      description="يُصدر القرار من الديوان الذي يحمل الوثيقة. القرار غير القابل للاعتراض يولد نهائيًا."
      size="lg"
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
            إلغاء
          </Button>
          <Button type="submit" form="issue-decision-form" disabled={mutation.isPending || typeOptions.length === 0}>
            {mutation.isPending ? 'جارٍ الإصدار…' : 'إصدار'}
          </Button>
        </>
      }
    >
      <form id="issue-decision-form" className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <FormField label="نوع القرار" required error={errors.decisionTypeId?.message}>
          <Controller
            control={control}
            name="decisionTypeId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={typeOptions}
                placeholder="اختر النوع"
                invalid={Boolean(errors.decisionTypeId)}
              />
            )}
          />
        </FormField>
        {selectedType?.effectId === DECISION_EFFECT.Refer && (
          <p className="text-sm text-muted">
            الوجهة المقترحة: {destinationName ?? 'قسم الوجهة المعرّف على النوع'}
          </p>
        )}
        {selectedType?.effectId === DECISION_EFFECT.SendToArchive && (
          <p className="text-sm text-muted">
            بعد الإصدار يُقترح إرسال الوثيقة للحفظ. الأرشفة نفسها تبقى إجراءً منفصلاً.
          </p>
        )}
        {isJoin && (
          <>
            <p className="text-sm text-muted">
              يُربط هذا الملف بالملف الرئيسي دون تغيير رقمه أو مكانه. إن كان الملفان عند ديوانين
              مختلفين، انقل هذا الملف بحركة عادية إلى ديوان الملف الرئيسي بعد الإصدار. لا تُنشأ الحركة
              تلقائيًا.
            </p>
            <FormField
              label="الملف الرئيسي"
              required
              error={errors.joinTargetDocumentId?.message}
              guideId="document-details.join-target"
            >
              <Controller
                control={control}
                name="joinTargetDocumentId"
                render={({ field }) => (
                  <JoinTargetPicker
                    excludeDocumentId={documentId}
                    value={field.value}
                    invalid={Boolean(errors.joinTargetDocumentId)}
                    onChange={field.onChange}
                  />
                )}
              />
            </FormField>
          </>
        )}
        <FormField label="رقم القرار" required error={errors.decisionNumber?.message} guideId="document-details.decision-number">
          <Input {...register('decisionNumber')} dir="ltr" invalid={Boolean(errors.decisionNumber)} />
        </FormField>
        <FormField label="تاريخ القرار" required error={errors.decisionDate?.message}>
          <Input {...register('decisionDate')} type="date" dir="ltr" invalid={Boolean(errors.decisionDate)} />
        </FormField>
        <FormField label="الخلاصة" error={errors.summary?.message}>
          <Textarea {...register('summary')} invalid={Boolean(errors.summary)} />
        </FormField>
        {fileOptions.length > 0 && (
          <FormField label="نسخة القرار" error={errors.documentFileId?.message} hint="اختياري. ملف من نوع قرار على هذه الوثيقة.">
            <Controller
              control={control}
              name="documentFileId"
              render={({ field }) => (
                <Select
                  value={field.value || undefined}
                  onValueChange={field.onChange}
                  options={fileOptions}
                  placeholder="بدون ملف"
                />
              )}
            />
          </FormField>
        )}
      </form>
    </Dialog>
  )
}
