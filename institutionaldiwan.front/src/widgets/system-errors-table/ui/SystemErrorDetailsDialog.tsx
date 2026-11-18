import type { ReactNode } from 'react'
import {
  SYSTEM_ERROR_KIND_LABEL_KEYS,
  SYSTEM_ERROR_SOURCE_LABEL_KEYS,
  useSystemError,
} from '@/entities/system-error'
import { UpdateSystemErrorForm } from '@/features/system-error-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { formatDateTime, t } from '@/shared/lib'
import { Dialog, Skeleton } from '@/shared/ui'

interface SystemErrorDetailsDialogProps {
  systemErrorId: string | null
  onClose: () => void
}

function Fact({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs text-muted">{label}</dt>
      <dd className="break-words text-sm text-foreground">{children}</dd>
    </div>
  )
}

function Latin({ children }: { children: ReactNode }) {
  return <span dir="ltr">{children || '—'}</span>
}

/** The latest occurrence of one error: what failed, where, the stack trace and the developer's notes. */
export function SystemErrorDetailsDialog({ systemErrorId, onClose }: SystemErrorDetailsDialogProps) {
  const query = useSystemError(systemErrorId)
  const error = query.data

  return (
    <Dialog
      open={systemErrorId !== null}
      onOpenChange={(next) => {
        if (!next) onClose()
      }}
      title={t('system-error.details.title')}
      size="lg"
    >
      {query.isLoading && <Skeleton className="h-64 w-full" />}
      {query.isError && (
        <p role="alert" className="text-sm text-danger">
          {toArabicErrorMessage(query.error)}
        </p>
      )}
      {error && (
        <div className="flex flex-col gap-4">
          <p dir="ltr" className="break-words text-start text-sm font-medium text-foreground">
            {error.summary.title}
          </p>
          <p dir="auto" className="break-words text-start text-sm text-muted">
            {error.summary.message}
          </p>

          <dl className="grid gap-3 sm:grid-cols-2">
            <Fact label={t('system-error.column.source')}>{t(SYSTEM_ERROR_SOURCE_LABEL_KEYS[error.summary.source])}</Fact>
            <Fact label={t('system-error.column.kind')}>{t(SYSTEM_ERROR_KIND_LABEL_KEYS[error.summary.kind])}</Fact>
            <Fact label={t('system-error.column.path')}>
              <Latin>
                {error.summary.httpMethod} {error.summary.path}
              </Latin>
            </Fact>
            <Fact label={t('system-error.details.http-code')}>
              <Latin>{error.summary.statusCode}</Latin>
            </Fact>
            <Fact label={t('system-error.details.occurrence-count')}>
              <Latin>{error.summary.occurrenceCount}</Latin>
            </Fact>
            <Fact label={t('system-error.column.last-username')}>
              <Latin>{error.summary.lastUsername}</Latin>
            </Fact>
            <Fact label={t('system-error.column.first-seen')}>
              <Latin>{formatDateTime(error.summary.firstSeenAt)}</Latin>
            </Fact>
            <Fact label={t('system-error.column.last-seen')}>
              <Latin>{formatDateTime(error.summary.lastSeenAt)}</Latin>
            </Fact>
            <Fact label={t('system-error.details.trace-id')}>
              <Latin>{error.traceId}</Latin>
            </Fact>
            <Fact label={t('system-error.details.browser')}>
              <Latin>{error.userAgent}</Latin>
            </Fact>
          </dl>

          {error.details && (
            <pre
              dir="ltr"
              className="max-h-80 overflow-auto rounded-lg border border-border bg-surface-muted p-3 text-start text-xs text-foreground"
            >
              {error.details}
            </pre>
          )}

          {/* Keyed so a refetched error reseeds the form. */}
          <UpdateSystemErrorForm key={`${error.summary.systemErrorId}-${error.summary.status}`} error={error} />
        </div>
      )}
    </Dialog>
  )
}
