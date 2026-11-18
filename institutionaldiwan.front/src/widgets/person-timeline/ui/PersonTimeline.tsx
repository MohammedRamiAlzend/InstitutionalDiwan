import { Link } from 'react-router-dom'
import { Archive, ChevronLeft, ChevronRight, Hash, Send, UserPlus, Waypoints, type LucideIcon } from 'lucide-react'
import { DepartmentLabel } from '@/entities/department'
import { DocumentStatusBadge } from '@/entities/document'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { usePersonTimeline, type PersonTimelineEntryResponse, type PersonTimelineKind } from '@/entities/person'
import { UserLabel } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { formatDateTime, t, useTableState } from '@/shared/lib'
import { Button, Label, Skeleton } from '@/shared/ui'

const KIND_ICONS: Record<PersonTimelineKind, LucideIcon> = {
  STATUS: Waypoints,
  MOVEMENT: Send,
  NUMBER: Hash,
  ARCHIVE: Archive,
  PERSON: UserPlus,
}

interface PersonTimelineProps {
  personId: string
}

function EventSummary({ entry }: { entry: PersonTimelineEntryResponse }) {
  switch (entry.kind) {
    case 'STATUS':
      return (
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="text-xs text-muted">{t('persons.status-change')}</span>
          {entry.fromStatusId !== null && (
            <>
              <DocumentStatusBadge statusId={entry.fromStatusId} />
              <span className="text-xs text-muted">{t('persons.timeline.to')}</span>
            </>
          )}
          <DocumentStatusBadge statusId={entry.toStatusId ?? 0} />
        </div>
      )
    case 'MOVEMENT':
      return (
        <div className="flex flex-wrap items-center gap-1.5">
          <LookupLabel lookup={LOOKUP_SLUGS.MovementEventTypes} id={entry.movementEventTypeId} className="font-medium" />
          {entry.fromDepartmentId && (
            <>
              <span className="text-xs text-muted">{t('persons.timeline.from')}</span>
              <DepartmentLabel departmentId={entry.fromDepartmentId} className="text-sm" />
            </>
          )}
          {entry.toDepartmentId && (
            <>
              <span className="text-xs text-muted">{t('persons.timeline.to')}</span>
              <DepartmentLabel departmentId={entry.toDepartmentId} className="text-sm" />
            </>
          )}
        </div>
      )
    case 'NUMBER':
      return <LookupLabel lookup={LOOKUP_SLUGS.DocumentNumberEventTypes} id={entry.numberEventTypeId} />
    case 'ARCHIVE':
      return (
        <div className="flex flex-wrap items-center gap-1.5">
          <LookupLabel lookup={LOOKUP_SLUGS.ArchiveEventTypes} id={entry.archiveEventTypeId} className="font-medium" />
          {entry.archiveLocationId !== null && (
            <>
              <span className="text-xs text-muted">{t('persons.timeline.in')}</span>
              <LookupLabel lookup={LOOKUP_SLUGS.ArchiveLocations} id={entry.archiveLocationId} />
            </>
          )}
        </div>
      )
    case 'PERSON':
      return (
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="text-xs text-muted">{t('persons.timeline.linked-as')}</span>
          <LookupLabel lookup={LOOKUP_SLUGS.DocumentPersonRoles} id={entry.roleId} className="font-medium" />
        </div>
      )
  }
}

/** Everything that happened to the person's documents, newest first, each event naming its document. */
export function PersonTimeline({ personId }: PersonTimelineProps) {
  const table = useTableState()
  const timelineQuery = usePersonTimeline(personId, { page: table.page, pageSize: table.pageSize })

  if (timelineQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3">
        <Skeleton className="h-16 w-full" />
        <Skeleton className="h-16 w-full" />
        <Skeleton className="h-16 w-full" />
      </div>
    )
  }

  if (timelineQuery.isError) {
    return (
      <div className="flex flex-col items-center gap-3 rounded-xl border border-border bg-surface px-6 py-10 text-center">
        <p className="text-sm font-medium text-danger">{toArabicErrorMessage(timelineQuery.error)}</p>
        <Button type="button" variant="outline" onClick={() => void timelineQuery.refetch()}>
          <Label k="common.action.retry" />
        </Button>
      </div>
    )
  }

  const entries = timelineQuery.data?.items ?? []
  const totalPages = timelineQuery.data?.totalPages ?? 0

  if (entries.length === 0) {
    return <p className="text-sm text-muted">{t('persons.timeline.empty')}</p>
  }

  return (
    <div className="flex flex-col gap-4">
      <ol className="flex flex-col gap-3">
        {entries.map((entry, index) => {
          const Icon = KIND_ICONS[entry.kind]
          return (
            <li
              key={`${entry.kind}-${entry.occurredAt}-${index}`}
              className="flex gap-3 rounded-lg border border-border bg-surface px-4 py-3"
            >
              <span className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-full bg-surface-muted text-muted">
                <Icon className="size-4" aria-hidden />
              </span>
              <div className="flex min-w-0 flex-1 flex-col gap-1">
                <Link
                  to={ROUTES.DOCUMENT_DETAIL(entry.documentId)}
                  className="truncate text-sm font-medium text-accent hover:underline"
                >
                  {entry.documentTitle}
                </Link>
                <EventSummary entry={entry} />
                {entry.notes && <p className="text-xs leading-6 text-muted">{entry.notes}</p>}
                <div className="flex flex-wrap items-center gap-2 text-xs text-muted">
                  <span dir="ltr">{formatDateTime(entry.occurredAt)}</span>
                  <span aria-hidden>·</span>
                  <span>{t('persons.timeline.by')}</span>
                  <UserLabel userId={entry.actorUserId} />
                </div>
              </div>
            </li>
          )
        })}
      </ol>

      {totalPages > 1 && (
        <nav aria-label={t('persons.timeline.pages-aria')} className="flex items-center justify-center gap-3">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={table.page <= 1}
            onClick={() => table.setPage(table.page - 1)}
          >
            <ChevronRight className="size-4" aria-hidden />
            <Label k="persons.timeline.newer" />
          </Button>
          <span className="text-sm text-muted">
            {table.page} / {totalPages}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={table.page >= totalPages}
            onClick={() => table.setPage(table.page + 1)}
          >
            <Label k="persons.timeline.older" />
            <ChevronLeft className="size-4" aria-hidden />
          </Button>
        </nav>
      )}
    </div>
  )
}
