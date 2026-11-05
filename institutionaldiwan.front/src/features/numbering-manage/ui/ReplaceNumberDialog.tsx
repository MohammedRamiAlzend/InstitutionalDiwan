import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { DocumentNumberResponse } from '@/entities/document'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Textarea } from '@/shared/ui'
import { useReplaceOfficialNumberMutation } from '../api/numberMutations'
import { replaceNumberSchema, type ReplaceNumberFormValues } from '../model/schemas'

interface ReplaceNumberDialogProps {
  documentId: string
  number: DocumentNumberResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Replaces an active number: issues the next one, links both, and re-points the document. */
export function ReplaceNumberDialog({
  documentId,
  number,
  open,
  onOpenChange,
}: ReplaceNumberDialogProps) {
  const mutation = useReplaceOfficialNumberMutation(documentId)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ReplaceNumberFormValues>({
    resolver: zodResolver(replaceNumberSchema),
    defaultValues: { reason: '', numberingYear: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      const replacement = await mutation.mutateAsync({
        documentNumberId: number.documentNumberId,
        reason: values.reason.trim(),
        numberingYear: values.numberingYear.trim()
          ? Number(values.numberingYear)
          : null,
      })
      toast.success(`تم استبدال الرقم بالرقم ${replacement.officialNumberText}.`)
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="استبدال رقم النظام"
      description={`سيُخصص الرقم التالي ويُربط بالرقم «${number.officialNumberText}» الذي سيبقى في السجل بحالة «مُستبدل»، وتُحدَّث الوثيقة بالرقم الجديد.`}
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
          <Button type="submit" form="replace-number-form" loading={mutation.isPending}>
            استبدال الرقم
          </Button>
        </>
      }
    >
      <form id="replace-number-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField label="سبب الاستبدال" required error={errors.reason?.message}>
          <Textarea
            rows={3}
            placeholder="مثال: خطأ في السنة؛ صدر الرقم بسنة خاطئة."
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
