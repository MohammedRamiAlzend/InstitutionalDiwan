import { useExternalOrganizationById } from '@/entities/external-organization'
import type { DocumentParticipantResponse } from '@/entities/document'
import { Skeleton } from '@/shared/ui'
import { formatShortId } from '@/shared/lib'

interface ParticipantTargetProps {
  participant: DocumentParticipantResponse
  /** Department label resolved by the caller from `identity-options/departments`. */
  departmentName?: string
}

/** Name for whichever single target the participant carries (department, organization, person). */
export function ParticipantTarget({ participant, departmentName }: ParticipantTargetProps) {
  if (participant.internalDepartmentId) {
    return departmentName ? (
      <span className="text-sm text-foreground">{departmentName}</span>
    ) : (
      <ShortId value={participant.internalDepartmentId} />
    )
  }

  if (participant.externalOrganizationId) {
    return <ExternalOrganizationName id={participant.externalOrganizationId} />
  }

  return (
    <span className="text-sm text-foreground">
      {participant.personName ?? '—'}
    </span>
  )
}

function ExternalOrganizationName({ id }: { id: string }) {
  const query = useExternalOrganizationById(id)

  if (query.isLoading) {
    return <Skeleton className="inline-block h-4 w-28 align-middle" />
  }

  return query.data ? (
    <span className="text-sm text-foreground">{query.data.nameAr}</span>
  ) : (
    <ShortId value={id} />
  )
}

function ShortId({ value }: { value: string }) {
  return (
    <span
      dir="ltr"
      title={value}
      className="font-mono text-xs text-muted"
    >
      {formatShortId(value)}
    </span>
  )
}
