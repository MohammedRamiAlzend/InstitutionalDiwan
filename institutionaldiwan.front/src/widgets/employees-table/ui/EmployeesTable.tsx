import { useMemo, useState } from 'react'
import {
  Copy,
  Hash,
  Pencil,
  Plus,
  RefreshCw,
  UserCheck,
  UserX,
} from 'lucide-react'
import { toast } from 'sonner'
import { DepartmentBadge, useDepartmentOptions, DepartmentLabel } from '@/entities/department'
import { useEmployees, type EmployeeResponse } from '@/entities/employee'
import { useSessionStore } from '@/entities/user'
import {
  EmployeeFormDialog,
  useSetEmployeeActiveMutation,
} from '@/features/employee-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import {
  formatDate,
  formatDateTime,
  t,
  useLabel,
  useTableState,
} from '@/shared/lib'
import {
  Badge,
  Button,
  ConfirmDialog,
  DataTable,
  GuideChip,
  Label,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

/** Module 02 employees administration grid (server search + paging). */
export function EmployeesTable() {
  const table = useTableState({ defaultSortKey: 'fullNameAr', defaultSortDirection: 'asc', filterKeys: ['departmentId'] })
  const employeesQuery = useEmployees({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    departmentId: table.filters.departmentId || undefined,
    sortBy: table.sortKey ?? 'fullNameAr',
    sortDescending: table.sortDirection === 'desc',
  })

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManageEmployees = hasPermission(Permissions.EmployeeManage)
  const allDepartmentsLabel = useLabel('employee.filter.all-departments')

  const departmentsQuery = useDepartmentOptions({ enabled: canManageEmployees })

  const departmentById = useMemo(
    () =>
      new Map(
        (departmentsQuery.data ?? []).map((department) => [
          department.departmentId,
          department,
        ]),
      ),
    [departmentsQuery.data],
  )

  const [formOpen, setFormOpen] = useState(false)
  const [formEmployee, setFormEmployee] = useState<EmployeeResponse | null>(null)
  const [statusEmployee, setStatusEmployee] = useState<EmployeeResponse | null>(null)

  const statusMutation = useSetEmployeeActiveMutation()

  const openCreate = () => {
    setFormEmployee(null)
    setFormOpen(true)
  }

  const openEdit = (employee: EmployeeResponse) => {
    setFormEmployee(employee)
    setFormOpen(true)
  }

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('employee.toast.copied', { label }))
    } catch {
      toast.error(t('employee.toast.copy-failed'))
    }
  }

  const handleToggleStatus = async () => {
    if (!statusEmployee) return
    try {
      await statusMutation.mutateAsync({
        employeeId: statusEmployee.employeeId,
        active: !statusEmployee.isActive,
      })
      toast.success(
        statusEmployee.isActive ? t('employee.toast.suspended') : t('employee.toast.activated'),
      )
      setStatusEmployee(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<EmployeeResponse>[]>(
    () => [
      {
        id: 'employee',
        headerKey: 'employee.column.employee',
        sortable: true,
        sortKey: 'fullNameAr',
        hideable: false,
        cell: (employee) => (
          <div className="flex flex-col gap-0.5">
            <span className="text-sm font-medium text-foreground">
              {employee.fullNameAr}
            </span>
            {employee.fullNameEn && (
              <span dir="ltr" className="text-xs text-muted">
                {employee.fullNameEn}
              </span>
            )}
          </div>
        ),
      },
      {
        id: 'employeeNumber',
        headerKey: 'employee.column.number',
        sortable: true,
        cell: (employee) =>
          employee.employeeNumber ? (
            <span dir="ltr" className="font-mono text-xs text-foreground">
              {employee.employeeNumber}
            </span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'department',
        headerKey: 'employee.column.department',
        cell: (employee) => {
          const department = departmentById.get(employee.departmentId)
          return department ? (
            <DepartmentBadge
              nameAr={department.nameAr}
              code={department.code}
              isActive={department.isActive}
            />
          ) : (
            <DepartmentLabel departmentId={employee.departmentId} />
          )
        },
      },
      {
        id: 'jobTitleAr',
        headerKey: 'employee.column.job-title',
        cell: (employee) =>
          employee.jobTitleAr ? (
            <span className="text-xs text-foreground">{employee.jobTitleAr}</span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'email',
        headerKey: 'employee.column.email',
        defaultHidden: true,
        cell: (employee) =>
          employee.email ? (
            <span
              dir="ltr"
              title={employee.email}
              className="block max-w-64 truncate text-xs text-foreground"
            >
              {employee.email}
            </span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'phone',
        headerKey: 'employee.column.phone',
        defaultHidden: true,
        cell: (employee) =>
          employee.phone ? (
            <span dir="ltr" className="whitespace-nowrap text-xs text-foreground">
              {employee.phone}
            </span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'status',
        headerKey: 'employee.column.status',
        sortable: true,
        sortKey: 'isActive',
        cell: (employee) => (
          <Badge variant={employee.isActive ? 'success' : 'danger'}>
            {employee.isActive ? t('employee.status.active') : t('employee.status.suspended')}
          </Badge>
        ),
      },
      {
        id: 'createdAt',
        headerKey: 'employee.column.created-at',
        sortable: true,
        defaultHidden: true,
        cell: (employee) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(employee.createdAt)}
          </span>
        ),
      },
      {
        id: 'updatedAt',
        headerKey: 'employee.column.updated-at',
        defaultHidden: true,
        cell: (employee) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(employee.updatedAt)}
          </span>
        ),
      },
    ],
    [departmentById],
  )

  const buildRowMenu = (employee: EmployeeResponse): MenuEntry[] => [
    {
      id: 'edit',
      label: t('employee.menu.edit'),
      icon: <Pencil className="size-4" aria-hidden />,
      onSelect: () => openEdit(employee),
    },
    { type: 'separator' },
    {
      id: 'toggle-active',
      label: employee.isActive ? t('employee.menu.suspend') : t('employee.menu.activate'),
      icon: employee.isActive ? (
        <UserX className="size-4" aria-hidden />
      ) : (
        <UserCheck className="size-4" aria-hidden />
      ),
      danger: employee.isActive,
      onSelect: () => setStatusEmployee(employee),
    },
    { type: 'separator' },
    {
      id: 'copy-number',
      label: t('employee.menu.copy-number'),
      icon: <Copy className="size-4" aria-hidden />,
      disabled: !employee.employeeNumber,
      onSelect: () =>
        void copyValue(employee.employeeNumber ?? '', t('employee.copy.number')),
    },
    {
      id: 'copy-id',
      label: t('employee.menu.copy-id'),
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyValue(employee.employeeId, t('employee.copy.id')),
    },
  ]

  const headerMenu: MenuEntry[] = [
    {
      id: 'create',
      label: t('employee.create'),
      icon: <Plus className="size-4" aria-hidden />,
      onSelect: openCreate,
    },
    {
      id: 'refresh',
      label: t('table.refresh'),
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void employeesQuery.refetch(),
    },
  ]

  return (
    <>
      <DataTable
        tableId="employees-table"
        caption={t('employee.caption')}
        columns={columns}
        rows={employeesQuery.data?.items}
        rowKey={(employee) => employee.employeeId}
        isLoading={employeesQuery.isLoading}
        error={employeesQuery.error}
        onRetry={() => void employeesQuery.refetch()}
        onRefresh={() => void employeesQuery.refetch()}
        isFetching={employeesQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={employeesQuery.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <Select
              aria-label={t('employee.filter.department-aria')}
              value={table.filters.departmentId || 'all'}
              onValueChange={(id) => table.setFilter('departmentId', id === 'all' ? null : id)}
              options={[
                { value: 'all', label: allDepartmentsLabel },
                ...(departmentsQuery.data ?? []).map((department) => ({ value: department.departmentId, label: department.nameAr })),
              ]}
              disabled={departmentsQuery.isLoading}
            />
            {departmentsQuery.error && (
              <Button type="button" variant="outline" onClick={() => void departmentsQuery.refetch()}><Label k="employee.filter.reload-departments" /></Button>
            )}
          </div>
        }
        searchPlaceholder={t('employee.search-placeholder')}
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(employee) => employee.fullNameAr}
        toolbar={
          <>
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              <Label k="employee.create" />
            </Button>
            <GuideChip id="employees.create" />
          </>
        }
        emptyTitle={table.search ? t('employee.empty.search-title') : t('employee.empty.title')}
        emptyDescription={
          table.search
            ? t('employee.empty.search-description')
            : t('employee.empty.description')
        }
        emptyAction={
          table.search ? (
            <Button type="button" variant="outline" onClick={() => table.setSearch('')}>
              <Label k="employee.empty.clear-search" />
            </Button>
          ) : (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              <Label k="employee.empty.add" />
            </Button>
          )
        }
      />

      {formOpen && (
        <EmployeeFormDialog
          key={formEmployee?.employeeId ?? 'create'}
          open
          onOpenChange={setFormOpen}
          employee={formEmployee}
        />
      )}

      <ConfirmDialog
        open={statusEmployee !== null}
        onOpenChange={(open) => {
          if (!open) setStatusEmployee(null)
        }}
        title={statusEmployee?.isActive ? t('employee.confirm.suspend-title') : t('employee.confirm.activate-title')}
        description={
          statusEmployee
            ? statusEmployee.isActive
              ? t('employee.confirm.suspend-description', { name: statusEmployee.fullNameAr })
              : t('employee.confirm.activate-description', { name: statusEmployee.fullNameAr })
            : undefined
        }
        confirmLabel={statusEmployee?.isActive ? t('employee.confirm.suspend') : t('employee.confirm.activate')}
        destructive={statusEmployee?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
