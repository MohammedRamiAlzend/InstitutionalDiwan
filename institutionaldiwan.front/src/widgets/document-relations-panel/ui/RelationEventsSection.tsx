import { useSearchParams } from 'react-router-dom'
import { useRelationEvents } from '@/entities/document-relation'
import { RelationTypeBadge } from '@/entities/document-relation'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { formatDateTime, formatShortId } from '@/shared/lib'
import { Button, Skeleton } from '@/shared/ui'
import { UserLabel } from '@/entities/user'

const PAGE_SIZE = 10
const PAGE_PARAM = 'eventsPage'

function readPage(raw: string | null): number {
  const parsed = Number.parseInt(raw ?? '', 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 1
}

/** The permanent relation history, newest first; its page lives in the URL beside the tab. */
export function RelationEventsSection({ documentId }: { documentId: string }) {
  const [searchParams, setSearchParams] = useSearchParams()
  const page = readPage(searchParams.get(PAGE_PARAM))
  const eventsQuery = useRelationEvents(documentId, page, PAGE_SIZE)

  const setPage = (next: number) => {
    setSearchParams(
      (current) => {
        const params = new URLSearchParams(current)
        if (next <= 1) params.delete(PAGE_PARAM)
        else params.set(PAGE_PARAM, String(next))
        return params
      },
      { replace: true },
    )
  }

  if (eventsQuery.isLoading) return <Skeleton className="h-20 w-full" />

  if (eventsQuery.isError) {
    return (
      <div className="flex flex-col items-center gap-3 rounded-xl border border-border bg-surface px-6 py-8 text-center">
        <p role="alert" className="text-sm font-medium text-danger">
          {toArabicErrorMessage(eventsQuery.error)}
        </p>
        <Button type="button" variant="outline" onClick={() => void eventsQuery.refetch()}>
          إعادة المحاولة
        </Button>
      </div>
    )
  }

  const result = eventsQuery.data
  const events = result?.items ?? []

  if (events.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-border bg-surface px-6 py-8 text-center">
        <p className="text-sm font-medium text-foreground">لا سجل للروابط بعد.</p>
        <p className="mt-1 text-xs text-muted">تظهر هنا عمليات إضافة الروابط وإزالتها عند حدوثها.</p>
      </div>
    )
  }

  const totalPages = Math.max(1, Math.ceil((result?.totalCount ?? 0) / PAGE_SIZE))

  return (
    <div className="flex flex-col gap-3">
      <ol className="flex flex-col gap-3">
        {events.map((event) => (
          <li
            key={event.relationEventId}
            className="flex flex-col gap-2 rounded-lg border border-border bg-background px-4 py-3"
          >
            <div className="flex flex-wrap items-center gap-2">
              <LookupLabel
                lookup={LOOKUP_SLUGS.RelationActionTypes}
                id={event.actionTypeId}
                className="text-sm font-medium"
              />
              <RelationTypeBadge relationTypeId={event.relationTypeId} />
              <span dir="ltr" className="ms-auto whitespace-nowrap text-xs text-muted">
                {formatDateTime(event.occurredAt)}
              </span>
            </div>
            <dl className="grid gap-x-6 gap-y-1 text-sm sm:grid-cols-2">
              <div className="flex items-baseline gap-1.5">
                <dt className="text-muted">المصدر ← الهدف:</dt>
                <dd dir="ltr" className="font-mono text-xs">
                  {formatShortId(event.sourceDocumentId)} → {formatShortId(event.targetDocumentId)}
                </dd>
              </div>
              <div className="flex items-baseline gap-1.5">
                <dt className="text-muted">المنفّذ:</dt>
                <dd className="text-xs">
                  <UserLabel userId={event.actorUserId} />
                </dd>
              </div>
              {event.reason && (
                <div className="flex items-baseline gap-1.5 sm:col-span-2">
                  <dt className="text-muted">السبب:</dt>
                  <dd className="whitespace-pre-line">{event.reason}</dd>
                </div>
              )}
            </dl>
          </li>
        ))}
      </ol>

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-3">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage(page - 1)}
          >
            السابق
          </Button>
          <span className="text-sm text-muted">
            {page} / {totalPages}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage(page + 1)}
          >
            التالي
          </Button>
        </div>
      )}
    </div>
  )
}
