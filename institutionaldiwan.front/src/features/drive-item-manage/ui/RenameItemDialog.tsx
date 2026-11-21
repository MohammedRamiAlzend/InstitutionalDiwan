import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { DriveItemResponse } from '@/entities/drive-item'
import { t, useDraftForm } from '@/shared/lib'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Label } from '@/shared/ui'
import { useRenameItemMutation } from '../api/driveItemMutations'
import { itemNameSchema, type ItemNameFormValues } from '../model/schemas'

interface RenameItemDialogProps {
  item: DriveItemResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Renames a folder or file (Editor access); the current name prefills the field. */
export function RenameItemDialog({ item, open, onOpenChange }: RenameItemDialogProps) {
  const mutation = useRenameItemMutation(item.itemId)
  const formId = `drive-rename-item:${item.itemId}`
  const {
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<ItemNameFormValues>({ formId }, {
    resolver: zodResolver(itemNameSchema),
    defaultValues: { name: item.name },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({ name: values.name.trim() })
      toast.success(t('drive.toast.renamed'))
      draft.discard()
      onOpenChange(false)
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
      title={t('drive.dialog.rename-title')}
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
        <FormField labelKey="drive.dialog.item-name" required error={errors.name?.message}>
          <Input autoFocus invalid={Boolean(errors.name)} {...register('name')} />
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