import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { DriveSpaceResponse } from '@/entities/drive-space'
import { t, useDraftForm } from '@/shared/lib'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Label } from '@/shared/ui'
import { useRenameSpaceMutation } from '../api/driveSpaceMutations'
import { spaceNameSchema, type SpaceNameFormValues } from '../model/schemas'

interface RenameSpaceDialogProps {
  space: DriveSpaceResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Renames a space the caller owns (or any space for ADMIN). */
export function RenameSpaceDialog({ space, open, onOpenChange }: RenameSpaceDialogProps) {
  const mutation = useRenameSpaceMutation(space.spaceId)
  const {
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<SpaceNameFormValues>({ formId: `drive-rename-space:${space.spaceId}` }, {
    resolver: zodResolver(spaceNameSchema),
    defaultValues: { name: space.name },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({ name: values.name.trim() })
      toast.success(t('drive.toast.space-renamed'))
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
      title={t('drive.dialog.rename-space-title')}
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
          <Button type="submit" form="drive-rename-space-form" loading={mutation.isPending}>
            <Label k="common.action.save" />
          </Button>
        </>
      }
    >
      <form
        id="drive-rename-space-form"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <FormField labelKey="drive.dialog.space-name" required error={errors.name?.message}>
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