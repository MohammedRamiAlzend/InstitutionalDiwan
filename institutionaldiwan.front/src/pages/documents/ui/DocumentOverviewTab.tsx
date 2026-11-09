import type { ReactNode } from 'react'
import {
  DocumentStatusBadge,
  OfficialNumberTag,
  type DocumentParticipantResponse,
  type DocumentResponse,
  type DocumentStatusHistoryResponse,
  type DocumentTimelineEntryResponse,
} from '@/entities/document'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { FEATURES } from '@/shared/config'
import { formatDate, formatDateTime } from '@/shared/lib'
import { Badge } from '@/shared/ui'
import { DocumentTimeline } from './DocumentTimeline'
import { DepartmentLabel } from '@/entities/department'
import { UserLabel } from '@/entities/user'

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
          <Field label="الحالة">
            <DocumentStatusBadge statusId={document.statusId} />
          </Field>
          <Field label="رقم النظام">
            <OfficialNumberTag officialNumberText={document.officialNumberText} />
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
              <DepartmentLabel departmentId={document.owningDepartmentId} />
            )}
          </Field>
          <Field label="تاريخ الإنشاء">
            <span dir="ltr" className="text-sm text-foreground">
              {formatDateTime(document.createdAt)}
            </span>
          </Field>
          <Field label="أنشأها">
            <UserLabel userId={document.createdByUserId} />
          </Field>
        </dl>

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
                  <UserLabel userId={entry.changedByUserId} />
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="rounded-xl border border-border bg-surface p-5">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="text-base font-semibold text-foreground">الخط الزمني</h2>
          <Badge variant="outline">{FEATURES.participants ? 'حالة · مشاركون · أرقام' : 'حالة · أرقام'}</Badge>
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

