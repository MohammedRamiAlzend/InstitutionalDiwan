import { useState } from 'react'
import { Pencil } from 'lucide-react'
import {
  formatFieldValue,
  toFieldValueMap,
  useDocumentFields,
  visibleFields,
} from '@/entities/document-schema'
import { EditFieldValuesForm } from '@/features/document-field-values'
import { toArabicErrorMessage } from '@/shared/api'
import { Badge, Button, Skeleton } from '@/shared/ui'

interface DocumentFieldsPanelProps {
  documentId: string
  /** `DOCUMENT_EDIT` on a draft; registered, canceled and archived documents keep their values locked. */
  canEdit: boolean
  /** Why editing is unavailable, shown next to the values when the caller could otherwise edit. */
  lockedReason?: string
}

/** The document's dynamic fields against the schema version it was pinned to at creation. */
export function DocumentFieldsPanel({ documentId, canEdit, lockedReason }: DocumentFieldsPanelProps) {
  const query = useDocumentFields(documentId)
  const [editing, setEditing] = useState(false)

  if (query.isLoading) return <Skeleton className="h-40 w-full" />
  if (query.isError) {
    return (
      <p role="alert" className="text-sm text-danger">
        {toArabicErrorMessage(query.error)}
      </p>
    )
  }

  const data = query.data
  if (!data || data.schemaVersionId === null) {
    return <p className="text-sm text-muted">لا حقول إضافية لهذه الوثيقة.</p>
  }

  // A field hidden by its condition has nothing to say on this document.
  const shown = new Set(
    visibleFields(data.fields.map((entry) => entry.field), toFieldValueMap(data.fields)).map(
      (field) => field.key,
    ),
  )

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2 text-sm text-muted">
          <span>النسخة المثبّتة للوثيقة</span>
          <Badge variant="outline">
            <span dir="ltr">v{data.versionNumber}</span>
          </Badge>
        </div>
        {canEdit && !editing && (
          <Button type="button" size="sm" variant="outline" onClick={() => setEditing(true)}>
            <Pencil className="size-4" aria-hidden />
            تعديل القيم
          </Button>
        )}
      </div>

      {lockedReason && <p className="text-sm text-muted">{lockedReason}</p>}

      {editing ? (
        <EditFieldValuesForm documentId={documentId} fields={data} onDone={() => setEditing(false)} />
      ) : (
        <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
          {data.fields.filter((entry) => shown.has(entry.field.key)).map((entry) => (
            <div key={entry.field.documentFieldId} className="flex flex-col gap-0.5">
              <dt className="text-xs text-muted">
                {entry.field.labelAr}
                {entry.field.isRequired && <span className="text-danger"> *</span>}
              </dt>
              <dd className="whitespace-pre-wrap text-sm text-foreground">{formatFieldValue(entry.field, entry.value)}</dd>
            </div>
          ))}
        </dl>
      )}
    </section>
  )
}
