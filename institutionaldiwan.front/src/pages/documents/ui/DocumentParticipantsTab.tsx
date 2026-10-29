import { UserPlus } from 'lucide-react'
import type { DocumentParticipantResponse } from '@/entities/document'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { formatDateTime } from '@/shared/lib'
import { Badge, Button } from '@/shared/ui'
import { ParticipantTarget } from './ParticipantTarget'

interface DocumentParticipantsTabProps {
  participants: DocumentParticipantResponse[]
  departmentNames: Map<string, string>
  canAdd: boolean
  onAdd: () => void
}

/** Participants tab: every party with its role, resolved target name, and notes. */
export function DocumentParticipantsTab({
  participants,
  departmentNames,
  canAdd,
  onAdd,
}: DocumentParticipantsTabProps) {
  return (
    <section className="rounded-xl border border-border bg-surface p-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="text-base font-semibold text-foreground">المشاركون</h2>
          <Badge variant="outline">{participants.length}</Badge>
        </div>
        {canAdd && (
          <Button type="button" size="sm" onClick={onAdd}>
            <UserPlus className="size-4" aria-hidden />
            إضافة مشارك
          </Button>
        )}
      </div>

      {participants.length === 0 ? (
        <div className="mt-6 flex flex-col items-center gap-2 rounded-lg border border-dashed border-border px-4 py-10 text-center">
          <p className="text-sm font-medium text-foreground">لا يوجد مشاركون بعد.</p>
          <p className="max-w-md text-xs text-muted">
            أضف الجهة المرسلة أو المستلمة أو نسخة إلى الوثيقة. تُقبل جهة خارجية واحدة فقط لكل
            وثيقة.
          </p>
          {canAdd && (
            <Button type="button" size="sm" variant="outline" onClick={onAdd}>
              <UserPlus className="size-4" aria-hidden />
              إضافة أول مشارك
            </Button>
          )}
        </div>
      ) : (
        <ul className="mt-4 flex flex-col gap-3">
          {participants.map((participant) => (
            <li
              key={participant.participantId}
              className="flex flex-col gap-2 rounded-lg border border-border bg-background px-4 py-3 sm:flex-row sm:items-start sm:justify-between"
            >
              <div className="flex min-w-0 flex-col gap-1">
                <div className="flex flex-wrap items-center gap-2">
                  <LookupLabel
                    lookup={LOOKUP_SLUGS.ParticipantRoles}
                    id={participant.roleId}
                    className="font-medium"
                  />
                  {participant.isPrimary && <Badge variant="accent">أساسي</Badge>}
                </div>
                <div className="flex flex-wrap items-center gap-1.5">
                  <LookupLabel
                    lookup={LOOKUP_SLUGS.ParticipantTargetTypes}
                    id={participant.targetTypeId}
                    className="text-xs text-muted"
                  />
                  <span aria-hidden className="text-xs text-muted">
                    ·
                  </span>
                  <ParticipantTarget
                    participant={participant}
                    departmentName={
                      participant.internalDepartmentId
                        ? departmentNames.get(participant.internalDepartmentId)
                        : undefined
                    }
                  />
                </div>
                {participant.notes && (
                  <p className="text-xs leading-6 text-muted">{participant.notes}</p>
                )}
              </div>

              <span className="shrink-0 text-xs text-muted">
                {formatDateTime(participant.createdAt)}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
