import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { DocumentResponse } from '@/entities/document'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Select, Textarea } from '@/shared/ui'
import { useUpdateDraftDocumentMutation } from '../api/documentMutations'
import { editDraftSchema, type EditDraftFormValues } from '../model/schemas'

interface EditDraftDialogProps {
  document: DocumentResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Edits a draft's title, summary, confidentiality, and priority (type/direction stay fixed). */
export function EditDraftDialog({ document, open, onOpenChange }: EditDraftDialogProps) {
  const mutation = useUpdateDraftDocumentMutation(document.documentId)
  const confidentialityQuery = useLookupEntries(LOOKUP_SLUGS.DocumentConfidentialityLevels)
  const priorityQuery = useLookupEntries(LOOKUP_SLUGS.DocumentPriorityLevels)

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<EditDraftFormValues>({
    resolver: zodResolver(editDraftSchema),
    defaultValues: {
      title: document.title,
      subjectSummary: document.subjectSummary ?? '',
      confidentialityLevelId: String(document.confidentialityLevelId),
      priorityLevelId: String(document.priorityLevelId),
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        title: values.title.trim(),
        subjectSummary: values.subjectSummary.trim() || null,
        confidentialityLevelId: Number(values.confidentialityLevelId),
        priorityLevelId: Number(values.priorityLevelId),
      })
      toast.success('تم تحديث بيانات المسودة.')
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
      description="يمكن تعديل العنوان والملخص والسرية والأولوية فقط، ويُقفل التعديل بعد تسجيل الوثيقة."
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

        <FormField
          label="مستوى السرية"
          required
          error={errors.confidentialityLevelId?.message}
        >
          <Controller
            control={control}
            name="confidentialityLevelId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={(confidentialityQuery.data ?? []).map((entry) => ({
                  value: String(entry.id),
                  label: entry.descAr,
                }))}
                disabled={mutation.isPending || confidentialityQuery.isLoading}
                invalid={Boolean(errors.confidentialityLevelId)}
                aria-label="مستوى السرية"
              />
            )}
          />
        </FormField>

        <FormField label="درجة الأولوية" required error={errors.priorityLevelId?.message}>
          <Controller
            control={control}
            name="priorityLevelId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={(priorityQuery.data ?? []).map((entry) => ({
                  value: String(entry.id),
                  label: entry.descAr,
                }))}
                disabled={mutation.isPending || priorityQuery.isLoading}
                invalid={Boolean(errors.priorityLevelId)}
                aria-label="درجة الأولوية"
              />
            )}
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
