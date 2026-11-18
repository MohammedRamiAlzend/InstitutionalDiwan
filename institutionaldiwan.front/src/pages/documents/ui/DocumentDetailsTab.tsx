import { useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  Activity,
  Building2,
  CalendarDays,
  Clock,
  FileText,
  Hash,
  History,
  UserRound,
} from 'lucide-react'
import { DepartmentLabel } from '@/entities/department'
import {
  DocumentStatusBadge,
  OfficialNumberTag,
  type DocumentParticipantResponse,
  type DocumentResponse,
  type DocumentStatusHistoryResponse,
  type DocumentTimelineEntryResponse,
} from '@/entities/document'
import { DOCUMENT_STATUS, LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { UserLabel } from '@/entities/user'
import { type LabelKey } from '@/shared/config'
import { cn, formatDate, formatDateTime, t } from '@/shared/lib'
import { CollapsibleSection, GuideRegion, Label } from '@/shared/ui'
import { DepartmentRecordsPanel } from '@/widgets/department-records-panel'
import { DocumentFieldsPanel } from '@/widgets/document-fields-panel'
import { DocumentGrantsSection } from './DocumentGrantsSection'
import { DocumentNumbersSection } from './DocumentNumbersSection'
import { DocumentTimeline } from './DocumentTimeline'

interface DocumentDetailsTabProps {
  document: DocumentResponse
  statusHistory: DocumentStatusHistoryResponse[]
  timeline: DocumentTimelineEntryResponse[]
  timelineLoading: boolean
  participantsById: Map<string, DocumentParticipantResponse>
  departmentNames: Map<string, string>
  canEdit: boolean
  canViewNumbering: boolean
  canManageNumbering: boolean
  canManageGrants: boolean
  hasDynamicFields: boolean
  hasDepartmentRecords: boolean
}

interface OpenSections {
  numbers: boolean
  grants: boolean
  fields: boolean
  logs: boolean
}

/**
 * Details tab: a sticky «بيانات الوثيقة» summary beside a column of collapsible sections
 * (numbers, grants, fields, logs) with chip navigation and scrollspy. The «السجلات» section
 * holds the unified timeline plus the department records, which stay a separate editable block.
 */
export function DocumentDetailsTab({
  document,
  statusHistory,
  timeline,
  timelineLoading,
  participantsById,
  departmentNames,
  canEdit,
  canViewNumbering,
  canManageNumbering,
  canManageGrants,
  hasDynamicFields,
  hasDepartmentRecords,
}: DocumentDetailsTabProps) {
  const isDraft = document.statusId === DOCUMENT_STATUS.Draft
  const isArchived = document.statusId === DOCUMENT_STATUS.Archived
  const owningDepartmentName = departmentNames.get(document.owningDepartmentId)

  const [openSections, setOpenSections] = useState<OpenSections>({
    numbers: true,
    grants: true,
    fields: true,
    logs: true,
  })
  const [activeSection, setActiveSection] = useState('data')

  const sections = useMemo(() => {
    const list: { id: string; labelKey: LabelKey }[] = [
      { id: 'data', labelKey: 'docdetails.section.data' },
    ]
    if (canViewNumbering) list.push({ id: 'numbers', labelKey: 'docdetails.section.numbers' })
    if (canManageGrants) list.push({ id: 'grants', labelKey: 'docdetails.section.grants' })
    if (hasDynamicFields) list.push({ id: 'fields', labelKey: 'docdetails.section.fields' })
    list.push({ id: 'logs', labelKey: 'docdetails.section.logs' })
    return list
  }, [canViewNumbering, canManageGrants, hasDynamicFields])

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            setActiveSection(entry.target.id.replace('document-details-', ''))
          }
        }
      },
      { rootMargin: '-25% 0px -65% 0px' },
    )
    for (const section of sections) {
      const element = window.document.getElementById(`document-details-${section.id}`)
      if (element) observer.observe(element)
    }
    return () => observer.disconnect()
  }, [sections])

  const jumpTo = (id: string) => {
    if (id === 'numbers' || id === 'grants' || id === 'fields' || id === 'logs') {
      setOpenSections((prev) => ({ ...prev, [id]: true }))
    }
    requestAnimationFrame(() => {
      window.document.getElementById(`document-details-${id}`)?.scrollIntoView({
        behavior:
          window.document.documentElement.dataset.motion === 'off' ? 'auto' : 'smooth',
        block: 'start',
      })
    })
  }

  return (
    <div className="flex flex-col gap-6 xl:grid xl:grid-cols-[minmax(0,1fr)_20rem] xl:items-start">
      <aside
        id="document-details-data"
        className="order-1 scroll-mt-24 xl:order-2 xl:sticky xl:top-20"
      >
        <section className="rounded-xl border border-border bg-surface p-5">
          <div className="flex items-center gap-2">
            <FileText className="size-4 text-accent" aria-hidden />
            <h2 className="text-base font-semibold text-foreground"><Label k="docdetails.data-title" /></h2>
          </div>
          <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-3 xl:grid-cols-1 xl:gap-y-0 xl:divide-y xl:divide-border">
            <InfoRow icon={<FileText className="size-3.5" aria-hidden />} labelKey="docdetails.field.type">
              <LookupLabel lookup={LOOKUP_SLUGS.DocumentTypes} id={document.documentTypeId} />
            </InfoRow>
            <InfoRow icon={<Activity className="size-3.5" aria-hidden />} labelKey="docdetails.field.status">
              <DocumentStatusBadge statusId={document.statusId} />
            </InfoRow>
            <InfoRow
              icon={<Hash className="size-3.5" aria-hidden />}
              labelKey="docdetails.field.system-number"
              className="col-span-2 xl:col-span-1"
            >
              <OfficialNumberTag officialNumberText={document.officialNumberText} />
            </InfoRow>
            <InfoRow icon={<CalendarDays className="size-3.5" aria-hidden />} labelKey="docdetails.field.registration-date">
              <span dir="ltr">{formatDate(document.registrationDate)}</span>
            </InfoRow>
            <InfoRow icon={<Building2 className="size-3.5" aria-hidden />} labelKey="docdetails.field.owning-department">
              {owningDepartmentName ? (
                owningDepartmentName
              ) : (
                <DepartmentLabel departmentId={document.owningDepartmentId} />
              )}
            </InfoRow>
            <InfoRow icon={<Clock className="size-3.5" aria-hidden />} labelKey="docdetails.field.created-at">
              <span dir="ltr">{formatDateTime(document.createdAt)}</span>
            </InfoRow>
            <InfoRow icon={<UserRound className="size-3.5" aria-hidden />} labelKey="docdetails.field.created-by">
              <UserLabel userId={document.createdByUserId} />
            </InfoRow>
          </dl>
        </section>
      </aside>

      <div className="order-2 flex min-w-0 flex-col gap-6 xl:order-1">
        <nav aria-label={t('docdetails.sections-aria')} className="flex flex-wrap items-center gap-2">
          {sections.map((section) => (
            <button
              key={section.id}
              type="button"
              onClick={() => jumpTo(section.id)}
              aria-current={activeSection === section.id ? 'true' : undefined}
              className={cn(
                'rounded-full border px-3 py-1.5 text-xs transition-colors',
                activeSection === section.id
                  ? 'border-accent bg-accent/10 font-medium text-accent'
                  : 'border-border bg-surface text-muted hover:border-accent/40 hover:text-foreground',
              )}
            >
              <Label k={section.labelKey} />
            </button>
          ))}
        </nav>

        {canViewNumbering && (
          <div id="document-details-numbers" className="scroll-mt-24">
            <GuideRegion id="document-details.section-numbers">
              <DocumentNumbersSection
                documentId={document.documentId}
                documentStatusId={document.statusId}
                canManage={canManageNumbering && !isArchived}
                departmentNames={departmentNames}
                open={openSections.numbers}
                onOpenChange={(open) =>
                  setOpenSections((prev) => ({ ...prev, numbers: open }))
                }
              />
            </GuideRegion>
          </div>
        )}

        {canManageGrants && (
          <div id="document-details-grants" className="scroll-mt-24">
            <GuideRegion id="document-details.section-grants">
              <DocumentGrantsSection
                documentId={document.documentId}
                open={openSections.grants}
                onOpenChange={(open) =>
                  setOpenSections((prev) => ({ ...prev, grants: open }))
                }
              />
            </GuideRegion>
          </div>
        )}

        {hasDynamicFields && (
          <div id="document-details-fields" className="scroll-mt-24">
            <GuideRegion id="document-details.section-fields">
              <CollapsibleSection
                title={t('docdetails.fields-title')}
                icon={<FileText className="size-4" aria-hidden />}
                hint={t('docdetails.fields-hint')}
                open={openSections.fields}
                onOpenChange={(open) =>
                  setOpenSections((prev) => ({ ...prev, fields: open }))
                }
              >
                <DocumentFieldsPanel
                  documentId={document.documentId}
                  canEdit={canEdit && isDraft}
                  lockedReason={
                    canEdit && !isDraft
                      ? t('docdetails.fields-locked')
                      : undefined
                  }
                />
              </CollapsibleSection>
            </GuideRegion>
          </div>
        )}

        <div id="document-details-logs" className="scroll-mt-24">
          <GuideRegion id="document-details.section-logs">
            <CollapsibleSection
              title={t('docdetails.section.logs')}
              icon={<History className="size-4" aria-hidden />}
              hint={t('docdetails.logs-hint')}
              open={openSections.logs}
              onOpenChange={(open) =>
                setOpenSections((prev) => ({ ...prev, logs: open }))
              }
            >
              <div className="flex flex-col gap-6">
                <DocumentTimeline
                  documentId={document.documentId}
                  statusHistory={statusHistory}
                  entries={timeline}
                  participantsById={participantsById}
                  departmentNames={departmentNames}
                  isLoading={timelineLoading}
                />

                {hasDepartmentRecords && (
                  <GuideRegion id="document-details.section-department-records">
                    <div className="flex flex-col gap-2 border-t border-border pt-4">
                      <h3 className="text-sm font-semibold text-foreground"><Label k="docdetails.diwan-records-title" /></h3>
                      <DepartmentRecordsPanel documentId={document.documentId} />
                    </div>
                  </GuideRegion>
                )}
              </div>
            </CollapsibleSection>
          </GuideRegion>
        </div>
      </div>
    </div>
  )
}

function InfoRow({
  icon,
  labelKey,
  className,
  children,
}: {
  icon: ReactNode
  labelKey: LabelKey
  className?: string
  children: ReactNode
}) {
  return (
    <div
      className={cn(
        'flex flex-col gap-0.5 xl:py-2.5 xl:first:pt-0 xl:last:pb-0',
        className,
      )}
    >
      <dt className="flex items-center gap-1.5 text-xs text-muted">
        <span className="text-accent">{icon}</span>
        <Label k={labelKey} />
      </dt>
      <dd className="text-sm text-foreground">{children}</dd>
    </div>
  )
}