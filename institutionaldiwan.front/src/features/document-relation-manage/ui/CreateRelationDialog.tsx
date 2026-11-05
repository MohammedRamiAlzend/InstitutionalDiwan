import { useMemo, useState } from 'react'
import { Controller } from 'react-hook-form'
import { useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { DocumentResponse } from '@/entities/document'
import { LOOKUP_SLUGS, useLookupEntries, usePreselectLookupDefault } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Select, Textarea } from '@/shared/ui'
import { useCreateRelationMutation } from '../api/relationMutations'
import { createRelationSchema, type CreateRelationFormValues } from '../model/schemas'
import { DocumentPicker } from './DocumentPicker'

interface CreateRelationDialogProps {
  documentId: string
  documentTitle: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Links the current document to another one; «يلغي» is informational and changes no status. */
export function CreateRelationDialog({
  documentId,
  documentTitle,
  open,
  onOpenChange,
}: CreateRelationDialogProps) {
  const mutation = useCreateRelationMutation(documentId)
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentRelationTypes)
  const [target, setTarget] = useState<DocumentResponse | null>(null)

  const typeOptions = useMemo(
    () => (typesQuery.data ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    [typesQuery.data],
  )

  const {
    control,
    getValues,
    register,
    setValue,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<CreateRelationFormValues>({ formId: `create-relation:${documentId}` }, {
    resolver: zodResolver(createRelationSchema),
    defaultValues: { targetDocumentId: '', relationTypeId: '', notes: '' },
  })

  usePreselectLookupDefault(typesQuery.data, getValues('relationTypeId'), (value) => setValue('relationTypeId', value))

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        targetDocumentId: values.targetDocumentId,
        relationTypeId: Number(values.relationTypeId),
        notes: values.notes.trim() || undefined,
      })
      toast.success('تمت إضافة الرابط.')
      draft.discard()
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && mutation.isPending) return
        onOpenChange(nextOpen)
      }}
      title="إضافة رابط"
      description={`اربط «${documentTitle}» بوثيقة أخرى. الرابط يظهر في الوثيقتين، وحذفه يُسجَّل بسبب في سجل الروابط.`}
      size="md"
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
          <Button type="submit" form="create-relation-form" loading={mutation.isPending}>
            إضافة الرابط
          </Button>
        </>
      }
    >
      <form id="create-relation-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField
          label="الوثيقة المراد ربطها"
          required
          error={errors.targetDocumentId?.message}
          guideId="document-details.relations-target"
        >
          <DocumentPicker
            excludeDocumentId={documentId}
            selected={target}
            invalid={Boolean(errors.targetDocumentId)}
            onSelect={(document) => {
              setTarget(document)
              setValue('targetDocumentId', document?.documentId ?? '', { shouldValidate: true })
            }}
          />
        </FormField>

        <FormField
          label="نوع الرابط"
          required
          error={errors.relationTypeId?.message}
          guideId="document-details.relations-types"
        >
          <Controller
            control={control}
            name="relationTypeId"
            render={({ field }) => (
              <Select
                aria-label="نوع الرابط"
                value={field.value}
                onValueChange={field.onChange}
                options={typeOptions}
                placeholder="اختر نوع الرابط"
                invalid={Boolean(errors.relationTypeId)}
              />
            )}
          />
        </FormField>

        <FormField label="ملاحظات" error={errors.notes?.message}>
          <Textarea
            rows={3}
            placeholder="اختياري، مثال: جواب على طلب سابق."
            invalid={Boolean(errors.notes)}
            {...register('notes')}
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
