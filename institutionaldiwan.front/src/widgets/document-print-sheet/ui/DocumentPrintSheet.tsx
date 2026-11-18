import { useDocumentArchiveEvents } from '@/entities/archive'
import { DepartmentLabel } from '@/entities/department'
import {
  useDocumentRegistryNumbers,
  useDocumentTimeline,
  type DocumentDetailResponse,
  type DocumentTimelineEntryResponse,
} from '@/entities/document'
import { useDocumentFiles } from '@/entities/document-file'
import { RELATION_DIRECTION_LABELS, useDocumentRelations } from '@/entities/document-relation'
import { formatFieldValue, useDocumentFields } from '@/entities/document-schema'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { useDocumentMovements } from '@/entities/movement'
import { useDocumentPersons } from '@/entities/person'
import { UserLabel } from '@/entities/user'
import { FEATURES } from '@/shared/config'
import { formatDateTime, t } from '@/shared/lib'
import { PrintSheet } from '@/shared/ui'
import {
  PrintDate,
  PrintDateTime,
  PrintFacts,
  PrintLatin,
  PrintSection,
  PrintTable,
} from './printParts'

interface DocumentPrintSheetProps {
  details: DocumentDetailResponse
  /** The registers need the numbering permission; without it that section is left out. */
  canViewNumbering: boolean
  onDone: () => void
}

function DocumentStatus({ id }: { id: number | null }) {
  return <LookupLabel lookup={LOOKUP_SLUGS.DocumentStatusTypes} id={id} />
}

function TimelineEvent({ entry }: { entry: DocumentTimelineEntryResponse }) {
  if (entry.eventKind === 'STATUS') {
    return (
      <span>
        {t('print.timeline.status-change')} {entry.fromStatusId !== null && <><DocumentStatus id={entry.fromStatusId} /> ← </>}
        <DocumentStatus id={entry.toStatusId} />
      </span>
    )
  }
  if (entry.eventKind === 'NUMBER') {
    return <LookupLabel lookup={LOOKUP_SLUGS.DocumentNumberEventTypes} id={entry.numberEventTypeId} />
  }
  return <span>{t('print.timeline.add-participant')}</span>
}

/**
 * The whole document on one printable template. A section the caller may not read, or one with
 * nothing in it, is simply left out.
 */
export function DocumentPrintSheet({ details, canViewNumbering, onDone }: DocumentPrintSheetProps) {
  const { document, participants, statusHistory } = details
  const documentId = document.documentId

  const fields = useDocumentFields(documentId)
  const numbers = useDocumentRegistryNumbers(documentId, { enabled: canViewNumbering })
  const persons = useDocumentPersons(documentId)
  const movements = useDocumentMovements(documentId)
  const files = useDocumentFiles(documentId)
  const relations = useDocumentRelations(documentId)
  const archive = useDocumentArchiveEvents(documentId)
  const timeline = useDocumentTimeline(documentId)

  const ready = ![fields, numbers, persons, movements, files, relations, archive, timeline].some(
    (query) => query.isLoading,
  )

  const fieldValues = fields.data?.fields ?? []
  const departmentNumbers = numbers.data?.departmentNumbers ?? []
  const personRows = persons.data ?? []
  const movementRows = movements.data ?? []
  const fileRows = files.data ?? []
  const relationRows = relations.data?.items ?? []
  const hiddenRelations = relations.data?.hiddenRelationCount ?? 0
  const archiveRows = archive.data ?? []
  const timelineRows = timeline.data ?? []

  return (
    <PrintSheet fileName={document.title} ready={ready} onDone={onDone}>
      <header className="flex items-end justify-between gap-4 border-b-2 border-accent pb-3">
        <div>
          <p className="text-xs text-muted">{t('print.document-kicker')}</p>
          <h1 className="text-xl font-bold text-foreground">{document.title}</h1>
        </div>
        <p className="text-[10px] text-muted">
          {t('print.extracted-at')} <span dir="ltr">{formatDateTime(new Date().toISOString())}</span>
        </p>
      </header>

      <PrintSection title={t('print.section.document')}>
        <PrintFacts
          facts={[
            { label: t('print.fact.type'), value: <LookupLabel lookup={LOOKUP_SLUGS.DocumentTypes} id={document.documentTypeId} /> },
            { label: t('print.fact.status'), value: <DocumentStatus id={document.statusId} /> },
            { label: t('print.fact.number'), value: <PrintLatin value={document.officialNumberText} /> },
            { label: t('print.fact.registration-date'), value: <PrintDate value={document.registrationDate} /> },
            { label: t('print.fact.owning-department'), value: <DepartmentLabel departmentId={document.owningDepartmentId} /> },
            { label: t('print.fact.created-at'), value: <PrintDateTime value={document.createdAt} /> },
            { label: t('print.fact.created-by'), value: <UserLabel userId={document.createdByUserId} /> },
          ]}
        />
      </PrintSection>

      {departmentNumbers.length > 0 && (
        <PrintSection title={t('print.section.registers')}>
          <PrintTable
            headers={[t('print.header.department'), t('print.header.registry-number'), t('print.header.year'), t('print.header.registry-date')]}
            rows={departmentNumbers.map((line) => [
              <DepartmentLabel departmentId={line.departmentId} />,
              <PrintLatin value={line.registryNumberText} />,
              <span dir="ltr">{line.numberingYear}</span>,
              <PrintDate value={line.registryDate} />,
            ])}
          />
        </PrintSection>
      )}

      {fieldValues.length > 0 && (
        <PrintSection title={t('print.section.fields')}>
          <PrintFacts
            columns={2}
            facts={fieldValues.map(({ field, value }) => ({
              label: field.labelAr,
              value: formatFieldValue(field, value),
            }))}
          />
        </PrintSection>
      )}

      {personRows.length > 0 && (
        <PrintSection title={t('print.section.persons')}>
          <PrintTable
            headers={[t('print.header.name'), t('print.header.role'), t('print.header.added-at')]}
            rows={personRows.map((person) => [
              person.displayName,
              <LookupLabel lookup={LOOKUP_SLUGS.DocumentPersonRoles} id={person.roleId} />,
              <PrintDateTime value={person.createdAt} />,
            ])}
          />
        </PrintSection>
      )}

      {FEATURES.participants && participants.length > 0 && (
        <PrintSection title={t('print.section.participants')}>
          <PrintTable
            headers={[t('print.header.participant-role'), t('print.header.party'), t('print.header.notes')]}
            rows={participants.map((participant) => [
              <LookupLabel lookup={LOOKUP_SLUGS.ParticipantRoles} id={participant.roleId} />,
              participant.internalDepartmentId ? (
                <DepartmentLabel departmentId={participant.internalDepartmentId} />
              ) : (
                participant.personName ?? '—'
              ),
              participant.notes ?? '—',
            ])}
          />
        </PrintSection>
      )}

      {movementRows.length > 0 && (
        <PrintSection title={t('print.section.movements')}>
          <div className="flex flex-col gap-3">
            {movementRows.map((movement) => (
              <div key={movement.movementId}>
                <PrintFacts
                  facts={[
                    { label: t('print.fact.movement-type'), value: <LookupLabel lookup={LOOKUP_SLUGS.MovementTypes} id={movement.movementTypeId} /> },
                    { label: t('print.fact.status'), value: <LookupLabel lookup={LOOKUP_SLUGS.MovementStatuses} id={movement.statusId} /> },
                    { label: t('print.fact.due-date'), value: <PrintDate value={movement.dueDate} /> },
                    { label: t('print.fact.from'), value: <DepartmentLabel departmentId={movement.sourceDepartmentId} /> },
                    { label: t('print.fact.to'), value: <DepartmentLabel departmentId={movement.destinationDepartmentId} /> },
                    { label: t('print.fact.instructions'), value: movement.instructions ?? '—' },
                    { label: t('print.fact.started'), value: <PrintDateTime value={movement.createdAt} /> },
                    { label: t('print.fact.closed'), value: <PrintDateTime value={movement.closedAt} /> },
                  ]}
                />
                {movement.steps.length > 0 && (
                  <div className="mt-1">
                    <PrintTable
                      headers={['#', t('print.header.from'), t('print.header.to'), t('print.header.status'), t('print.header.sent'), t('print.header.received'), t('print.header.completed'), t('print.header.registry-number'), t('print.header.registry-date'), t('print.header.notes')]}
                      rows={movement.steps.map((step) => [
                        <span dir="ltr">{step.stepNo}</span>,
                        <DepartmentLabel departmentId={step.fromDepartmentId} />,
                        <DepartmentLabel departmentId={step.toDepartmentId} />,
                        <LookupLabel lookup={LOOKUP_SLUGS.MovementStepStatuses} id={step.stepStatusId} />,
                        <PrintDateTime value={step.sentAt} />,
                        <PrintDateTime value={step.receivedAt} />,
                        <PrintDateTime value={step.completedAt} />,
                        <PrintLatin value={step.registryNumberText} />,
                        <PrintDate value={step.registryDate} />,
                        [step.instructions, step.outcomeNotes].filter(Boolean).join(' · ') || '—',
                      ])}
                    />
                  </div>
                )}
              </div>
            ))}
          </div>
        </PrintSection>
      )}

      {fileRows.length > 0 && (
        <PrintSection title={t('print.section.files')}>
          <PrintTable
            headers={[t('print.header.file'), t('print.header.type'), t('print.header.version'), t('print.header.primary-scan'), t('print.header.added-at')]}
            rows={fileRows.map((file) => [
              file.isArchived ? t('print.file.archived', { title: file.title }) : file.title,
              <LookupLabel lookup={LOOKUP_SLUGS.DocumentFileTypes} id={file.fileTypeId} />,
              <span dir="ltr">{file.currentVersionNumber}</span>,
              file.isPrimaryScan ? t('print.yes') : t('print.no'),
              <PrintDateTime value={file.createdAt} />,
            ])}
          />
        </PrintSection>
      )}

      {relationRows.length > 0 && (
        <PrintSection title={t('print.section.relations')}>
          <PrintTable
            headers={[t('print.header.relation-type'), t('print.header.direction'), t('print.header.document'), t('print.fact.number'), t('print.header.status'), t('print.header.notes')]}
            rows={relationRows.map((relation) => [
              relation.type.descAr,
              t(RELATION_DIRECTION_LABELS[relation.direction]),
              relation.relatedDocument.title,
              <PrintLatin value={relation.relatedDocument.officialNumberText} />,
              <DocumentStatus id={relation.relatedDocument.statusId} />,
              relation.notes ?? '—',
            ])}
          />
          {hiddenRelations > 0 && (
            <p className="mt-1 text-[10px] text-muted">
              {t('print.relations.hidden', { count: hiddenRelations })}
            </p>
          )}
        </PrintSection>
      )}

      {statusHistory.length > 0 && (
        <PrintSection title={t('print.section.status-history')}>
          <PrintTable
            headers={[t('print.header.from'), t('print.header.to'), t('print.header.reason'), t('print.header.by'), t('print.header.date')]}
            rows={statusHistory.map((entry) => [
              <DocumentStatus id={entry.fromStatusId} />,
              <DocumentStatus id={entry.toStatusId} />,
              entry.reason ?? '—',
              <UserLabel userId={entry.changedByUserId} />,
              <PrintDateTime value={entry.changedAt} />,
            ])}
          />
        </PrintSection>
      )}

      {timelineRows.length > 0 && (
        <PrintSection title={t('print.section.timeline')}>
          <PrintTable
            headers={[t('print.header.event'), t('print.header.by'), t('print.header.date')]}
            rows={timelineRows.map((entry) => [
              <TimelineEvent entry={entry} />,
              <UserLabel userId={entry.actorUserId} />,
              <PrintDateTime value={entry.occurredAt} />,
            ])}
          />
        </PrintSection>
      )}

      {archiveRows.length > 0 && (
        <PrintSection title={t('print.section.archive')}>
          <PrintTable
            headers={[t('print.header.event'), t('print.header.location'), t('print.header.reason'), t('print.header.by'), t('print.header.date')]}
            rows={archiveRows.map((event) => [
              <LookupLabel lookup={LOOKUP_SLUGS.ArchiveEventTypes} id={event.eventTypeId} />,
              <LookupLabel lookup={LOOKUP_SLUGS.ArchiveLocations} id={event.archiveLocationId} />,
              event.reason ?? '—',
              <UserLabel userId={event.actorUserId} />,
              <PrintDateTime value={event.occurredAt} />,
            ])}
          />
        </PrintSection>
      )}
    </PrintSheet>
  )
}
