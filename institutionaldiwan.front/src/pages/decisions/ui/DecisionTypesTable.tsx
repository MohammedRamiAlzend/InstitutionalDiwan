import { useMemo, useState } from 'react'
import { Pencil, Plus, Power, PowerOff } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { useDecisionTypes, type DecisionTypeResponse } from '@/entities/decision-type'
import {
  DecisionTypeFormDialog,
  useActivateDecisionTypeMutation,
  useDeactivateDecisionTypeMutation,
} from '@/features/decision-type-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { decisionEffectLabel } from '@/shared/config'
import { useTableState, t, useLabel } from '@/shared/lib'
import { Badge, Button, DataTable, GuideRegion, Label, Select, type DataTableColumn, type MenuEntry } from '@/shared/ui'

/** Client grid of decision types. A used type is deactivated, never deleted. */
export function DecisionTypesTable() {
  const table = useTableState({
    defaultSortKey: 'name',
    defaultSortDirection: 'asc',
    filterKeys: ['active'],
  })
  const typesQuery = useDecisionTypes()
  const departmentsQuery = useDepartmentOptions()
  const activate = useActivateDecisionTypeMutation()
  const deactivate = useDeactivateDecisionTypeMutation()
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<DecisionTypeResponse | null>(null)

  const activeFilterLabel = useLabel('decisions.types.filter.active')
  const inactiveFilterLabel = useLabel('decisions.types.filter.inactive')
  const allStatusesLabel = useLabel('decisions.filter.all-statuses')

  const departmentName = useMemo(() => {
    const names = new Map(
      (departmentsQuery.data ?? []).map((department) => [department.departmentId, department.nameAr]),
    )
    return (departmentId: string | null) => (departmentId ? names.get(departmentId) ?? '—' : '—')
  }, [departmentsQuery.data])

  const rows = useMemo(() => {
    const all = typesQuery.data ?? []
    if (table.filters.active === 'active') return all.filter((type) => type.isActive)
    if (table.filters.active === 'inactive') return all.filter((type) => !type.isActive)
    return all
  }, [typesQuery.data, table.filters.active])

  const toggle = async (type: DecisionTypeResponse) => {
    try {
      if (type.isActive) {
        await deactivate.mutateAsync(type.decisionTypeId)
        toast.success(t('decisions.types.toast.deactivated'))
      } else {
        await activate.mutateAsync(type.decisionTypeId)
        toast.success(t('decisions.types.toast.activated'))
      }
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<DecisionTypeResponse>[]>(
    () => [
      {
        id: 'name',
        headerKey: 'decisions.types.column.name',
        hideable: false,
        sortable: true,
        sortValue: (row) => row.nameAr,
        cell: (row) => row.nameAr,
      },
      {
        id: 'effect',
        headerKey: 'decisions.types.column.effect',
        sortable: true,
        sortValue: (row) => row.effectId,
        cell: (row) => decisionEffectLabel(row.effectId),
      },
      {
        id: 'destination',
        headerKey: 'decisions.types.column.destination',
        cell: (row) => departmentName(row.destinationDepartmentId),
      },
      {
        id: 'appealable',
        headerKey: 'decisions.types.column.appealable',
        cell: (row) => (
          <Label k={row.isAppealable ? 'decisions.types.appealable.yes' : 'decisions.types.appealable.no'} />
        ),
      },
      {
        id: 'active',
        headerKey: 'decisions.types.column.status',
        sortable: true,
        sortValue: (row) => (row.isActive ? 1 : 0),
        cell: (row) => (
          <Badge variant={row.isActive ? 'success' : 'outline'}>
            <Label k={row.isActive ? 'decisions.types.active.yes' : 'decisions.types.active.no'} />
          </Badge>
        ),
      },
    ],
    [departmentName],
  )

  const rowMenu = (row: DecisionTypeResponse): MenuEntry[] => [
    {
      id: 'edit',
      label: t('decisions.types.row.edit'),
      icon: <Pencil className="size-4" aria-hidden />,
      onSelect: () => {
        setEditing(row)
        setFormOpen(true)
      },
    },
    {
      id: 'toggle',
      label: t(row.isActive ? 'decisions.types.row.deactivate' : 'decisions.types.row.activate'),
      icon: row.isActive ? <PowerOff className="size-4" aria-hidden /> : <Power className="size-4" aria-hidden />,
      onSelect: () => void toggle(row),
    },
  ]

  return (
    <GuideRegion id="decisions.types">
      <DataTable
        tableId="decision-types"
        mode="client"
        caption={t('decisions.tab.types')}
        columns={columns}
        rows={rows}
        rowKey={(row) => row.decisionTypeId}
        isLoading={typesQuery.isLoading}
        error={typesQuery.error}
        onRetry={() => void typesQuery.refetch()}
        onRefresh={() => void typesQuery.refetch()}
        isFetching={typesQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={rows.length}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        searchPlaceholder={t('decisions.types.search-placeholder')}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        rowSearchText={(row) => row.nameAr}
        rowMenu={rowMenu}
        rowLabel={(row) => row.nameAr}
        emptyTitle={t('decisions.types.empty.title')}
        emptyDescription={t('decisions.types.empty.description')}
        toolbar={
          <Button
            type="button"
            onClick={() => {
              setEditing(null)
              setFormOpen(true)
            }}
          >
            <Plus className="size-4" aria-hidden />
            <Label k="decisions.types.action.new" />
          </Button>
        }
        filters={
          <Select
            aria-label={t('decisions.types.filter.status-aria')}
            value={table.filters.active || undefined}
            onValueChange={(value) => table.setFilter('active', value)}
            options={[
              { value: 'active', label: activeFilterLabel },
              { value: 'inactive', label: inactiveFilterLabel },
            ]}
            placeholder={allStatusesLabel}
          />
        }
      />
      {formOpen && (
        <DecisionTypeFormDialog
          key={editing?.decisionTypeId ?? 'new'}
          open
          onOpenChange={setFormOpen}
          decisionType={editing}
        />
      )}
    </GuideRegion>
  )
}
