import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Hash } from 'lucide-react'
import type { AuditIntegrityCheckResponse } from '@/entities/audit-log'
import { toArabicErrorMessage } from '@/shared/api'
import { formatNumber } from '@/shared/lib'
import { Button, Dialog, FormField, Input } from '@/shared/ui'
import { useRunIntegrityCheckMutation } from '../api/runIntegrityCheck'
import { integrityRangeSchema, type IntegrityRangeFormValues } from '../model/integrity.schema'

interface RunIntegrityCheckDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Prefills the lower bound when the action starts from one row. */
  startAuditLogId?: number
  /** Prefills the upper bound when re-running a stored report's range. */
  endAuditLogId?: number
}

/**
 * Walks the chain and recomputes every hash. A failure is a successful HTTP
 * 200 response with `isValid: false`, so it renders as a report, not an error.
 */
export function RunIntegrityCheckDialog({
  open,
  onOpenChange,
  startAuditLogId,
  endAuditLogId,
}: RunIntegrityCheckDialogProps) {
  const mutation = useRunIntegrityCheckMutation()
  const [result, setResult] = useState<AuditIntegrityCheckResponse | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<IntegrityRangeFormValues>({
    resolver: zodResolver(integrityRangeSchema),
    defaultValues: {
      startAuditLogId: startAuditLogId ? String(startAuditLogId) : '',
      endAuditLogId: endAuditLogId ? String(endAuditLogId) : '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      const report = await mutation.mutateAsync({
        startAuditLogId: values.startAuditLogId ? Number(values.startAuditLogId) : null,
        endAuditLogId: values.endAuditLogId ? Number(values.endAuditLogId) : null,
      })
      setResult(report)
    } catch {
      // The mutation error is rendered in the banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="فحص سلامة سلسلة التدقيق"
      description="يعيد الفحص حساب كل تجزئة ويتحقق من الربط بين السجلات. يُفحص كامل السلسلة عند ترك الحقلين فارغين."
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            إغلاق
          </Button>
          <Button type="submit" form="integrity-check-form" loading={mutation.isPending}>
            تشغيل الفحص
          </Button>
        </>
      }
    >
      <form
        id="integrity-check-form"
        onSubmit={onSubmit}
        noValidate
        className="flex flex-col gap-4"
      >
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FormField label="من معرّف السجل (اختياري)" error={errors.startAuditLogId?.message}>
            <Input
              type="number"
              inputMode="numeric"
              dir="ltr"
              min={1}
              placeholder="بداية السلسلة"
              startIcon={<Hash className="size-4" aria-hidden />}
              invalid={Boolean(errors.startAuditLogId)}
              {...register('startAuditLogId')}
            />
          </FormField>

          <FormField label="إلى معرّف السجل (اختياري)" error={errors.endAuditLogId?.message}>
            <Input
              type="number"
              inputMode="numeric"
              dir="ltr"
              min={1}
              placeholder="نهاية السلسلة"
              startIcon={<Hash className="size-4" aria-hidden />}
              invalid={Boolean(errors.endAuditLogId)}
              {...register('endAuditLogId')}
            />
          </FormField>
        </div>

        {mutation.error && (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}

        {result &&
          (result.isValid ? (
            <div
              role="status"
              className="rounded-lg border border-success/40 bg-success/10 px-3 py-2 text-sm text-success"
            >
              السلسلة سليمة: فُحص{' '}
              {formatNumber(result.endAuditLogId - result.startAuditLogId + 1)} سجلًا من{' '}
              <span dir="ltr">#{formatNumber(result.startAuditLogId)}</span> إلى{' '}
              <span dir="ltr">#{formatNumber(result.endAuditLogId)}</span>.
            </div>
          ) : (
            <div
              role="alert"
              className="flex flex-col gap-1 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
            >
              <p className="font-medium">اكُتشفت تجاوزات في سلسلة التدقيق.</p>
              {result.firstCorruptedLogId !== null && (
                <p>
                  أول سجل متأثر:{' '}
                  <span dir="ltr" className="font-mono">
                    #{formatNumber(result.firstCorruptedLogId)}
                  </span>
                </p>
              )}
              {result.failureDetails && (
                <p dir="ltr" className="text-start text-xs opacity-90">
                  {result.failureDetails}
                </p>
              )}
              <p className="text-xs">حُفظ التقرير في قائمة تقارير السلامة.</p>
            </div>
          ))}
      </form>
    </Dialog>
  )
}
