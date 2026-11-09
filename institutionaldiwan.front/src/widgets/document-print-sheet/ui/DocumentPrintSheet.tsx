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
import { formatDateTime } from '@/shared/lib'
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
        تغيير الحالة: {entry.fromStatusId !== null && <><DocumentStatus id={entry.fromStatusId} /> ← </>}
        <DocumentStatus id={entry.toStatusId} />
      </span>
    )
  }
  if (entry.eventKind === 'NUMBER') {
    return <LookupLabel lookup={LOOKUP_SLUGS.DocumentNumberEventTypes} id={entry.numberEventTypeId} />
  }
  return <span>إضافة مشارك</span>
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
          <p className="text-xs text-muted">وثيقة</p>
          <h1 className="text-xl font-bold text-foreground">{document.title}</h1>
        </div>
        <p className="text-[10px] text-muted">
          تاريخ الاستخراج: <span dir="ltr">{formatDateTime(new Date().toISOString())}</span>
        </p>
      </header>

      <PrintSection title="بيانات الوثيقة">
        <PrintFacts
          facts={[
            { label: 'نوع الوثيقة', value: <LookupLabel lookup={LOOKUP_SLUGS.DocumentTypes} id={document.documentTypeId} /> },
            { label: 'الحالة', value: <DocumentStatus id={document.statusId} /> },
            { label: 'رقم النظام', value: <PrintLatin value={document.officialNumberText} /> },
            { label: 'تاريخ التسجيل', value: <PrintDate value={document.registrationDate} /> },
            { label: 'القسم المالك', value: <DepartmentLabel departmentId={document.owningDepartmentId} /> },
            { label: 'تاريخ الإنشاء', value: <PrintDateTime value={document.createdAt} /> },
            { label: 'أنشأها', value: <UserLabel userId={document.createdByUserId} /> },
          ]}
        />
      </PrintSection>

      {departmentNumbers.length > 0 && (
        <PrintSection title="سجلات الأقسام">
          <PrintTable
            headers={['القسم', 'رقم السجل', 'السنة', 'تاريخ السجل']}
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
        <PrintSection title="حقول الوثيقة">
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
        <PrintSection title="الأشخاص المرتبطون">
          <PrintTable
            headers={['الاسم', 'الصفة', 'تاريخ الإضافة']}
            rows={personRows.map((person) => [
              person.displayName,
              <LookupLabel lookup={LOOKUP_SLUGS.DocumentPersonRoles} id={person.roleId} />,
              <PrintDateTime value={person.createdAt} />,
            ])}
          />
        </PrintSection>
      )}

      {FEATURES.participants && participants.length > 0 && (
        <PrintSection title="المشاركون">
          <PrintTable
            headers={['الدور', 'الجهة', 'ملاحظات']}
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
        <PrintSection title="حركة الوثيقة">
          <div className="flex flex-col gap-3">
            {movementRows.map((movement) => (
              <div key={movement.movementId}>
                <PrintFacts
                  facts={[
                    { label: 'نوع الحركة', value: <LookupLabel lookup={LOOKUP_SLUGS.MovementTypes} id={movement.movementTypeId} /> },
                    { label: 'الحالة', value: <LookupLabel lookup={LOOKUP_SLUGS.MovementStatuses} id={movement.statusId} /> },
                    { label: 'الموعد النهائي', value: <PrintDate value={movement.dueDate} /> },
                    { label: 'من', value: <DepartmentLabel departmentId={movement.sourceDepartmentId} /> },
                    { label: 'إلى', value: <DepartmentLabel departmentId={movement.destinationDepartmentId} /> },
                    { label: 'التعليمات', value: movement.instructions ?? '—' },
                    { label: 'بدأت', value: <PrintDateTime value={movement.createdAt} /> },
                    { label: 'أُغلقت', value: <PrintDateTime value={movement.closedAt} /> },
                  ]}
                />
                {movement.steps.length > 0 && (
                  <div className="mt-1">
                    <PrintTable
                      headers={['#', 'من', 'إلى', 'الحالة', 'الإرسال', 'الاستلام', 'الإنجاز', 'رقم السجل', 'تاريخ السجل', 'الملاحظات']}
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
        <PrintSection title="المرفقات">
          <PrintTable
            headers={['الملف', 'النوع', 'الإصدار', 'نسخة المسح الأساسية', 'تاريخ الإضافة']}
            rows={fileRows.map((file) => [
              file.isArchived ? `${file.title} (مؤرشف)` : file.title,
              <LookupLabel lookup={LOOKUP_SLUGS.DocumentFileTypes} id={file.fileTypeId} />,
              <span dir="ltr">{file.currentVersionNumber}</span>,
              file.isPrimaryScan ? 'نعم' : 'لا',
              <PrintDateTime value={file.createdAt} />,
            ])}
          />
        </PrintSection>
      )}

      {relationRows.length > 0 && (
        <PrintSection title="الوثائق المرتبطة">
          <PrintTable
            headers={['نوع الرابط', 'الاتجاه', 'الوثيقة', 'رقم النظام', 'الحالة', 'ملاحظات']}
            rows={relationRows.map((relation) => [
              relation.type.descAr,
              RELATION_DIRECTION_LABELS[relation.direction],
              relation.relatedDocument.title,
              <PrintLatin value={relation.relatedDocument.officialNumberText} />,
              <DocumentStatus id={relation.relatedDocument.statusId} />,
              relation.notes ?? '—',
            ])}
          />
          {hiddenRelations > 0 && (
            <p className="mt-1 text-[10px] text-muted">
              توجد <span dir="ltr">{hiddenRelations}</span> روابط إضافية لوثائق لا تملك صلاحية عرضها.
            </p>
          )}
        </PrintSection>
      )}

      {statusHistory.length > 0 && (
        <PrintSection title="سجل الحالة">
          <PrintTable
            headers={['من', 'إلى', 'السبب', 'بواسطة', 'التاريخ']}
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
        <PrintSection title="الخط الزمني">
          <PrintTable
            headers={['الحدث', 'بواسطة', 'التاريخ']}
            rows={timelineRows.map((entry) => [
              <TimelineEvent entry={entry} />,
              <UserLabel userId={entry.actorUserId} />,
              <PrintDateTime value={entry.occurredAt} />,
            ])}
          />
        </PrintSection>
      )}

      {archiveRows.length > 0 && (
        <PrintSection title="سجل الأرشفة">
          <PrintTable
            headers={['الحدث', 'الموقع', 'السبب', 'بواسطة', 'التاريخ']}
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
