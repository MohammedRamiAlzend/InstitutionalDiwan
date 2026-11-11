import { useMemo, useState } from 'react'
import { Gavel, Paperclip, Route, Scale } from 'lucide-react'
import { ArchiveDocumentDialog } from '@/features/archive-manage'
import {
  AttachDecisionFileDialog,
  CancelDecisionDialog,
  IssueDecisionDialog,
  RecordAppealDialog,
  RecordAppealOutcomeDialog,
} from '@/features/decision-manage'
import { OpenMovementDialog } from '@/features/movement-routing'
import { useDepartmentOptions } from '@/entities/department'
import {
  DecisionStatusBadge,
  useDocumentDecisions,
  useDocumentNextAction,
  type DecisionAppealResponse,
  type DocumentDecisionResponse,
} from '@/entities/document-decision'
import { useSessionStore } from '@/entities/user'
import { tokenStorage, toArabicErrorMessage } from '@/shared/api'
import {
  appealOutcomeLabel,
  APPEAL_OUTCOME,
  decisionEffectLabel,
  DECISION_STATUS,
  isDecisionInForce,
  Permissions,
  USER_SCOPE_TYPE,
} from '@/shared/config'
import { decodeJwtPayload, formatDate, useTableState } from '@/shared/lib'
import {
  Button,
  DataTable,
  GuideChip,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

interface DocumentDecisionsPanelProps {
  documentId: string
  documentTitle: string
  documentTypeId: number
  owningDepartmentId: string
  canIssue: boolean
  canMove: boolean
  canArchive: boolean
}

interface OutcomeTarget {
  decisionId: string
  appealId: string
  decisionNumber: string
}

interface SuggestedMovement {
  destinationDepartmentId: string
  movementTypeId: number
}

function isSystemScope(): boolean {
  return decodeJwtPayload(tokenStorage.get())?.scope_type === String(USER_SCOPE_TYPE.System)
}

function AppealLines({
  appeals,
  departmentName,
}: {
  appeals: DecisionAppealResponse[]
  departmentName: (departmentId: string | null) => string
}) {
  if (appeals.length === 0) return <span>—</span>
  return (
    <ul className="flex flex-col gap-1">
      {appeals.map((appeal) => (
        <li key={appeal.appealId}>
          <span dir="ltr">{formatDate(appeal.appealDate)}</span>
          {' · '}
          {departmentName(appeal.appellantDepartmentId)}
          {' · '}
          {appealOutcomeLabel(appeal.outcomeId)}
          {appeal.referenceNumber ? (
            <>
              {' · '}
              <span dir="ltr" className="font-mono">
                {appeal.referenceNumber}
              </span>
            </>
          ) : null}
          {appeal.notes ? ` · ${appeal.notes}` : ''}
        </li>
      ))}
    </ul>
  )
}

const NEXT_ACTION_LABEL = {
  refer: 'إحالة إلى الوجهة',
  'send-for-archive': 'إرسال للحفظ',
  archive: 'أرشفة',
} as const

/** The decision in force, its history, and the one suggested next step. */
export function DocumentDecisionsPanel({
  documentId,
  documentTitle,
  documentTypeId,
  owningDepartmentId,
  canIssue,
  canMove,
  canArchive,
}: DocumentDecisionsPanelProps) {
  const decisionsQuery = useDocumentDecisions(documentId)
  const nextQuery = useDocumentNextAction(documentId)
  const departmentsQuery = useDepartmentOptions()
  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManage = hasPermission(Permissions.DecisionManage)
  const systemScope = isSystemScope()

  const table = useTableState({ defaultSortKey: 'recordedAt', defaultSortDirection: 'desc' })
  const [issueOpen, setIssueOpen] = useState(false)
  const [archiveOpen, setArchiveOpen] = useState(false)
  const [movement, setMovement] = useState<SuggestedMovement | null>(null)
  const [cancelTarget, setCancelTarget] = useState<DocumentDecisionResponse | null>(null)
  const [attachTarget, setAttachTarget] = useState<DocumentDecisionResponse | null>(null)
  const [appealTarget, setAppealTarget] = useState<DocumentDecisionResponse | null>(null)
  const [outcomeTarget, setOutcomeTarget] = useState<OutcomeTarget | null>(null)

  const departmentName = useMemo(() => {
    const names = new Map(
      (departmentsQuery.data ?? []).map((department) => [department.departmentId, department.nameAr]),
    )
    return (departmentId: string | null) => (departmentId ? names.get(departmentId) ?? departmentId : '—')
  }, [departmentsQuery.data])

  const decisions = decisionsQuery.data ?? []
  const inForce = decisions.find((decision) => isDecisionInForce(decision.statusId))
  const next = nextQuery.data
  const nextKind = next?.kind ?? null
  const pendingAppeal = inForce?.appeals.find((appeal) => appeal.outcomeId === APPEAL_OUTCOME.Pending)
  const canRunNext =
    nextKind === 'archive' ? canArchive : nextKind === 'refer' || nextKind === 'send-for-archive' ? canMove : false

  const runNext = () => {
    if (!next?.kind) return
    if (next.kind === 'archive') {
      setArchiveOpen(true)
      return
    }
    if (next.destinationDepartmentId && next.movementTypeId) {
      setMovement({
        destinationDepartmentId: next.destinationDepartmentId,
        movementTypeId: next.movementTypeId,
      })
    }
  }

  const columns = useMemo<DataTableColumn<DocumentDecisionResponse>[]>(
    () => [
      {
        id: 'number',
        header: 'الرقم',
        hideable: false,
        sortable: true,
        sortValue: (row) => row.decisionNumber,
        cell: (row) => (
          <span dir="ltr" className="font-mono text-sm">
            {row.decisionNumber}
          </span>
        ),
      },
      {
        id: 'type',
        header: 'النوع',
        sortable: true,
        sortValue: (row) => row.typeNameAr,
        cell: (row) => row.typeNameAr,
      },
      {
        id: 'date',
        header: 'التاريخ',
        sortable: true,
        sortValue: (row) => row.decisionDate,
        cell: (row) => <span dir="ltr">{formatDate(row.decisionDate)}</span>,
      },
      {
        id: 'effect',
        header: 'الأثر',
        sortable: true,
        sortValue: (row) => row.effectId,
        cell: (row) => decisionEffectLabel(row.effectId),
      },
      {
        id: 'status',
        header: 'الحالة',
        sortable: true,
        sortValue: (row) => row.statusId,
        cell: (row) => <DecisionStatusBadge statusId={row.statusId} />,
      },
      {
        id: 'issuer',
        header: 'الديوان المُصدر',
        cell: (row) => departmentName(row.issuingDepartmentId),
      },
      {
        id: 'appeals',
        header: 'الاعتراض',
        cell: (row) => <AppealLines appeals={row.appeals} departmentName={departmentName} />,
      },
    ],
    [departmentName],
  )

  const rowMenu = (row: DocumentDecisionResponse): MenuEntry[] => {
    const items: MenuEntry[] = []
    const pending = row.appeals.find((appeal) => appeal.outcomeId === APPEAL_OUTCOME.Pending)
    if (canManage && row.isAppealable && row.statusId === DECISION_STATUS.Issued) {
      items.push({
        id: 'appeal',
        label: 'تسجيل اعتراض',
        icon: <Scale className="size-4" aria-hidden />,
        onSelect: () => setAppealTarget(row),
      })
    }
    if (canManage && pending) {
      items.push({
        id: 'outcome',
        label: 'تسجيل النتيجة',
        onSelect: () =>
          setOutcomeTarget({
            decisionId: row.decisionId,
            appealId: pending.appealId,
            decisionNumber: row.decisionNumber,
          }),
      })
    }
    if (canManage && row.statusId !== DECISION_STATUS.Canceled) {
      items.push({
        id: 'attach',
        label: 'ربط نسخة',
        icon: <Paperclip className="size-4" aria-hidden />,
        onSelect: () => setAttachTarget(row),
      })
    }
    if (canManage && systemScope && row.statusId !== DECISION_STATUS.Canceled) {
      items.push({
        id: 'cancel',
        label: 'إلغاء الإدخال',
        danger: true,
        onSelect: () => setCancelTarget(row),
      })
    }
    return items
  }

  return (
    <div className="flex flex-col gap-4">
      {inForce && (
        <section className="flex flex-col gap-2 rounded-xl border border-border bg-surface p-4">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 className="flex items-center gap-2 text-sm font-semibold">
              <Scale className="size-4 text-accent" aria-hidden />
              القرار النافذ
            </h2>
            <DecisionStatusBadge statusId={inForce.statusId} />
          </div>
          <p className="text-sm">
            {inForce.typeNameAr}
            <span dir="ltr" className="ms-2 font-mono">
              {inForce.decisionNumber}
            </span>
          </p>
          <p className="text-sm text-muted">
            {decisionEffectLabel(inForce.effectId)}
            {inForce.destinationDepartmentId
              ? ` · الوجهة ${departmentName(inForce.destinationDepartmentId)}`
              : ''}
            {inForce.summary ? ` · ${inForce.summary}` : ''}
          </p>
          {inForce.appeals.length > 0 && (
            <div className="text-sm">
              <AppealLines appeals={inForce.appeals} departmentName={departmentName} />
            </div>
          )}
        </section>
      )}

      <div className="flex flex-wrap items-center gap-2">
        {canIssue && (
          <Button type="button" onClick={() => setIssueOpen(true)}>
            <Gavel className="size-4" aria-hidden />
            إصدار قرار
          </Button>
        )}
        {canManage && inForce && inForce.isAppealable && inForce.statusId === DECISION_STATUS.Issued && (
          <>
            <Button type="button" variant="outline" onClick={() => setAppealTarget(inForce)}>
              <Scale className="size-4" aria-hidden />
              تسجيل اعتراض
            </Button>
            <GuideChip id="document-details.record-appeal" />
          </>
        )}
        {canManage && inForce && pendingAppeal && (
          <Button
            type="button"
            variant="outline"
            onClick={() =>
              setOutcomeTarget({
                decisionId: inForce.decisionId,
                appealId: pendingAppeal.appealId,
                decisionNumber: inForce.decisionNumber,
              })
            }
          >
            تسجيل النتيجة
          </Button>
        )}
        {canRunNext && nextKind && (
          <>
            <Button type="button" variant="outline" onClick={runNext}>
              <Route className="size-4" aria-hidden />
              {NEXT_ACTION_LABEL[nextKind]}
            </Button>
            <GuideChip id="document-details.next-action" />
          </>
        )}
      </div>

      <DataTable
        tableId="document-decisions"
        columns={columns}
        rows={decisions}
        rowKey={(row) => row.decisionId}
        mode="client"
        caption="سجل قرارات الوثيقة"
        isLoading={decisionsQuery.isLoading}
        error={decisionsQuery.error}
        onRetry={() => void decisionsQuery.refetch()}
        onRefresh={() => void decisionsQuery.refetch()}
        isFetching={decisionsQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={decisions.length}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        searchPlaceholder="ابحث برقم القرار أو نوعه"
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        rowSearchText={(row) => `${row.decisionNumber} ${row.typeNameAr} ${row.summary ?? ''}`}
        rowMenu={canManage ? rowMenu : undefined}
        rowLabel={(row) => row.decisionNumber}
        emptyTitle="لا قرارات على هذه الوثيقة"
        emptyDescription={
          decisionsQuery.isError
            ? toArabicErrorMessage(decisionsQuery.error)
            : 'يظهر القرار هنا بعد أن يصدره الديوان الذي يحمل الوثيقة.'
        }
      />

      {issueOpen && (
        <IssueDecisionDialog
          documentId={documentId}
          documentTypeId={documentTypeId}
          open
          onOpenChange={setIssueOpen}
        />
      )}
      {movement && (
        <OpenMovementDialog
          key={`${movement.destinationDepartmentId}:${movement.movementTypeId}`}
          documentId={documentId}
          documentTitle={documentTitle}
          open
          onOpenChange={(open) => {
            if (!open) setMovement(null)
          }}
          initialDestinationId={movement.destinationDepartmentId}
          initialMovementTypeId={movement.movementTypeId}
        />
      )}
      {archiveOpen && (
        <ArchiveDocumentDialog
          documentId={documentId}
          documentTitle={documentTitle}
          open
          onOpenChange={setArchiveOpen}
        />
      )}
      {cancelTarget && (
        <CancelDecisionDialog
          documentId={documentId}
          decisionId={cancelTarget.decisionId}
          decisionNumber={cancelTarget.decisionNumber}
          open
          onOpenChange={(open) => {
            if (!open) setCancelTarget(null)
          }}
        />
      )}
      {attachTarget && (
        <AttachDecisionFileDialog
          documentId={documentId}
          decisionId={attachTarget.decisionId}
          open
          onOpenChange={(open) => {
            if (!open) setAttachTarget(null)
          }}
        />
      )}
      {appealTarget && (
        <RecordAppealDialog
          documentId={documentId}
          decisionId={appealTarget.decisionId}
          decisionNumber={appealTarget.decisionNumber}
          owningDepartmentId={owningDepartmentId}
          open
          onOpenChange={(open) => {
            if (!open) setAppealTarget(null)
          }}
        />
      )}
      {outcomeTarget && (
        <RecordAppealOutcomeDialog
          documentId={documentId}
          decisionId={outcomeTarget.decisionId}
          appealId={outcomeTarget.appealId}
          decisionNumber={outcomeTarget.decisionNumber}
          open
          onOpenChange={(open) => {
            if (!open) setOutcomeTarget(null)
          }}
        />
      )}
    </div>
  )
}
