import { useState } from 'react'
import { Lock, LockOpen, Pencil, ShieldCheck } from 'lucide-react'
import {
  formatFieldValue,
  toFieldValueMap,
  useDepartmentRecords,
  visibleFields,
  type DepartmentRecordVisitResponse,
} from '@/entities/document-schema'
import { EditVisitRecordForm } from '@/features/department-record-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { formatDate, formatDateTime } from '@/shared/lib'
import { Badge, Button, Skeleton } from '@/shared/ui'

interface DepartmentRecordsPanelProps {
  documentId: string
}

type Editing = { visitId: string; mode: 'edit' | 'correct' } | null

/**
 * The records each department kept on its visits of this document, as a timeline per department.
 * The latest visit stays editable while the document is still in that department; earlier visits
 * are read-only and marked locked. The server returns only the records the caller may read.
 */
export function DepartmentRecordsPanel({ documentId }: DepartmentRecordsPanelProps) {
  const query = useDepartmentRecords(documentId)
  const [editing, setEditing] = useState<Editing>(null)

  if (query.isLoading) return <Skeleton className="h-40 w-full" />
  if (query.isError) {
    return (
      <p role="alert" className="text-sm text-danger">
        {toArabicErrorMessage(query.error)}
      </p>
    )
  }

  const records = query.data?.records ?? []
  if (records.length === 0) {
    return <p className="text-sm text-muted">لا سجلات دواوين متاحة لك على هذه الوثيقة.</p>
  }

  return (
    <div className="flex flex-col gap-6">
      {records.map((record) => (
        <section
          key={record.registryNumberId}
          aria-label={`سجل ${record.departmentNameAr}`}
          className="flex flex-col gap-3 rounded-xl border border-border bg-surface p-4"
        >
          <header className="flex flex-wrap items-center gap-2">
            <h3 className="text-sm font-semibold text-foreground">{record.departmentNameAr}</h3>
            <Badge variant="outline">
              <span dir="ltr" className="font-mono">
                {record.registryNumberText}
              </span>
            </Badge>
            <span dir="ltr" className="text-xs text-muted">
              {record.numberingYear}
            </span>
          </header>

          <ol className="flex flex-col gap-3 border-s border-border ps-4">
            {[...record.visits].reverse().map((visit) => (
              <VisitItem
                key={visit.visitId}
                documentId={documentId}
                visit={visit}
                editing={editing?.visitId === visit.visitId ? editing.mode : null}
                onEdit={(mode) => setEditing({ visitId: visit.visitId, mode })}
                onDone={() => setEditing(null)}
              />
            ))}
          </ol>
        </section>
      ))}
    </div>
  )
}

interface VisitItemProps {
  documentId: string
  visit: DepartmentRecordVisitResponse
  editing: 'edit' | 'correct' | null
  onEdit: (mode: 'edit' | 'correct') => void
  onDone: () => void
}

function VisitItem({ documentId, visit, editing, onEdit, onDone }: VisitItemProps) {
  const hasForm = visit.schemaVersionId !== null
  // A field hidden by its condition has nothing to say on this visit.
  const shown = new Set(
    visibleFields(visit.fields.map((entry) => entry.field), toFieldValueMap(visit.fields)).map(
      (field) => field.key,
    ),
  )

  return (
    <li className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex flex-wrap items-center gap-2 text-sm">
          <span className="font-medium text-foreground">زيارة</span>
          <span dir="ltr" className="text-muted">
            {formatDate(visit.registryDate)}
          </span>
          {visit.isLocked ? (
            <Badge variant="outline" title={visit.lockedAt ? formatDateTime(visit.lockedAt) : undefined}>
              <Lock className="size-3" aria-hidden />
              مقفل
            </Badge>
          ) : (
            <Badge variant="accent">
              <LockOpen className="size-3" aria-hidden />
              مفتوح
            </Badge>
          )}
          {hasForm && (
            <Badge variant="outline">
              <span dir="ltr">v{visit.versionNumber}</span>
            </Badge>
          )}
        </div>
        {editing === null && (
          <div className="flex items-center gap-2">
            {visit.canEdit && (
              <Button type="button" size="sm" variant="outline" onClick={() => onEdit('edit')}>
                <Pencil className="size-4" aria-hidden />
                تعديل السجل
              </Button>
            )}
            {visit.canCorrect && (
              <Button type="button" size="sm" variant="ghost" onClick={() => onEdit('correct')}>
                <ShieldCheck className="size-4" aria-hidden />
                تصحيح
              </Button>
            )}
          </div>
        )}
      </div>

      {visit.correctedAt && (
        <p className="text-xs text-muted">
          صُحّح في <span dir="ltr">{formatDateTime(visit.correctedAt)}</span>
          {visit.correctionReason ? ` — ${visit.correctionReason}` : ''}
        </p>
      )}

      {!hasForm ? (
        <p className="text-xs text-muted">لم يكن للديوان نموذج سجل لهذا النوع عند هذه الزيارة.</p>
      ) : editing ? (
        <EditVisitRecordForm documentId={documentId} visit={visit} mode={editing} onDone={onDone} />
      ) : (
        <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
          {visit.fields
            .filter((entry) => shown.has(entry.field.key))
            .map((entry) => (
              <div key={entry.field.documentFieldId} className="flex flex-col gap-0.5">
                <dt className="text-xs text-muted">
                  {entry.field.labelAr}
                  {entry.field.isRequired && <span className="text-danger"> *</span>}
                </dt>
                <dd className="whitespace-pre-wrap text-sm text-foreground">
                  {formatFieldValue(entry.field, entry.value)}
                </dd>
              </div>
            ))}
        </dl>
      )}
    </li>
  )
}
