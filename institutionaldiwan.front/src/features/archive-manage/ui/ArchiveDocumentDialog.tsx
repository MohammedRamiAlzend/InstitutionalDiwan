import { useMemo } from 'react'
import { Controller } from 'react-hook-form'
import { useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { LOOKUP_SLUGS, useLookupEntries, usePreselectLookupDefault } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Select, Textarea } from '@/shared/ui'
import { useArchiveDocumentMutation } from '../api/archiveMutations'
import { archiveDocumentSchema, type ArchiveDocumentFormValues } from '../model/schemas'

interface ArchiveDocumentDialogProps {
  documentId: string
  documentTitle: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Archives a registered document at a chosen location; the document is frozen until unarchived. */
export function ArchiveDocumentDialog({
  documentId,
  documentTitle,
  open,
  onOpenChange,
}: ArchiveDocumentDialogProps) {
  const mutation = useArchiveDocumentMutation(documentId)
  const locationsQuery = useLookupEntries(LOOKUP_SLUGS.ArchiveLocations)

  const locationOptions = useMemo(
    () => (locationsQuery.data ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    [locationsQuery.data],
  )

  const {
    control,
    getValues,
    setValue,
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<ArchiveDocumentFormValues>({ formId: `archive-document:${documentId}` }, {
    resolver: zodResolver(archiveDocumentSchema),
    defaultValues: { archiveLocationId: '', reason: '' },
  })

  usePreselectLookupDefault(locationsQuery.data, getValues('archiveLocationId'), (value) => setValue('archiveLocationId', value))

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        archiveLocationId: Number(values.archiveLocationId),
        reason: values.reason.trim() || undefined,
      })
      toast.success('تمت أرشفة الوثيقة.')
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
      title="أرشفة الوثيقة"
      description={`ستُؤرشف «${documentTitle}» ولن تقبل إضافة أشخاص أو مرفقات أو أرقام حتى تُلغى أرشفتها. القراءة والتنزيل يبقيان متاحين.`}
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
          <Button type="submit" form="archive-document-form" loading={mutation.isPending}>
            أرشفة
          </Button>
        </>
      }
    >
      <form id="archive-document-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField
          label="موقع الأرشفة"
          required
          error={errors.archiveLocationId?.message}
          guideId="archive.location"
        >
          <Controller
            control={control}
            name="archiveLocationId"
            render={({ field }) => (
              <Select
                aria-label="موقع الأرشفة"
                value={field.value}
                onValueChange={field.onChange}
                options={locationOptions}
                placeholder="اختر موقع الأرشفة"
                invalid={Boolean(errors.archiveLocationId)}
              />
            )}
          />
        </FormField>

        <FormField label="السبب" error={errors.reason?.message}>
          <Textarea
            rows={3}
            placeholder="اختياري، مثال: انتهت معالجة المعاملة."
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
