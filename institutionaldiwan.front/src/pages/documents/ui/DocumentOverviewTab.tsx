import type { ReactNode } from 'react'
import {
  DocumentDirectionBadge,
  DocumentStatusBadge,
  OfficialNumberTag,
  type DocumentParticipantResponse,
  type DocumentResponse,
  type DocumentStatusHistoryResponse,
  type DocumentTimelineEntryResponse,
} from '@/entities/document'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { formatDate, formatDateTime, formatShortId } from '@/shared/lib'
import { Badge } from '@/shared/ui'
import { DocumentTimeline } from './DocumentTimeline'

interface DocumentOverviewTabProps {
  document: DocumentResponse
  statusHistory: DocumentStatusHistoryResponse[]
  timeline: DocumentTimelineEntryResponse[]
  timelineLoading: boolean
  participantsById: Map<string, DocumentParticipantResponse>
  departmentNames: Map<string, string>
}

/** Details tab: core metadata, the recorded status transitions, and the audit timeline. */
export function DocumentOverviewTab({
  document,
  statusHistory,
  timeline,
  timelineLoading,
  participantsById,
  departmentNames,
}: DocumentOverviewTabProps) {
  const owningDepartmentName = departmentNames.get(document.owningDepartmentId)

  return (
    <div className="flex flex-col gap-6">
      <section className="rounded-xl border border-border bg-surface p-5">
        <h2 className="text-base font-semibold text-foreground">بيانات الوثيقة</h2>
        <dl className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <Field label="نوع الوثيقة">
            <LookupLabel lookup={LOOKUP_SLUGS.DocumentTypes} id={document.documentTypeId} />
          </Field>
          <Field label="الاتجاه">
            <DocumentDirectionBadge directionId={document.directionId} />
          </Field>
          <Field label="الحالة">
            <DocumentStatusBadge statusId={document.statusId} />
          </Field>
          <Field label="مستوى السرية">
            <LookupLabel
              lookup={LOOKUP_SLUGS.DocumentConfidentialityLevels}
              id={document.confidentialityLevelId}
            />
          </Field>
          <Field label="درجة الأولوية">
            <LookupLabel
              lookup={LOOKUP_SLUGS.DocumentPriorityLevels}
              id={document.priorityLevelId}
            />
          </Field>
          <Field label="الرقم الرسمي">
            <OfficialNumberTag officialNumberText={document.officialNumberText} />
          </Field>
          <Field label="تاريخ الوثيقة">
            <span dir="ltr" className="text-sm text-foreground">
              {formatDate(document.documentDate)}
            </span>
          </Field>
          <Field label="تاريخ التسجيل">
            <span dir="ltr" className="text-sm text-foreground">
              {formatDate(document.registrationDate)}
            </span>
          </Field>
          <Field label="القسم المالك">
            {owningDepartmentName ? (
              <span className="text-sm text-foreground">{owningDepartmentName}</span>
            ) : (
              <ShortId value={document.owningDepartmentId} />
            )}
          </Field>
          <Field label="تاريخ الإنشاء">
            <span dir="ltr" className="text-sm text-foreground">
              {formatDateTime(document.createdAt)}
            </span>
          </Field>
          <Field label="أنشأها">
            <ShortId value={document.createdByUserId} />
          </Field>
        </dl>

        {document.subjectSummary && (
          <div className="mt-4 flex flex-col gap-1 border-t border-border pt-4">
            <span className="text-xs text-muted">ملخص الموضوع</span>
            <p className="text-sm leading-7 text-foreground">{document.subjectSummary}</p>
          </div>
        )}
      </section>

      <section className="rounded-xl border border-border bg-surface p-5">
        <h2 className="text-base font-semibold text-foreground">سجل الحالة</h2>
        {statusHistory.length === 0 ? (
          <p className="mt-3 text-sm text-muted">لا توجد انتقالات حالة مسجلة بعد.</p>
        ) : (
          <ul className="mt-4 flex flex-col gap-3">
            {statusHistory.map((entry) => (
              <li
                key={entry.historyId}
                className="flex flex-col gap-1 rounded-lg border border-border bg-background px-4 py-3"
              >
                <div className="flex flex-wrap items-center gap-1.5">
                  {entry.fromStatusId !== null && (
                    <>
                      <DocumentStatusBadge statusId={entry.fromStatusId} />
                      <span className="text-xs text-muted">إلى</span>
                    </>
                  )}
                  <DocumentStatusBadge statusId={entry.toStatusId} />
                </div>
                {entry.reason && (
                  <p className="text-sm text-foreground">السبب: {entry.reason}</p>
                )}
                <div className="flex flex-wrap items-center gap-2 text-xs text-muted">
                  <span>{formatDateTime(entry.changedAt)}</span>
                  <span aria-hidden>·</span>
                  <span>بواسطة</span>
                  <ShortId value={entry.changedByUserId} />
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="rounded-xl border border-border bg-surface p-5">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="text-base font-semibold text-foreground">الخط الزمني</h2>
          <Badge variant="outline">حالة · مشاركون · أرقام</Badge>
        </div>
        <div className="mt-4">
          <DocumentTimeline
            entries={timeline}
            participantsById={participantsById}
            departmentNames={departmentNames}
            isLoading={timelineLoading}
          />
        </div>
      </section>
    </div>
  )
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="text-xs text-muted">{label}</dt>
      <dd className="flex items-center">{children}</dd>
    </div>
  )
}

function ShortId({ value }: { value: string | null | undefined }) {
  if (!value) return <span className="text-sm text-muted">—</span>

  return (
    <span dir="ltr" title={value} className="font-mono text-xs text-muted">
      {formatShortId(value)}
    </span>
  )
}
