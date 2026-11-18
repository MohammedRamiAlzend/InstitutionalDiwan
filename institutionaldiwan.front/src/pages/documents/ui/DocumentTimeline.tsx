import { useMemo } from 'react'
import { Archive, Hash, UserPlus, Waypoints } from 'lucide-react'
import {
  ArchiveEventTypeBadge,
  useDocumentArchiveEvents,
  type DocumentArchiveEventResponse,
} from '@/entities/archive'
import {
  DocumentStatusBadge,
  type DocumentParticipantResponse,
  type DocumentStatusHistoryResponse,
  type DocumentTimelineEntryResponse,
} from '@/entities/document'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { UserLabel } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { DOCUMENT_NUMBER_EVENT_TYPE, type LabelKey } from '@/shared/config'
import { formatDateTime, formatShortId, resolveLabel, t, useLabelsStore } from '@/shared/lib'
import { Button, Label, Skeleton } from '@/shared/ui'
import { ParticipantTarget } from './ParticipantTarget'

const NUMBER_EVENT_LABELS: Record<number, LabelKey> = {
  [DOCUMENT_NUMBER_EVENT_TYPE.Issued]: 'docdetails.timeline.number-event.issued',
  [DOCUMENT_NUMBER_EVENT_TYPE.Cancelled]: 'docdetails.timeline.number-event.cancelled',
  [DOCUMENT_NUMBER_EVENT_TYPE.Replaced]: 'docdetails.timeline.number-event.replaced',
  [DOCUMENT_NUMBER_EVENT_TYPE.RegisteredInDepartment]: 'docdetails.timeline.number-event.registered-dept',
  [DOCUMENT_NUMBER_EVENT_TYPE.ReRegisteredInDepartment]: 'docdetails.timeline.number-event.re-registered-dept',
}

type TimelineKind = 'status' | 'participant' | 'number' | 'archive'

interface TimelineRow {
  id: string
  kind: TimelineKind
  at: string
  actorUserId: string
  statusEntry?: DocumentStatusHistoryResponse
  timelineEntry?: DocumentTimelineEntryResponse
  archiveEvent?: DocumentArchiveEventResponse
}

const ROW_ICONS: Record<TimelineKind, typeof Waypoints> = {
  status: Waypoints,
  participant: UserPlus,
  number: Hash,
  archive: Archive,
}

interface DocumentTimelineProps {
  documentId: string
  statusHistory: DocumentStatusHistoryResponse[]
  entries: DocumentTimelineEntryResponse[]
  participantsById: Map<string, DocumentParticipantResponse>
  departmentNames: Map<string, string>
  isLoading: boolean
}

/**
 * Unified log of the details tab, newest first: status transitions, participant and number
 * events, and archive operations on one vertical track. `STATUS` timeline entries are skipped
 * because the status history already carries them with their reasons. Server descriptions are
 * English system text, so the rows are rebuilt from the structural fields instead.
 */
export function DocumentTimeline({
  documentId,
  statusHistory,
  entries,
  participantsById,
  departmentNames,
  isLoading,
}: DocumentTimelineProps) {
  const archiveQuery = useDocumentArchiveEvents(documentId)

  const rows = useMemo(() => {
    const merged: TimelineRow[] = [
      ...statusHistory.map((entry) => ({
        id: `status-${entry.historyId}`,
        kind: 'status' as const,
        at: entry.changedAt,
        actorUserId: entry.changedByUserId,
        statusEntry: entry,
      })),
      ...entries
        .filter((entry) => entry.eventKind !== 'STATUS')
        .map((entry, index) => ({
          id: `timeline-${entry.eventKind}-${entry.occurredAt}-${index}`,
          kind: (entry.eventKind === 'PARTICIPANT' ? 'participant' : 'number') as TimelineKind,
          at: entry.occurredAt,
          actorUserId: entry.actorUserId,
          timelineEntry: entry,
        })),
      ...(archiveQuery.data ?? []).map((event) => ({
        id: `archive-${event.archiveEventId}`,
        kind: 'archive' as const,
        at: event.occurredAt,
        actorUserId: event.actorUserId,
        archiveEvent: event,
      })),
    ]

    return merged.sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime())
  }, [statusHistory, entries, archiveQuery.data])

  if (isLoading || archiveQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3">
        <Skeleton className="h-14 w-full" />
        <Skeleton className="h-14 w-full" />
        <Skeleton className="h-14 w-full" />
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-4">
      {archiveQuery.isError && (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
        >
          <span>{t('docdetails.timeline.archive-error', { error: toArabicErrorMessage(archiveQuery.error) })}</span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => void archiveQuery.refetch()}
          >
            <Label k="common.action.retry" />
          </Button>
        </div>
      )}

      {rows.length === 0 ? (
        <p className="text-sm text-muted"><Label k="docdetails.timeline.empty" /></p>
      ) : (
        <ol className="flex flex-col gap-4 border-s-2 border-border ps-5">
          {rows.map((row) => {
            const Icon = ROW_ICONS[row.kind]
            return (
              <li key={row.id} className="relative flex flex-col gap-1.5">
                <span className="absolute -start-[33px] flex size-6 items-center justify-center rounded-full border border-border bg-surface text-muted">
                  <Icon className="size-3.5" aria-hidden />
                </span>
                <TimelineRowBody
                  row={row}
                  participantsById={participantsById}
                  departmentNames={departmentNames}
                />
                <div className="flex flex-wrap items-center gap-2 text-xs text-muted">
                  <span dir="ltr">{formatDateTime(row.at)}</span>
                  <span aria-hidden>·</span>
                  <Label k="docdetails.by" />
                  <UserLabel userId={row.actorUserId} />
                  {row.archiveEvent && row.archiveEvent.archiveLocationId !== null && (
                    <>
                      <span aria-hidden>·</span>
                      <Label k="docdetails.timeline.location" />
                      <LookupLabel
                        lookup={LOOKUP_SLUGS.ArchiveLocations}
                        id={row.archiveEvent.archiveLocationId}
                      />
                    </>
                  )}
                </div>
              </li>
            )
          })}
        </ol>
      )}
    </div>
  )
}

function TimelineRowBody({
  row,
  participantsById,
  departmentNames,
}: {
  row: TimelineRow
  participantsById: Map<string, DocumentParticipantResponse>
  departmentNames: Map<string, string>
}) {
  const overrides = useLabelsStore((state) => state.overrides)

  if (row.kind === 'status' && row.statusEntry) {
    const entry = row.statusEntry
    return (
      <>
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="text-xs text-muted"><Label k="docdetails.timeline.status-change" /></span>
          {entry.fromStatusId !== null && (
            <>
              <DocumentStatusBadge statusId={entry.fromStatusId} />
              <span className="text-xs text-muted"><Label k="docdetails.timeline.to" /></span>
            </>
          )}
          <DocumentStatusBadge statusId={entry.toStatusId} />
        </div>
        {entry.reason && (
          <p className="text-xs leading-6 text-foreground"><Label k="docdetails.timeline.reason" /> {entry.reason}</p>
        )}
      </>
    )
  }

  if (row.kind === 'participant' && row.timelineEntry) {
    const entry = row.timelineEntry
    const participant = entry.participantId
      ? participantsById.get(entry.participantId)
      : undefined

    return (
      <div className="flex flex-wrap items-center gap-1.5">
        <span className="text-xs text-muted"><Label k="docdetails.action.add-participant" /></span>
        {participant ? (
          <>
            <LookupLabel lookup={LOOKUP_SLUGS.ParticipantRoles} id={participant.roleId} />
            <ParticipantTarget
              participant={participant}
              departmentName={
                participant.internalDepartmentId
                  ? departmentNames.get(participant.internalDepartmentId)
                  : undefined
              }
            />
          </>
        ) : (
          <span className="text-sm text-foreground"><Label k="docdetails.timeline.participant-fallback" /></span>
        )}
      </div>
    )
  }

  if (row.kind === 'number' && row.timelineEntry) {
    const entry = row.timelineEntry
    return (
      <div className="flex flex-wrap items-center gap-1.5">
        <span className="text-sm text-foreground">
          {entry.numberEventTypeId !== null
            ? resolveLabel(NUMBER_EVENT_LABELS[entry.numberEventTypeId] ?? 'docdetails.timeline.number-event.unknown', overrides)
            : t('docdetails.timeline.number-event.unknown')}
        </span>
        {entry.documentNumberId && (
          <span
            dir="ltr"
            title={entry.documentNumberId}
            className="font-mono text-xs text-muted"
          >
            {formatShortId(entry.documentNumberId)}
          </span>
        )}
      </div>
    )
  }

  if (row.kind === 'archive' && row.archiveEvent) {
    const event = row.archiveEvent
    return (
      <>
        <div className="flex flex-wrap items-center gap-2">
          <ArchiveEventTypeBadge eventTypeId={event.eventTypeId} />
          <span className="flex items-center gap-1.5 text-xs text-muted">
            <DocumentStatusBadge statusId={event.previousStatusId} />
            ←
            <DocumentStatusBadge statusId={event.newStatusId} />
          </span>
        </div>
        {event.reason && (
          <p className="whitespace-pre-line text-xs leading-6 text-foreground">
            <Label k="docdetails.timeline.reason" /> {event.reason}
          </p>
        )}
      </>
    )
  }

  return null
}