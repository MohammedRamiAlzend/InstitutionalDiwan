import { useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { DocumentResponse } from '@/entities/document'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Textarea } from '@/shared/ui'
import { useUpdateDraftDocumentMutation } from '../api/documentMutations'
import { editDraftSchema, type EditDraftFormValues } from '../model/schemas'

interface EditDraftDialogProps {
  document: DocumentResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Edits a draft's title and summary (the document type stays fixed). */
export function EditDraftDialog({ document, open, onOpenChange }: EditDraftDialogProps) {
  const mutation = useUpdateDraftDocumentMutation(document.documentId)

  const {
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<EditDraftFormValues>({ formId: `edit-draft:${document.documentId}` }, {
    resolver: zodResolver(editDraftSchema),
    defaultValues: {
      title: document.title,
      subjectSummary: document.subjectSummary ?? '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        title: values.title.trim(),
        subjectSummary: values.subjectSummary.trim() || null,
      })
      toast.success('تم تحديث بيانات المسودة.')
      draft.discard()
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="تعديل بيانات المسودة"
      description="يمكن تعديل العنوان والملخص فقط، ويُقفل التعديل بعد تسجيل الوثيقة."
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
          <Button type="submit" form="document-edit-form" loading={mutation.isPending}>
            حفظ التعديلات
          </Button>
        </>
      }
    >
      <form
        id="document-edit-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <FormField
          label="عنوان الوثيقة"
          required
          error={errors.title?.message}
          className="sm:col-span-2"
        >
          <Input invalid={Boolean(errors.title)} {...register('title')} />
        </FormField>

        <FormField
          label="ملخص الموضوع"
          error={errors.subjectSummary?.message}
          className="sm:col-span-2"
        >
          <Textarea
            rows={3}
            invalid={Boolean(errors.subjectSummary)}
            {...register('subjectSummary')}
          />
        </FormField>

        {mutation.error && (
          <p
            role="alert"
            className="sm:col-span-2 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
