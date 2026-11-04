import { ArchiveEventTypeBadge, useDocumentArchiveEvents } from '@/entities/archive'
import { DocumentStatusBadge } from '@/entities/document'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { formatDateTime } from '@/shared/lib'
import { Button, Skeleton } from '@/shared/ui'
import { UserLabel } from '@/entities/user'

interface DocumentArchiveTabProps {
  documentId: string
}

/** The document's archive history, newest first: who archived or restored it, where and why. */
export function DocumentArchiveTab({ documentId }: DocumentArchiveTabProps) {
  const eventsQuery = useDocumentArchiveEvents(documentId)

  if (eventsQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3">
        <Skeleton className="h-20 w-full" />
        <Skeleton className="h-20 w-full" />
      </div>
    )
  }

  if (eventsQuery.isError) {
    return (
      <div className="flex flex-col items-center gap-3 rounded-xl border border-border bg-surface px-6 py-10 text-center">
        <p role="alert" className="text-sm font-medium text-danger">
          {toArabicErrorMessage(eventsQuery.error)}
        </p>
        <Button type="button" variant="outline" onClick={() => void eventsQuery.refetch()}>
          إعادة المحاولة
        </Button>
      </div>
    )
  }

  const events = eventsQuery.data ?? []

  if (events.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-border bg-surface px-6 py-10 text-center">
        <p className="text-sm font-medium text-foreground">لم تُؤرشف هذه الوثيقة من قبل.</p>
        <p className="mt-1 text-xs text-muted">تظهر هنا عمليات الأرشفة وإلغاؤها عند حدوثها.</p>
      </div>
    )
  }

  return (
    <ol className="flex flex-col gap-3">
      {events.map((event) => (
        <li
          key={event.archiveEventId}
          className="flex flex-col gap-2 rounded-lg border border-border bg-background px-4 py-3"
        >
          <div className="flex flex-wrap items-center gap-2">
            <ArchiveEventTypeBadge eventTypeId={event.eventTypeId} />
            <span className="flex items-center gap-1.5 text-xs text-muted">
              <DocumentStatusBadge statusId={event.previousStatusId} />
              ←
              <DocumentStatusBadge statusId={event.newStatusId} />
            </span>
            <span dir="ltr" className="ms-auto whitespace-nowrap text-xs text-muted">
              {formatDateTime(event.occurredAt)}
            </span>
          </div>

          <dl className="grid gap-x-6 gap-y-1 text-sm sm:grid-cols-2">
            {event.archiveLocationId !== null && (
              <div className="flex items-baseline gap-1.5">
                <dt className="text-muted">الموقع:</dt>
                <dd>
                  <LookupLabel
                    lookup={LOOKUP_SLUGS.ArchiveLocations}
                    id={event.archiveLocationId}
                  />
                </dd>
              </div>
            )}
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
  )
}
