import type { ReactNode } from 'react'
import {
  SYSTEM_ERROR_KIND_LABELS,
  SYSTEM_ERROR_SOURCE_LABELS,
  useSystemError,
} from '@/entities/system-error'
import { UpdateSystemErrorForm } from '@/features/system-error-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { formatDateTime } from '@/shared/lib'
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
      title="تفاصيل الخطأ"
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
            <Fact label="المصدر">{SYSTEM_ERROR_SOURCE_LABELS[error.summary.source]}</Fact>
            <Fact label="النوع">{SYSTEM_ERROR_KIND_LABELS[error.summary.kind]}</Fact>
            <Fact label="المسار">
              <Latin>
                {error.summary.httpMethod} {error.summary.path}
              </Latin>
            </Fact>
            <Fact label="رمز HTTP">
              <Latin>{error.summary.statusCode}</Latin>
            </Fact>
            <Fact label="عدد المرات">
              <Latin>{error.summary.occurrenceCount}</Latin>
            </Fact>
            <Fact label="آخر مستخدم">
              <Latin>{error.summary.lastUsername}</Latin>
            </Fact>
            <Fact label="أول ظهور">
              <Latin>{formatDateTime(error.summary.firstSeenAt)}</Latin>
            </Fact>
            <Fact label="آخر ظهور">
              <Latin>{formatDateTime(error.summary.lastSeenAt)}</Latin>
            </Fact>
            <Fact label="معرّف التتبع">
              <Latin>{error.traceId}</Latin>
            </Fact>
            <Fact label="المتصفح">
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
