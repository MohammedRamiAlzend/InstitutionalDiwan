import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Ban, ExternalLink } from 'lucide-react'
import { useDepartmentOptions } from '@/entities/department'
import { useDecisionTypes } from '@/entities/decision-type'
import {
  DecisionStatusBadge,
  useDecisionRegister,
  type DecisionRegisterItemResponse,
} from '@/entities/document-decision'
import { useSessionStore } from '@/entities/user'
import { CancelDecisionDialog } from '@/features/decision-manage'
import { tokenStorage } from '@/shared/api'
import {
  DECISION_STATUS,
  decisionEffectLabel,
  decisionStatusLabel,
  Permissions,
  ROUTES,
  USER_SCOPE_TYPE,
} from '@/shared/config'
import { decodeJwtPayload, formatDate, t, useLabel, useTableState } from '@/shared/lib'
import { Button, DataTable, GuideRegion, Label, Select, type DataTableColumn, type MenuEntry } from '@/shared/ui'

function isSystemScope(): boolean {
  return decodeJwtPayload(tokenStorage.get())?.scope_type === String(USER_SCOPE_TYPE.System)
}

/** Server-paged register of decisions the caller is allowed to see. */
export function DecisionRegisterTable() {
  const navigate = useNavigate()
  const table = useTableState({
    filterKeys: ['type', 'status', 'department', 'year'],
  })
  const departmentsQuery = useDepartmentOptions()
  const typesQuery = useDecisionTypes()
  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManage = hasPermission(Permissions.DecisionManage)
  const systemScope = isSystemScope()
  const [cancelTarget, setCancelTarget] = useState<DecisionRegisterItemResponse | null>(null)

  const year = Number.parseInt(table.filters.year ?? '', 10)
  const statusId = Number.parseInt(table.filters.status ?? '', 10)
  const registerQuery = useDecisionRegister({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    sortBy: table.sortKey ?? undefined,
    sortDescending: table.sortDirection === 'desc',
    decisionTypeId: table.filters.type || undefined,
    statusId: Number.isFinite(statusId) ? statusId : undefined,
    issuingDepartmentId: table.filters.department || undefined,
    year: Number.isFinite(year) ? year : undefined,
  })

  const departmentName = useMemo(() => {
    const names = new Map(
      (departmentsQuery.data ?? []).map((department) => [department.departmentId, department.nameAr]),
    )
    return (departmentId: string) => names.get(departmentId) ?? departmentId
  }, [departmentsQuery.data])

  const columns = useMemo<DataTableColumn<DecisionRegisterItemResponse>[]>(
    () => [
      {
        id: 'number',
        headerKey: 'decisions.register.column.number',
        hideable: false,
        sortable: true,
        sortKey: 'decisionNumber',
        cell: (row) => (
          <span dir="ltr" className="font-mono text-sm">
            {row.decisionNumber}
          </span>
        ),
      },
      {
        id: 'document',
        headerKey: 'decisions.register.column.document',
        cell: (row) => (
          <span>
            {row.documentTitle}
            {row.officialNumberText && (
              <span dir="ltr" className="ms-2 font-mono text-xs text-muted">
                {row.officialNumberText}
              </span>
            )}
          </span>
        ),
      },
      {
        id: 'type',
        headerKey: 'decisions.register.column.type',
        sortable: true,
        sortKey: 'type',
        cell: (row) => row.typeNameAr,
      },
      {
        id: 'date',
        headerKey: 'decisions.register.column.date',
        sortable: true,
        sortKey: 'decisionDate',
        cell: (row) => <span dir="ltr">{formatDate(row.decisionDate)}</span>,
      },
      {
        id: 'effect',
        headerKey: 'decisions.register.column.effect',
        cell: (row) => decisionEffectLabel(row.effectId),
      },
      {
        id: 'status',
        headerKey: 'decisions.register.column.status',
        sortable: true,
        sortKey: 'status',
        cell: (row) => <DecisionStatusBadge statusId={row.statusId} />,
      },
      {
        id: 'department',
        headerKey: 'decisions.register.column.department',
        cell: (row) => departmentName(row.issuingDepartmentId),
      },
    ],
    [departmentName],
  )

  const rowMenu = (row: DecisionRegisterItemResponse): MenuEntry[] => {
    const items: MenuEntry[] = [
      {
        id: 'open',
        label: t('decisions.register.row.open'),
        icon: <ExternalLink className="size-4" aria-hidden />,
        onSelect: () => navigate(`${ROUTES.DOCUMENT_DETAIL(row.documentId)}?tab=decisions`),
      },
    ]
    if (canManage && systemScope && row.statusId !== DECISION_STATUS.Canceled) {
      items.push({
        id: 'cancel',
        label: t('decisions.register.row.cancel'),
        icon: <Ban className="size-4" aria-hidden />,
        danger: true,
        onSelect: () => setCancelTarget(row),
      })
    }
    return items
  }

  const allTypesLabel = useLabel('decisions.filter.all-types')
  const allStatusesLabel = useLabel('decisions.filter.all-statuses')
  const allDepartmentsLabel = useLabel('decisions.filter.all-departments')

  const typeOptions = [
    { value: '', label: allTypesLabel },
    ...(typesQuery.data ?? []).map((type) => ({ value: type.decisionTypeId, label: type.nameAr })),
  ]
  const statusOptions = [
    { value: '', label: allStatusesLabel },
    ...[
      DECISION_STATUS.Issued,
      DECISION_STATUS.Appealed,
      DECISION_STATUS.Final,
      DECISION_STATUS.Revoked,
      DECISION_STATUS.Canceled,
    ].map((statusId) => ({ value: String(statusId), label: decisionStatusLabel(statusId) })),
  ]
  const departmentOptions = [
    { value: '', label: allDepartmentsLabel },
    ...(departmentsQuery.data ?? []).map((department) => ({
      value: department.departmentId,
      label: department.nameAr,
    })),
  ]

  return (
    <GuideRegion id="decisions.register">
      <DataTable
        tableId="decisions"
        mode="server"
        caption={t('decisions.tab.register')}
        columns={columns}
        rows={registerQuery.data?.items}
        rowKey={(row) => row.decisionId}
        isLoading={registerQuery.isLoading}
        error={registerQuery.error}
        onRetry={() => void registerQuery.refetch()}
        onRefresh={() => void registerQuery.refetch()}
        isFetching={registerQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={registerQuery.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        searchPlaceholder={t('decisions.register.search-placeholder')}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        rowMenu={rowMenu}
        rowLabel={(row) => row.decisionNumber}
        emptyTitle={t('decisions.register.empty.title')}
        emptyDescription={t('decisions.register.empty.description')}
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <Select
              aria-label={t('decisions.register.filter.type-aria')}
              value={table.filters.type || undefined}
              onValueChange={(value) => table.setFilter('type', value || null)}
              options={typeOptions.filter((option) => option.value !== '')}
              placeholder={allTypesLabel}
            />
            <Select
              aria-label={t('decisions.register.filter.status-aria')}
              value={table.filters.status || undefined}
              onValueChange={(value) => table.setFilter('status', value || null)}
              options={statusOptions.filter((option) => option.value !== '')}
              placeholder={allStatusesLabel}
            />
            <Select
              aria-label={t('decisions.register.column.department')}
              value={table.filters.department || undefined}
              onValueChange={(value) => table.setFilter('department', value || null)}
              options={departmentOptions.filter((option) => option.value !== '')}
              placeholder={allDepartmentsLabel}
            />
            <input
              aria-label={t('decisions.register.filter.year-aria')}
              inputMode="numeric"
              dir="ltr"
              placeholder={t('decisions.register.filter.year-placeholder')}
              value={table.filters.year ?? ''}
              onChange={(event) => table.setFilter('year', event.target.value.replace(/\D/g, '').slice(0, 4) || null)}
              className="h-10 w-24 rounded-lg border border-border bg-surface px-3 text-sm"
            />
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" onClick={table.clearFilters}>
                <Label k="decisions.filter.clear" />
              </Button>
            )}
          </div>
        }
      />
      {cancelTarget && (
        <CancelDecisionDialog
          documentId={cancelTarget.documentId}
          decisionId={cancelTarget.decisionId}
          decisionNumber={cancelTarget.decisionNumber}
          open
          onOpenChange={(open) => {
            if (!open) setCancelTarget(null)
          }}
        />
      )}
    </GuideRegion>
  )
}
