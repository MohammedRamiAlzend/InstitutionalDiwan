import { useState } from 'react'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { formatNumber } from '@/shared/lib'
import { Button, ConfirmDialog, Dialog, FormField, Input, Select } from '@/shared/ui'
import { usePurgeSystemErrorsMutation } from '../api/systemErrorMutations'
import { purgeSystemErrorsSchema } from '../model/schemas'

const CUSTOM = 'custom'

const PERIOD_OPTIONS = [
  { value: '30', label: 'أقدم من 30 يومًا' },
  { value: '60', label: 'أقدم من 60 يومًا' },
  { value: '90', label: 'أقدم من 90 يومًا' },
  { value: '180', label: 'أقدم من 180 يومًا' },
  { value: CUSTOM, label: 'مدة أحددها' },
]

interface PurgeSystemErrorsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Manual cleanup: the developer picks the age, then confirms a deletion that cannot be undone. */
export function PurgeSystemErrorsDialog({ open, onOpenChange }: PurgeSystemErrorsDialogProps) {
  const mutation = usePurgeSystemErrorsMutation()
  const [period, setPeriod] = useState('90')
  const [customDays, setCustomDays] = useState('')
  const [error, setError] = useState<string>()
  const [confirmDays, setConfirmDays] = useState<number | null>(null)

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
        toast.success(`حُذف ${formatNumber(deleted)} خطأ.`)
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
        title="حذف الأخطاء القديمة"
        description="تُحذف الأخطاء التي لم تظهر منذ المدة المختارة، مهما كانت حالتها."
        footer={
          <>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              تراجع
            </Button>
            <Button type="button" variant="destructive" onClick={review}>
              متابعة
            </Button>
          </>
        }
      >
        <div className="flex flex-col gap-4">
          <FormField label="المدة" required>
            <Select aria-label="مدة الحذف" value={period} onValueChange={setPeriod} options={PERIOD_OPTIONS} />
          </FormField>
          {period === CUSTOM && (
            <FormField label="عدد الأيام" required error={error}>
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
        title="تأكيد الحذف النهائي"
        description={`ستُحذف نهائيًا كل الأخطاء التي لم تظهر منذ ${confirmDays ?? 0} يومًا، ولا يمكن التراجع عن ذلك.`}
        confirmLabel="حذف نهائي"
        destructive
        loading={mutation.isPending}
        onConfirm={purge}
      />
    </>
  )
}
