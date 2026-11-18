import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Hash } from 'lucide-react'
import type { AuditIntegrityCheckResponse } from '@/entities/audit-log'
import { toArabicErrorMessage } from '@/shared/api'
import { formatNumber, t } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Label } from '@/shared/ui'
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
      title={t('audit.integrity.dialog.title')}
      description={t('audit.integrity.dialog.description')}
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            <Label k="common.action.close" />
          </Button>
          <Button type="submit" form="integrity-check-form" loading={mutation.isPending}>
            <Label k="audit.integrity.dialog.run" />
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
          <FormField labelKey="audit.integrity.field.start" error={errors.startAuditLogId?.message}>
            <Input
              type="number"
              inputMode="numeric"
              dir="ltr"
              min={1}
              placeholder={t('audit.integrity.field.start-placeholder')}
              startIcon={<Hash className="size-4" aria-hidden />}
              invalid={Boolean(errors.startAuditLogId)}
              {...register('startAuditLogId')}
            />
          </FormField>

          <FormField labelKey="audit.integrity.field.end" error={errors.endAuditLogId?.message}>
            <Input
              type="number"
              inputMode="numeric"
              dir="ltr"
              min={1}
              placeholder={t('audit.integrity.field.end-placeholder')}
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
              {t('audit.integrity.result.valid-prefix')}{' '}
              {formatNumber(result.endAuditLogId - result.startAuditLogId + 1)} {t('audit.integrity.result.valid-middle')}{' '}
              <span dir="ltr">#{formatNumber(result.startAuditLogId)}</span> {t('audit.integrity.result.valid-to')}{' '}
              <span dir="ltr">#{formatNumber(result.endAuditLogId)}</span>.
            </div>
          ) : (
            <div
              role="alert"
              className="flex flex-col gap-1 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
            >
              <p className="font-medium">{t('audit.integrity.result.breached-title')}</p>
              {result.firstCorruptedLogId !== null && (
                <p>
                  {t('audit.integrity.result.first-corrupted')}{' '}
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
              <p className="text-xs">{t('audit.integrity.result.saved')}</p>
            </div>
          ))}
      </form>
    </Dialog>
  )
}
