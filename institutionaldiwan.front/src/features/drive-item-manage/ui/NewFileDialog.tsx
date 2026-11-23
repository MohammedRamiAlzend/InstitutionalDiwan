import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { DriveItemResponse } from '@/entities/drive-item'
import { toArabicErrorMessage } from '@/shared/api'
import { t, useDraftForm } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Label, Select, type SelectOption } from '@/shared/ui'
import { useCreateDriveFileMutation } from '../api/driveItemMutations'
import { itemNameSchema, type ItemNameFormValues } from '../model/schemas'

interface NewFileDialogProps {
  spaceId: string
  parentId: string | null
  open: boolean
  onOpenChange: (open: boolean) => void
  /** The created file, so the browser can open it in its editor right away. */
  onCreated?: (item: DriveItemResponse) => void
}

/** The four templates the server can build; the extension is appended to the typed base name. */
const FILE_TYPES: readonly SelectOption[] = [
  { value: 'docx', label: t('drive.file-type.word') },
  { value: 'xlsx', label: t('drive.file-type.sheet') },
  { value: 'txt', label: t('drive.file-type.text') },
  { value: 'md', label: t('drive.file-type.markdown') },
]

const KNOWN_EXTENSION = /\.(docx|xlsx|txt|md)$/i

/**
 * Creates an empty Word, Excel, plain-text or Markdown file in the open folder. The server writes a
 * minimal valid template as version 1, so the created file opens in the OnlyOffice editor (docx/xlsx)
 * or the in-app text editor (txt/md) without a separate upload.
 */
export function NewFileDialog({
  spaceId,
  parentId,
  open,
  onOpenChange,
  onCreated,
}: NewFileDialogProps) {
  const mutation = useCreateDriveFileMutation()
  const [fileType, setFileType] = useState('docx')
  const formId = `drive-new-file:${spaceId}:${parentId ?? 'root'}`
  const {
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<ItemNameFormValues>({ formId }, {
    resolver: zodResolver(itemNameSchema),
    defaultValues: { name: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      const baseName = values.name.trim().replace(KNOWN_EXTENSION, '')
      const created = await mutation.mutateAsync({
        spaceId,
        parentId,
        fileName: `${baseName}.${fileType}`,
      })
      toast.success(t('drive.toast.file-created'))
      draft.discard()
      onOpenChange(false)
      onCreated?.(created)
    } catch {
      // Rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && mutation.isPending) return
        onOpenChange(nextOpen)
      }}
      title={t('drive.dialog.new-file-title')}
      description={t('drive.dialog.new-file-hint')}
      size="sm"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            <Label k="common.action.cancel" />
          </Button>
          <Button type="submit" form={`${formId}-form`} loading={mutation.isPending}>
            <Label k="common.action.save" />
          </Button>
        </>
      }
    >
      <form id={`${formId}-form`} onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField labelKey="drive.dialog.new-file-name" required error={errors.name?.message}>
          <Input autoFocus invalid={Boolean(errors.name)} {...register('name')} />
        </FormField>

        <FormField labelKey="drive.dialog.new-file-type" required>
          <Select
            value={fileType}
            onValueChange={setFileType}
            options={FILE_TYPES}
            disabled={mutation.isPending}
            aria-label={t('drive.dialog.new-file-type')}
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
