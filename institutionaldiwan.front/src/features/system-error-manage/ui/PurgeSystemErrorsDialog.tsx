import { useState } from 'react'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { formatNumber, resolveLabel, t, useLabelsStore } from '@/shared/lib'
import { Button, ConfirmDialog, Dialog, FormField, Input, Label, Select } from '@/shared/ui'
import { usePurgeSystemErrorsMutation } from '../api/systemErrorMutations'
import { purgeSystemErrorsSchema } from '../model/schemas'

const CUSTOM = 'custom'

interface PurgeSystemErrorsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Manual cleanup: the developer picks the age, then confirms a deletion that cannot be undone. */
export function PurgeSystemErrorsDialog({ open, onOpenChange }: PurgeSystemErrorsDialogProps) {
  const mutation = usePurgeSystemErrorsMutation()
  const overrides = useLabelsStore((state) => state.overrides)
  const [period, setPeriod] = useState('90')
  const [customDays, setCustomDays] = useState('')
  const [error, setError] = useState<string>()
  const [confirmDays, setConfirmDays] = useState<number | null>(null)

  const periodOptions = [
    { value: '30', label: resolveLabel('system-error.purge.period-30', overrides) },
    { value: '60', label: resolveLabel('system-error.purge.period-60', overrides) },
    { value: '90', label: resolveLabel('system-error.purge.period-90', overrides) },
    { value: '180', label: resolveLabel('system-error.purge.period-180', overrides) },
    { value: CUSTOM, label: resolveLabel('system-error.purge.period-custom', overrides) },
  ]

  const review = () => {
    const parsed = purgeSystemErrorsSchema.safeParse({ days: period === CUSTOM ? customDays : period })
    if (!parsed.success) {
      setError(parsed.error.issues[0]?.message)
      return
    }
    setError(undefined)
    setConfirmDays(Number(parsed.data.days))
  }

  const purge = () => {
    if (confirmDays === null) return
    mutation.mutate(confirmDays, {
      onSuccess: (deleted) => {
        toast.success(t('system-error.purge.toast-deleted', { count: formatNumber(deleted) }))
        setConfirmDays(null)
        onOpenChange(false)
      },
      onError: () => setConfirmDays(null),
    })
  }

  return (
    <>
      <Dialog
        open={open}
        onOpenChange={onOpenChange}
        title={t('system-error.menu.purge')}
        description={t('system-error.purge.description')}
        footer={
          <>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              <Label k="system-error.purge.back" />
            </Button>
            <Button type="button" variant="destructive" onClick={review}>
              <Label k="system-error.purge.continue" />
            </Button>
          </>
        }
      >
        <div className="flex flex-col gap-4">
          <FormField labelKey="system-error.purge.period" required>
            <Select aria-label={t('system-error.purge.period-aria')} value={period} onValueChange={setPeriod} options={periodOptions} />
          </FormField>
          {period === CUSTOM && (
            <FormField labelKey="system-error.purge.days" required error={error}>
              <Input
                dir="ltr"
                inputMode="numeric"
                value={customDays}
                invalid={Boolean(error)}
                onChange={(event) => setCustomDays(event.target.value)}
              />
            </FormField>
          )}
          {mutation.error && (
            <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
              {toArabicErrorMessage(mutation.error)}
            </p>
          )}
        </div>
      </Dialog>

      <ConfirmDialog
        open={confirmDays !== null}
        onOpenChange={(next) => {
          if (!next) setConfirmDays(null)
        }}
        title={t('system-error.purge.confirm-title')}
        description={t('system-error.purge.confirm-description', { days: confirmDays ?? 0 })}
        confirmLabel={t('system-error.purge.confirm')}
        destructive
        loading={mutation.isPending}
        onConfirm={purge}
      />
    </>
  )
}