import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Hash } from 'lucide-react'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input } from '@/shared/ui'
import { useIssueOfficialNumberMutation } from '../api/numberMutations'
import { issueNumberSchema, type IssueNumberFormValues } from '../model/schemas'

interface IssueNumberDialogProps {
  documentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Issues the next number from the matching registry and moves the draft to registered. */
export function IssueNumberDialog({
  documentId,
  open,
  onOpenChange,
}: IssueNumberDialogProps) {
  const mutation = useIssueOfficialNumberMutation(documentId)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<IssueNumberFormValues>({
    resolver: zodResolver(issueNumberSchema),
    defaultValues: { numberingYear: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      const issued = await mutation.mutateAsync({
        numberingYear: values.numberingYear.trim()
          ? Number(values.numberingYear)
          : null,
      })
      toast.success(
        `تم إصدار الرقم ${issued.officialNumberText} وتسجيل الوثيقة.`,
      )
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="إصدار رقم رسمي"
      description="سيُخصص الرقم التالي من سجل الترقيم المطابق لدائرة الوثيقة ونوعها واتجاهها، وتنتقل الوثيقة من المسودة إلى «مسجل»."
      size="sm"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            إلغاء
          </Button>
          <Button type="submit" form="issue-number-form" loading={mutation.isPending}>
            إصدار الرقم
          </Button>
        </>
      }
    >
      <form id="issue-number-form" onSubmit={onSubmit} noValidate>
        <FormField label="سنة الترقيم (اختياري)" error={errors.numberingYear?.message}>
          <Input
            type="number"
            inputMode="numeric"
            dir="ltr"
            min={1900}
            max={2200}
            placeholder="اتركها فارغة لسنة الوثيقة أو السنة الحالية"
            startIcon={<Hash className="size-4" aria-hidden />}
            invalid={Boolean(errors.numberingYear)}
            {...register('numberingYear')}
          />
        </FormField>

        {mutation.error && (
          <p
            role="alert"
            className="mt-4 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
