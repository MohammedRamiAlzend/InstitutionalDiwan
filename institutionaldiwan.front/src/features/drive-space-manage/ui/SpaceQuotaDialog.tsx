import { Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { DriveSpaceResponse } from '@/entities/drive-space'
import { t, useDraftForm } from '@/shared/lib'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Label, Select } from '@/shared/ui'
import { useSetSpaceQuotaMutation } from '../api/driveSpaceMutations'
import {
  QUOTA_UNITS,
  quotaFormToBytes,
  spaceQuotaSchema,
  type SpaceQuotaFormValues,
} from '../model/schemas'

interface SpaceQuotaDialogProps {
  space: DriveSpaceResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

const UNIT_LABEL_KEY = {
  bytes: 'common.size.bytes',
  kb: 'common.size.kb',
  mb: 'common.size.mb',
  gb: 'common.size.gb',
} as const

/** ADMIN-only per-space quota override; an empty amount restores the institution default. */
export function SpaceQuotaDialog({ space, open, onOpenChange }: SpaceQuotaDialogProps) {
  const mutation = useSetSpaceQuotaMutation(space.spaceId)
  const {
    control,
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<SpaceQuotaFormValues>({ formId: `drive-space-quota:${space.spaceId}` }, {
    resolver: zodResolver(spaceQuotaSchema),
    defaultValues: {
      amount: space.quotaBytes !== null ? String(space.quotaBytes) : '',
      unit: 'bytes',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({ quotaBytes: quotaFormToBytes(values) })
      toast.success(t('drive.toast.quota-updated'))
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
      title={t('drive.dialog.space-quota-title')}
      description={space.name}
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
          <Button type="submit" form="drive-space-quota-form" loading={mutation.isPending}>
            <Label k="common.action.save" />
          </Button>
        </>
      }
    >
      <form
        id="drive-space-quota-form"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <FormField
          labelKey="drive.quota"
          hint={t('drive.dialog.space-quota-hint')}
          error={errors.amount?.message}
        >
          <div className="flex items-center gap-2">
            <Input
              autoFocus
              inputMode="numeric"
              dir="ltr"
              className="flex-1"
              invalid={Boolean(errors.amount)}
              {...register('amount')}
            />
            <Controller
              control={control}
              name="unit"
              render={({ field }) => (
                <Select
                  className="w-28"
                  value={field.value}
                  onValueChange={field.onChange}
                  aria-label={t('drive.quota')}
                  options={QUOTA_UNITS.map((unit) => ({
                    value: unit,
                    label: t(UNIT_LABEL_KEY[unit]),
                  }))}
                />
              )}
            />
          </div>
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