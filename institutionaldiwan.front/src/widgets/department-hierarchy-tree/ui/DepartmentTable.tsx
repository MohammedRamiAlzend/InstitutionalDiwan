import { useMemo } from 'react'
import {
  Copy,
  Eye,
  Hash,
  Pencil,
  Plus,
  Power,
  PowerOff,
  RefreshCw,
} from 'lucide-react'
import {
  DepartmentBadge,
  useDepartments,
  type DepartmentResponse,
  DepartmentLabel,
} from '@/entities/department'
import {
  formatDate,
  formatDateTime,
  t,
  type TableState,
} from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  Label,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'
import { copyDepartmentValue } from '../model/clipboard'
import { EmployeeLabel } from '@/entities/employee'
import { FEATURES } from '@/shared/config'

const EXTERNAL_COLUMN_IDS = new Set(['canSendExternal', 'canReceiveExternal'])

interface DepartmentTableProps {
  table: TableState
  query: ReturnType<typeof useDepartments>
  typeLabel: (departmentTypeId: number) => string
  resolveName: (departmentId: string) => string | undefined
  onOpen: (departmentId: string) => void
  onCreate: () => void
  onCreateChild: (parentDepartmentId: string) => void
  onEdit: (departmentId: string) => void
  onToggleActive: (department: DepartmentResponse) => void
}

/** Flat client-paged grid of every department (same URL search/paging as the tree). */
export function DepartmentTable({
  table,
  query,
  typeLabel,
  resolveName,
  onOpen,
  onCreate,
  onCreateChild,
  onEdit,
  onToggleActive,
}: DepartmentTableProps) {
  const columns = useMemo<DataTableColumn<DepartmentResponse>[]>(
    () => [
      {
        id: 'name',
        headerKey: 'department.column.name',
        hideable: false,
        sortable: true,
        sortValue: (department) => department.nameAr,
        cell: (department) => (
          <DepartmentBadge
            nameAr={department.nameAr}
            code={department.code}
            isActive={department.isActive}
          />
        ),
      },
      {
        id: 'type',
        headerKey: 'department.column.type',
        sortable: true,
        sortValue: (department) => typeLabel(department.departmentTypeId),
        cell: (department) => (
          <span className="text-xs text-foreground">
            {typeLabel(department.departmentTypeId)}
          </span>
        ),
      },
      {
        id: 'parent',
        headerKey: 'department.column.parent',
        sortable: true,
        sortValue: (department) =>
          (department.parentDepartmentId && resolveName(department.parentDepartmentId)) ?? '',
        cell: (department) => {
          if (!department.parentDepartmentId) return <span className="text-muted">—</span>
          const parentName = resolveName(department.parentDepartmentId)
          return parentName ? (
            <span className="text-xs text-foreground">{parentName}</span>
          ) : (
            <DepartmentLabel departmentId={department.parentDepartmentId} />
          )
        },
      },
      {
        id: 'status',
        headerKey: 'department.column.status',
        sortable: true,
        sortValue: (department) => (department.isActive ? 1 : 0),
        cell: (department) => (
          <Badge variant={department.isActive ? 'success' : 'danger'}>
            {department.isActive ? t('department.status.active') : t('department.status.suspended')}
          </Badge>
        ),
      },
      {
        id: 'canSendExternal',
        headerKey: 'department.column.send-external',
        defaultHidden: true,
        cell: (department) => (
          <Badge variant={department.canSendExternal ? 'success' : 'outline'}>
            {department.canSendExternal ? t('department.yes') : t('department.no')}
          </Badge>
        ),
      },
      {
        id: 'canReceiveExternal',
        headerKey: 'department.column.receive-external',
        defaultHidden: true,
        cell: (department) => (
          <Badge variant={department.canReceiveExternal ? 'success' : 'outline'}>
            {department.canReceiveExternal ? t('department.yes') : t('department.no')}
          </Badge>
        ),
      },
      {
        id: 'manager',
        headerKey: 'department.column.manager',
        defaultHidden: true,
        cell: (department) =>
          department.managerEmployeeId ? (
            <EmployeeLabel employeeId={department.managerEmployeeId} />
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'notes',
        headerKey: 'department.column.notes',
        defaultHidden: true,
        cell: (department) =>
          department.notes ? (
            <span
              title={department.notes}
              className="block max-w-64 truncate text-xs text-foreground"
            >
              {department.notes}
            </span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'createdAt',
        headerKey: 'department.column.created-at',
        defaultHidden: true,
        sortable: true,
        sortValue: (department) => department.createdAt,
        cell: (department) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(department.createdAt)}
          </span>
        ),
      },
      {
        id: 'updatedAt',
        headerKey: 'department.column.updated-at',
        defaultHidden: true,
        cell: (department) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(department.updatedAt)}
          </span>
        ),
      },
    ],
    [typeLabel, resolveName],
  )

  const buildRowMenu = (department: DepartmentResponse): MenuEntry[] => [
    {
      id: 'details',
      label: t('department.menu.details'),
      icon: <Eye className="size-4" aria-hidden />,
      onSelect: () => onOpen(department.departmentId),
    },
    { type: 'separator' },
    {
      id: 'add-child',
      label: t('department.menu.add-child'),
      icon: <Plus className="size-4" aria-hidden />,
      onSelect: () => onCreateChild(department.departmentId),
    },
    {
      id: 'edit',
      label: t('department.menu.edit'),
      icon: <Pencil className="size-4" aria-hidden />,
      onSelect: () => onEdit(department.departmentId),
    },
    {
      id: 'toggle-active',
      label: department.isActive ? t('department.menu.suspend') : t('department.menu.activate'),
      icon: department.isActive ? (
        <PowerOff className="size-4" aria-hidden />
      ) : (
        <Power className="size-4" aria-hidden />
      ),
      onSelect: () => onToggleActive(department),
    },
    { type: 'separator' },
    {
      id: 'copy-code',
      label: t('department.menu.copy-code'),
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyDepartmentValue(department.code, t('department.copy.code')),
    },
    {
      id: 'copy-id',
      label: t('department.menu.copy-id'),
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyDepartmentValue(department.departmentId, t('department.copy.id')),
    },
  ]

  const headerMenu: MenuEntry[] = [
    {
      id: 'create',
      label: t('department.create'),
      icon: <Plus className="size-4" aria-hidden />,
      onSelect: onCreate,
    },
    { type: 'separator' },
    {
      id: 'refresh',
      label: t('table.refresh'),
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void query.refetch(),
    },
  ]

  return (
    <DataTable
      tableId="departments"
      mode="client"
      caption={t('department.table.caption')}
      columns={FEATURES.externalOrganizations ? columns : columns.filter((column) => !EXTERNAL_COLUMN_IDS.has(column.id))}
      rows={query.data}
      rowKey={(department) => department.departmentId}
      isLoading={query.isLoading}
      error={query.error}
      onRetry={() => void query.refetch()}
      onRefresh={() => void query.refetch()}
      isFetching={query.isFetching}
      page={table.page}
      pageSize={table.pageSize}
      totalCount={query.data?.length ?? 0}
      onPageChange={table.setPage}
      onPageSizeChange={table.setPageSize}
      search={table.search}
      onSearchChange={table.setSearch}
      searchPlaceholder={t('department.search-placeholder')}
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      rowMenu={buildRowMenu}
      headerMenu={headerMenu}
      emptyMenu={headerMenu}
      rowLabel={(department) => department.nameAr}
      rowSearchText={(department) =>
        `${department.nameAr} ${department.nameEn ?? ''} ${department.code}`
      }
      emptyTitle={table.search ? t('department.empty.search-title') : t('department.empty.title')}
      emptyDescription={
        table.search
          ? t('department.empty.search-description')
          : t('department.empty.description')
      }
      emptyAction={
        table.search ? (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => table.setSearch('')}
          >
            <Label k="department.empty.clear-search" />
          </Button>
        ) : undefined
      }
    />
  )
}
