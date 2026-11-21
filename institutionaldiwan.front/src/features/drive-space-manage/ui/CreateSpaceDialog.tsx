import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { t, useDraftForm } from '@/shared/lib'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Label } from '@/shared/ui'
import { useCreateSpaceMutation } from '../api/driveSpaceMutations'
import { spaceNameSchema, type SpaceNameFormValues } from '../model/schemas'

interface CreateSpaceDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Creates a shared space owned by the caller; the institution default quota applies. */
export function CreateSpaceDialog({ open, onOpenChange }: CreateSpaceDialogProps) {
  const mutation = useCreateSpaceMutation()
  const {
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<SpaceNameFormValues>({ formId: 'drive-create-space' }, {
    resolver: zodResolver(spaceNameSchema),
    defaultValues: { name: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({ name: values.name.trim() })
      toast.success(t('drive.toast.space-created'))
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
      title={t('drive.dialog.new-space-title')}
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
          <Button type="submit" form="drive-create-space-form" loading={mutation.isPending}>
            <Label k="common.action.save" />
          </Button>
        </>
      }
    >
      <form
        id="drive-create-space-form"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <FormField labelKey="drive.dialog.space-name" required error={errors.name?.message}>
          <Input
            autoFocus
            placeholder={t('drive.dialog.space-name-placeholder')}
            invalid={Boolean(errors.name)}
            {...register('name')}
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