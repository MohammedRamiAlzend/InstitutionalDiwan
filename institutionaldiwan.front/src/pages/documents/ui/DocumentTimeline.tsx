import { Hash, UserPlus, Waypoints } from 'lucide-react'
import {
  DocumentStatusBadge,
  type DocumentParticipantResponse,
  type DocumentTimelineEntryResponse,
} from '@/entities/document'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { formatDateTime, formatShortId } from '@/shared/lib'
import { Skeleton } from '@/shared/ui'
import { ParticipantTarget } from './ParticipantTarget'

const NUMBER_EVENT_LABELS: Record<number, string> = {
  1: 'صدر رقم رسمي',
  2: 'أُلغي الرقم الرسمي',
  3: 'استُبدل الرقم الرسمي',
  4: 'سُجّل رقم الدائرة',
  5: 'أُعيد تسجيل رقم الدائرة',
}

interface DocumentTimelineProps {
  entries: DocumentTimelineEntryResponse[]
  participantsById: Map<string, DocumentParticipantResponse>
  departmentNames: Map<string, string>
  isLoading: boolean
}

/**
 * Chronological audit trail (oldest first). Server descriptions are English system
 * text, so the rows are rebuilt from the structural fields instead.
 */
export function DocumentTimeline({
  entries,
  participantsById,
  departmentNames,
  isLoading,
}: DocumentTimelineProps) {
  if (isLoading) {
    return (
      <div className="flex flex-col gap-3">
        <Skeleton className="h-14 w-full" />
        <Skeleton className="h-14 w-full" />
        <Skeleton className="h-14 w-full" />
      </div>
    )
  }

  if (entries.length === 0) {
    return <p className="text-sm text-muted">لا توجد أحداث مسجلة بعد.</p>
  }

  return (
    <ol className="flex flex-col gap-3">
      {entries.map((entry, index) => {
        const participant = entry.participantId
          ? participantsById.get(entry.participantId)
          : undefined

        return (
          <li
            key={`${entry.eventKind}-${entry.occurredAt}-${index}`}
            className="flex gap-3 rounded-lg border border-border bg-surface px-4 py-3"
          >
            <span className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-full bg-surface-muted text-muted">
              {entry.eventKind === 'STATUS' && <Waypoints className="size-4" aria-hidden />}
              {entry.eventKind === 'PARTICIPANT' && (
                <UserPlus className="size-4" aria-hidden />
              )}
              {entry.eventKind === 'NUMBER' && <Hash className="size-4" aria-hidden />}
            </span>

            <div className="flex min-w-0 flex-1 flex-col gap-1">
              {entry.eventKind === 'STATUS' && (
                <div className="flex flex-wrap items-center gap-1.5">
                  <span className="text-xs text-muted">تغيير الحالة</span>
                  {entry.fromStatusId !== null && (
                    <>
                      <DocumentStatusBadge statusId={entry.fromStatusId} />
                      <span className="text-xs text-muted">إلى</span>
                    </>
                  )}
                  <DocumentStatusBadge statusId={entry.toStatusId ?? 0} />
                </div>
              )}

              {entry.eventKind === 'PARTICIPANT' && (
                <div className="flex flex-wrap items-center gap-1.5">
                  <span className="text-xs text-muted">إضافة مشارك</span>
                  {participant ? (
                    <>
                      <LookupLabel
                        lookup={LOOKUP_SLUGS.ParticipantRoles}
                        id={participant.roleId}
                      />
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
                    <span className="text-sm text-foreground">مشارك</span>
                  )}
                </div>
              )}

              {entry.eventKind === 'NUMBER' && (
                <div className="flex flex-wrap items-center gap-1.5">
                  <span className="text-sm text-foreground">
                    {entry.numberEventTypeId !== null
                      ? (NUMBER_EVENT_LABELS[entry.numberEventTypeId] ?? 'حدث رقم')
                      : 'حدث رقم'}
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
              )}

              <div className="flex flex-wrap items-center gap-2 text-xs text-muted">
                <span>{formatDateTime(entry.occurredAt)}</span>
                <span aria-hidden>·</span>
                <span>بواسطة</span>
                <span dir="ltr" title={entry.actorUserId} className="font-mono">
                  {formatShortId(entry.actorUserId)}
                </span>
              </div>
            </div>
          </li>
        )
      })}
    </ol>
  )
}
