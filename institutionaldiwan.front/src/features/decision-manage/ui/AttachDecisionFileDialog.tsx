import { Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDocumentFiles } from '@/entities/document-file'
import { toArabicErrorMessage } from '@/shared/api'
import { DOCUMENT_FILE_TYPE } from '@/shared/config'
import { useDraftForm, t } from '@/shared/lib'
import { Button, Dialog, FormField, Label, Select } from '@/shared/ui'
import { useAttachDecisionFileMutation } from '../api/decisionMutations'
import { attachDecisionFileSchema, type AttachDecisionFileFormValues } from '../model/schemas'

interface AttachDecisionFileDialogProps {
  documentId: string
  decisionId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Links an existing decision-type file on the same document. */
export function AttachDecisionFileDialog({
  documentId,
  decisionId,
  open,
  onOpenChange,
}: AttachDecisionFileDialogProps) {
  const mutation = useAttachDecisionFileMutation(documentId)
  const filesQuery = useDocumentFiles(documentId, { enabled: open })
  const options = (filesQuery.data ?? [])
    .filter((file) => file.fileTypeId === DOCUMENT_FILE_TYPE.Decision && !file.isArchived)
    .map((file) => ({ value: file.fileId, label: file.title }))

  const {
    control,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<AttachDecisionFileFormValues>(
    { formId: `attach-decision:${decisionId}`, enabled: open },
    { resolver: zodResolver(attachDecisionFileSchema), defaultValues: { documentFileId: '' } },
  )

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({ decisionId, documentFileId: values.documentFileId })
      toast.success(t('decisions.attach.toast'))
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
      title={t('decisions.attach.title')}
      description={t('decisions.attach.description')}
      size="md"
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
            <Label k="common.action.cancel" />
          </Button>
          <Button type="submit" form="attach-decision-form" disabled={mutation.isPending || options.length === 0}>
            {mutation.isPending ? t('decisions.attach.submitting') : <Label k="decisions.attach.submit" />}
          </Button>
        </>
      }
    >
      <form id="attach-decision-form" className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <FormField labelKey="decisions.attach.field-file" required error={errors.documentFileId?.message}>
          <Controller
            control={control}
            name="documentFileId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={options}
                placeholder={options.length === 0 ? t('decisions.attach.empty-placeholder') : t('decisions.attach.file-placeholder')}
                disabled={options.length === 0}
                invalid={Boolean(errors.documentFileId)}
              />
            )}
          />
        </FormField>
      </form>
    </Dialog>
  )
}
