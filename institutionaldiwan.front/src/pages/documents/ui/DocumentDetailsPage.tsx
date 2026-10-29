import { useMemo, useState } from 'react'
import { Link, Navigate, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { ArrowRight, Ban, FilePenLine, Hash, UserPlus } from 'lucide-react'
import { useDepartmentOptions } from '@/entities/department'
import {
  DocumentDirectionBadge,
  DocumentStatusBadge,
  OfficialNumberTag,
  useDocumentDetails,
  useDocumentTimeline,
} from '@/entities/document'
import { DOCUMENT_STATUS } from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import { RequirePermission } from '@/features/auth'
import {
  AddParticipantDialog,
  CancelDocumentDialog,
  EditDraftDialog,
  RegisterDraftDialog,
} from '@/features/document-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions, ROUTES } from '@/shared/config'
import { cn } from '@/shared/lib'
import { Badge, Button, PageHeader, Skeleton } from '@/shared/ui'
import { DocumentGrantsTab } from './DocumentGrantsTab'
import { DocumentMovementsTab } from './DocumentMovementsTab'
import { DocumentNumbersTab } from './DocumentNumbersTab'
import { DocumentOverviewTab } from './DocumentOverviewTab'
import { DocumentParticipantsTab } from './DocumentParticipantsTab'

const TABS = [
  { key: 'details', label: 'التفاصيل', available: true },
  { key: 'participants', label: 'المشاركون', available: true },
  { key: 'numbers', label: 'الأرقام', available: true },
  { key: 'movements', label: 'الحركات', available: true },
  { key: 'files', label: 'الملفات', available: false },
  { key: 'grants', label: 'التصاريح', available: true },
] as const

type TabKey = (typeof TABS)[number]['key']
type AvailableTabKey = 'details' | 'participants' | 'numbers' | 'movements' | 'grants'

/** Unknown, not-yet-available, or permission-hidden tabs fall back to the details tab. */
function resolveTab(
  raw: string | null,
  canManageGrants: boolean,
  canViewNumbering: boolean,
): AvailableTabKey {
  if (raw === 'participants') return 'participants'
  if (raw === 'numbers' && canViewNumbering) return 'numbers'
  if (raw === 'movements') return 'movements'
  if (raw === 'grants' && canManageGrants) return 'grants'
  return 'details'
}

/** `/documents/:documentId` — document details and the Phase 3 lifecycle actions. */
export function DocumentDetailsPage() {
  return (
    <RequirePermission permission={Permissions.DocumentView}>
      <DocumentDetailsView />
    </RequirePermission>
  )
}

function DocumentDetailsView() {
  const { documentId } = useParams<{ documentId: string }>()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canEdit = hasPermission(Permissions.DocumentEdit)
  const canCreate = hasPermission(Permissions.DocumentCreate)
  const canManageGrants = hasPermission(Permissions.AccessGrantManage)
  const canViewNumbering = hasPermission(Permissions.NumberingView)
  const canManageNumbering = hasPermission(Permissions.NumberingManage)

  const activeTab = resolveTab(
    searchParams.get('tab'),
    canManageGrants,
    canViewNumbering,
  )

  const detailsQuery = useDocumentDetails(documentId)
  const timelineQuery = useDocumentTimeline(documentId)
  const departmentsQuery = useDepartmentOptions()

  const [editOpen, setEditOpen] = useState(false)
  const [registerOpen, setRegisterOpen] = useState(false)
  const [participantOpen, setParticipantOpen] = useState(false)
  const [cancelOpen, setCancelOpen] = useState(false)

  const departmentNames = useMemo(
    () =>
      new Map(
        (departmentsQuery.data ?? []).map((department) => [
          department.departmentId,
          department.nameAr,
        ]),
      ),
    [departmentsQuery.data],
  )

  const details = detailsQuery.data
  const participantsById = useMemo(
    () =>
      new Map(
        (details?.participants ?? []).map((participant) => [
          participant.participantId,
          participant,
        ]),
      ),
    [details?.participants],
  )

  if (!documentId) {
    return <Navigate to={ROUTES.DOCUMENTS} replace />
  }

  if (detailsQuery.isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-10 w-1/2" />
        <Skeleton className="h-6 w-1/3" />
        <Skeleton className="h-64 w-full" />
      </div>
    )
  }

  if (detailsQuery.isError || !details) {
    return (
      <div className="flex flex-col items-center gap-3 rounded-xl border border-border bg-surface px-6 py-12 text-center">
        <p className="text-sm font-medium text-danger">
          {toArabicErrorMessage(detailsQuery.error)}
        </p>
        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" variant="outline" onClick={() => void detailsQuery.refetch()}>
            إعادة المحاولة
          </Button>
          <Button
            type="button"
            variant="ghost"
            onClick={() => navigate(ROUTES.DOCUMENTS)}
          >
            العودة إلى سجل الوثائق
          </Button>
        </div>
      </div>
    )
  }

  const document = details.document
  const isDraft = document.statusId === DOCUMENT_STATUS.Draft
  const isCanceled = document.statusId === DOCUMENT_STATUS.Canceled
  const isArchived = document.statusId === DOCUMENT_STATUS.Archived
  const canManageParticipants = canEdit && !isCanceled
  const canCancel = canEdit && !isCanceled && !isArchived

  const setTab = (tab: TabKey) => {
    setSearchParams({ tab })
  }

  return (
    <div className="flex flex-col gap-6">
      <Link
        to={ROUTES.DOCUMENTS}
        className="inline-flex w-fit items-center gap-1 text-sm text-muted transition-colors hover:text-foreground"
      >
        <ArrowRight className="size-4" aria-hidden />
        العودة إلى سجل الوثائق
      </Link>

      <PageHeader
        title={document.title}
        description={document.subjectSummary ?? undefined}
        badge={
          <div className="flex flex-wrap items-center gap-2">
            <DocumentStatusBadge statusId={document.statusId} />
            <DocumentDirectionBadge directionId={document.directionId} />
            <OfficialNumberTag officialNumberText={document.officialNumberText} />
          </div>
        }
        actions={
          <>
            {isDraft && canEdit && (
              <Button
                type="button"
                variant="outline"
                onClick={() => setEditOpen(true)}
              >
                <FilePenLine className="size-4" aria-hidden />
                تعديل المسودة
              </Button>
            )}
            {isDraft && canCreate && (
              <Button type="button" onClick={() => setRegisterOpen(true)}>
                <Hash className="size-4" aria-hidden />
                تسجيل برقم
              </Button>
            )}
            {canManageParticipants && (
              <Button
                type="button"
                variant="outline"
                onClick={() => setParticipantOpen(true)}
              >
                <UserPlus className="size-4" aria-hidden />
                إضافة مشارك
              </Button>
            )}
            {canCancel && (
              <Button
                type="button"
                variant="outline"
                className="text-danger"
                onClick={() => setCancelOpen(true)}
              >
                <Ban className="size-4" aria-hidden />
                إلغاء الوثيقة
              </Button>
            )}
          </>
        }
      />

      <nav
        aria-label="تبويبات الوثيقة"
        className="flex flex-wrap items-center gap-1 border-b border-border pb-1"
      >
        {TABS.filter(
          (tab) =>
            (tab.key !== 'grants' || canManageGrants) &&
            (tab.key !== 'numbers' || canViewNumbering),
        ).map((tab) => {
          const isActive = activeTab === tab.key

          return (
            <button
              key={tab.key}
              type="button"
              disabled={!tab.available}
              aria-current={isActive ? 'page' : undefined}
              onClick={() => setTab(tab.key)}
              className={cn(
                'inline-flex items-center gap-1.5 rounded-t-md px-3 py-2 text-sm transition-colors',
                isActive
                  ? 'border-b-2 border-accent font-semibold text-accent'
                  : 'text-muted hover:bg-surface-muted hover:text-foreground',
                !tab.available && 'cursor-not-allowed opacity-60 hover:bg-transparent',
              )}
            >
              {tab.label}
              {!tab.available && (
                <Badge variant="outline" className="text-[10px]">
                  قريبًا
                </Badge>
              )}
            </button>
          )
        })}
      </nav>

      {activeTab === 'details' && (
        <DocumentOverviewTab
          document={document}
          statusHistory={details.statusHistory}
          timeline={timelineQuery.data ?? []}
          timelineLoading={timelineQuery.isLoading}
          participantsById={participantsById}
          departmentNames={departmentNames}
        />
      )}
      {activeTab === 'participants' && (
        <DocumentParticipantsTab
          participants={details.participants}
          departmentNames={departmentNames}
          canAdd={canManageParticipants}
          onAdd={() => setParticipantOpen(true)}
        />
      )}
      {activeTab === 'numbers' && canViewNumbering && (
        <DocumentNumbersTab
          documentId={document.documentId}
          documentStatusId={document.statusId}
          canManage={canManageNumbering}
          departmentNames={departmentNames}
        />
      )}
      {activeTab === 'movements' && (
        <DocumentMovementsTab
          documentId={document.documentId}
          documentTitle={document.title}
          documentStatusId={document.statusId}
          canEdit={canEdit}
          departmentNames={departmentNames}
        />
      )}
      {activeTab === 'grants' && canManageGrants && (
        <DocumentGrantsTab documentId={document.documentId} />
      )}

      {editOpen && (
        <EditDraftDialog
          document={document}
          open
          onOpenChange={setEditOpen}
        />
      )}
      {registerOpen && (
        <RegisterDraftDialog
          document={document}
          open
          onOpenChange={setRegisterOpen}
        />
      )}
      {participantOpen && (
        <AddParticipantDialog
          documentId={document.documentId}
          open
          onOpenChange={setParticipantOpen}
        />
      )}
      {cancelOpen && (
        <CancelDocumentDialog
          documentId={document.documentId}
          documentTitle={document.title}
          open
          onOpenChange={setCancelOpen}
        />
      )}
    </div>
  )
}
