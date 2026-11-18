import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
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
  useJoinCandidates,
  type DecisionAppealResponse,
  type DocumentDecisionResponse,
} from '@/entities/document-decision'
import { useDocumentRelations } from '@/entities/document-relation'
import { useSessionStore } from '@/entities/user'
import { tokenStorage, toArabicErrorMessage } from '@/shared/api'
import {
  appealOutcomeLabel,
  APPEAL_OUTCOME,
  decisionEffectLabel,
  DECISION_EFFECT,
  DECISION_STATUS,
  DOCUMENT_RELATION_TYPE,
  isDecisionInForce,
  Permissions,
  USER_SCOPE_TYPE,
  type LabelKey,
} from '@/shared/config'
import { decodeJwtPayload, formatDate, t, useTableState } from '@/shared/lib'
import {
  Button,
  DataTable,
  GuideChip,
  Label,
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

const NEXT_ACTION_LABEL_KEYS: Record<string, LabelKey> = {
  refer: 'decisions.next.refer',
  'send-for-archive': 'decisions.next.send-for-archive',
  archive: 'decisions.next.archive',
  move: 'decisions.next.move',
}

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
  const candidatesQuery = useJoinCandidates(documentId)
  const relationsQuery = useDocumentRelations(documentId)
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
  const inForce = decisions.find(
    (decision) => decision.documentId === documentId && isDecisionInForce(decision.statusId),
  )
  const principalLink = (relationsQuery.data?.items ?? []).find(
    (relation) =>
      relation.type.relationTypeId === DOCUMENT_RELATION_TYPE.JoinedInto &&
      relation.direction === 'Outgoing',
  )
  const candidates = candidatesQuery.data ?? []
  const next = nextQuery.data
  const nextKind = next?.kind ?? null
  const pendingAppeal = inForce?.appeals.find((appeal) => appeal.outcomeId === APPEAL_OUTCOME.Pending)
  const canRunNext =
    nextKind === 'archive'
      ? canArchive
      : nextKind === 'refer' || nextKind === 'send-for-archive' || nextKind === 'move'
        ? canMove
        : false

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
        headerKey: 'decisions.panel.column.number',
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
        headerKey: 'decisions.panel.column.type',
        sortable: true,
        sortValue: (row) => row.typeNameAr,
        cell: (row) => (
          <span className="flex flex-col gap-1">
            <span>{row.typeNameAr}</span>
            {row.documentId !== documentId && (
              <span className="text-xs text-muted"><Label k="decisions.panel.from-principal" /></span>
            )}
          </span>
        ),
      },
      {
        id: 'date',
        headerKey: 'decisions.panel.column.date',
        sortable: true,
        sortValue: (row) => row.decisionDate,
        cell: (row) => <span dir="ltr">{formatDate(row.decisionDate)}</span>,
      },
      {
        id: 'effect',
        headerKey: 'decisions.panel.column.effect',
        sortable: true,
        sortValue: (row) => row.effectId,
        cell: (row) => decisionEffectLabel(row.effectId),
      },
      {
        id: 'status',
        headerKey: 'decisions.panel.column.status',
        sortable: true,
        sortValue: (row) => row.statusId,
        cell: (row) => <DecisionStatusBadge statusId={row.statusId} />,
      },
      {
        id: 'issuer',
        headerKey: 'decisions.panel.column.issuer',
        cell: (row) => departmentName(row.issuingDepartmentId),
      },
      {
        id: 'appeals',
        headerKey: 'decisions.panel.column.appeals',
        cell: (row) => <AppealLines appeals={row.appeals} departmentName={departmentName} />,
      },
    ],
    [departmentName, documentId],
  )

  const rowMenu = (row: DocumentDecisionResponse): MenuEntry[] => {
    if (row.documentId !== documentId) return []
    const items: MenuEntry[] = []
    const pending = row.appeals.find((appeal) => appeal.outcomeId === APPEAL_OUTCOME.Pending)
    if (canManage && row.isAppealable && row.statusId === DECISION_STATUS.Issued) {
      items.push({
        id: 'appeal',
        label: t('decisions.panel.record-appeal'),
        icon: <Scale className="size-4" aria-hidden />,
        onSelect: () => setAppealTarget(row),
      })
    }
    if (canManage && pending) {
      items.push({
        id: 'outcome',
        label: t('decisions.panel.record-outcome'),
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
        label: t('decisions.panel.attach'),
        icon: <Paperclip className="size-4" aria-hidden />,
        onSelect: () => setAttachTarget(row),
      })
    }
    if (canManage && systemScope && row.statusId !== DECISION_STATUS.Canceled) {
      items.push({
        id: 'cancel',
        label: t('decisions.panel.cancel'),
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
              <Label k="decisions.panel.in-force-title" />
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
            {inForce.destinationDepartmentId ? (
              <>
                {' · '}
                <Label
                  k="decisions.panel.destination-prefix"
                  params={{ destination: departmentName(inForce.destinationDepartmentId) }}
                />
              </>
            ) : (
              ''
            )}
            {inForce.summary ? ` · ${inForce.summary}` : ''}
          </p>
          {inForce.appeals.length > 0 && (
            <div className="text-sm">
              <AppealLines appeals={inForce.appeals} departmentName={departmentName} />
            </div>
          )}
          {inForce.effectId === DECISION_EFFECT.Join && (
            <p className="text-sm text-muted">
              <Label k="decisions.panel.join-prefix" />{' '}
              {principalLink ? (
                <Link
                  to={`/documents/${principalLink.relatedDocument.documentId}`}
                  className="text-accent hover:underline"
                >
                  {principalLink.relatedDocument.title}
                </Link>
              ) : (
                <Label k="decisions.panel.join-principal-fallback" />
              )}
              <Label k="decisions.panel.join-suffix" />
              {principalLink?.relatedDocument.officialNumberText ? (
                <>
                  {' '}
                  <span dir="ltr" className="font-mono">
                    {principalLink.relatedDocument.officialNumberText}
                  </span>
                </>
              ) : null}
            </p>
          )}
        </section>
      )}

      {candidates.length > 0 && (
        <section className="flex flex-col gap-2 rounded-xl border border-border bg-surface p-4">
          <h2 className="flex items-center gap-2 text-sm font-semibold">
            <Label k="decisions.panel.candidates-title" />
            <GuideChip id="document-details.join-candidates" />
          </h2>
          <ul className="flex flex-col gap-1 text-sm text-muted">
            {candidates.map((candidate) => (
              <li key={candidate.documentId}>
                {candidate.personName}{' '}
                <Label k="decisions.panel.candidate-appears-in" />{' '}
                <Link
                  to={`/documents/${candidate.documentId}`}
                  className="text-accent hover:underline"
                >
                  {candidate.title}
                </Link>
                {candidate.officialNumberText ? (
                  <>
                    {' '}
                    <span dir="ltr" className="font-mono">
                      {candidate.officialNumberText}
                    </span>
                  </>
                ) : null}
                <Label k="decisions.panel.candidate-suffix" />
              </li>
            ))}
          </ul>
        </section>
      )}

      <div className="flex flex-wrap items-center gap-2">
        {canIssue && (
          <Button type="button" onClick={() => setIssueOpen(true)}>
            <Gavel className="size-4" aria-hidden />
            <Label k="decisions.panel.issue" />
          </Button>
        )}
        {canManage && inForce && inForce.isAppealable && inForce.statusId === DECISION_STATUS.Issued && (
          <>
            <Button type="button" variant="outline" onClick={() => setAppealTarget(inForce)}>
              <Scale className="size-4" aria-hidden />
              <Label k="decisions.panel.record-appeal" />
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
            <Label k="decisions.panel.record-outcome" />
          </Button>
        )}
        {canRunNext && nextKind && (
          <>
            <Button type="button" variant="outline" onClick={runNext}>
              <Route className="size-4" aria-hidden />
              <Label k={NEXT_ACTION_LABEL_KEYS[nextKind]} />
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
        caption={t('decisions.panel.caption')}
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
        searchPlaceholder={t('decisions.panel.search-placeholder')}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        rowSearchText={(row) => `${row.decisionNumber} ${row.typeNameAr} ${row.summary ?? ''}`}
        rowMenu={canManage ? rowMenu : undefined}
        rowLabel={(row) => row.decisionNumber}
        emptyTitle={t('decisions.panel.empty-title')}
        emptyDescription={
          decisionsQuery.isError
            ? toArabicErrorMessage(decisionsQuery.error)
            : t('decisions.panel.empty-description')
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
