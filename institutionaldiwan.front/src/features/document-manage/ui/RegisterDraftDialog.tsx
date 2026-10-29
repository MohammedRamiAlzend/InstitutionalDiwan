import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Hash } from 'lucide-react'
import { toast } from 'sonner'
import type { DocumentResponse } from '@/entities/document'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input } from '@/shared/ui'
import { useRegisterDraftDocumentMutation } from '../api/documentMutations'
import {
  registerDraftSchema,
  type RegisterDraftFormValues,
} from '../model/schemas'

interface RegisterDraftDialogProps {
  document: DocumentResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Registers a draft by allocating the next official number from the matching registry. */
export function RegisterDraftDialog({
  document,
  open,
  onOpenChange,
}: RegisterDraftDialogProps) {
  const mutation = useRegisterDraftDocumentMutation(document.documentId)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterDraftFormValues>({
    resolver: zodResolver(registerDraftSchema),
    defaultValues: { numberingYear: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      const registered = await mutation.mutateAsync({
        numberingYear: values.numberingYear.trim()
          ? Number(values.numberingYear)
          : null,
      })
      toast.success(
        registered.officialNumberText
          ? `تم تسجيل الوثيقة برقم ${registered.officialNumberText}.`
          : 'تمت تسجيل الوثيقة.',
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
      title="تسجيل المسودة برقم رسمي"
      description="سيُخصص الرقم التالي من سجل الترقيم المطابق لقسم الوثيقة ونوعها واتجاهها، وتنتقل الوثيقة إلى حالة «مسجل»."
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
          <Button type="submit" form="document-register-form" loading={mutation.isPending}>
            تسجيل الوثيقة
          </Button>
        </>
      }
    >
      <form id="document-register-form" onSubmit={onSubmit} noValidate>
        <FormField
          label="سنة الترقيم (اختياري)"
          error={errors.numberingYear?.message}
        >
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
          <p role="alert" className="mt-4 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
