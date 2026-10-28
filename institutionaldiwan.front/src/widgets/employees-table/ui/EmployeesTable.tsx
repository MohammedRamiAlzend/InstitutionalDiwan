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
import { DepartmentBadge, useDepartments } from '@/entities/department'
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
  formatShortId,
  useTableState,
} from '@/shared/lib'
import {
  Badge,
  Button,
  ConfirmDialog,
  DataTable,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

/** Module 02 employees administration grid (server search + paging). */
export function EmployeesTable() {
  const table = useTableState()
  const employeesQuery = useEmployees({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
  })

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManageDepartments = hasPermission(Permissions.DepartmentManage)

  const departmentsQuery = useDepartments({ enabled: canManageDepartments })

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
      toast.success(`تم نسخ ${label}.`)
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
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
        statusEmployee.isActive ? 'تم إيقاف الموظف.' : 'تم تفعيل الموظف.',
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
        header: 'الموظف',
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
        header: 'الرقم الوظيفي',
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
        header: 'القسم',
        cell: (employee) => {
          const department = departmentById.get(employee.departmentId)
          return department ? (
            <DepartmentBadge
              nameAr={department.nameAr}
              code={department.code}
              isActive={department.isActive}
            />
          ) : (
            <span
              dir="ltr"
              title={employee.departmentId}
              className="font-mono text-xs text-muted"
            >
              {formatShortId(employee.departmentId)}
            </span>
          )
        },
      },
      {
        id: 'jobTitleAr',
        header: 'المسمى الوظيفي',
        cell: (employee) =>
          employee.jobTitleAr ? (
            <span className="text-xs text-foreground">{employee.jobTitleAr}</span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'email',
        header: 'البريد الإلكتروني',
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
        header: 'الهاتف',
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
        header: 'الحالة',
        cell: (employee) => (
          <Badge variant={employee.isActive ? 'success' : 'danger'}>
            {employee.isActive ? 'نشط' : 'موقوف'}
          </Badge>
        ),
      },
      {
        id: 'createdAt',
        header: 'تاريخ الإنشاء',
        defaultHidden: true,
        cell: (employee) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(employee.createdAt)}
          </span>
        ),
      },
      {
        id: 'updatedAt',
        header: 'آخر تحديث',
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
      label: 'تعديل البيانات',
      icon: <Pencil className="size-4" aria-hidden />,
      onSelect: () => openEdit(employee),
    },
    { type: 'separator' },
    {
      id: 'toggle-active',
      label: employee.isActive ? 'إيقاف الموظف' : 'تفعيل الموظف',
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
      label: 'نسخ الرقم الوظيفي',
      icon: <Copy className="size-4" aria-hidden />,
      disabled: !employee.employeeNumber,
      onSelect: () =>
        void copyValue(employee.employeeNumber ?? '', 'الرقم الوظيفي'),
    },
    {
      id: 'copy-id',
      label: 'نسخ المعرّف',
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyValue(employee.employeeId, 'المعرّف'),
    },
  ]

  const headerMenu: MenuEntry[] = [
    {
      id: 'create',
      label: 'موظف جديد',
      icon: <Plus className="size-4" aria-hidden />,
      onSelect: openCreate,
    },
    {
      id: 'refresh',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void employeesQuery.refetch(),
    },
  ]

  return (
    <>
      <DataTable
        tableId="employees-table"
        caption="قائمة الموظفين"
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
        searchPlaceholder="ابحث باسم الموظف أو رقمه الوظيفي…"
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(employee) => employee.fullNameAr}
        toolbar={
          <Button type="button" onClick={openCreate}>
            <Plus className="size-4" aria-hidden />
            موظف جديد
          </Button>
        }
        emptyTitle={table.search ? 'لا توجد نتائج مطابقة.' : 'لا يوجد موظفون بعد.'}
        emptyDescription={
          table.search
            ? 'جرّب تعديل عبارة البحث أو امسحها لعرض كل الموظفين.'
            : 'ابدأ بإضافة أول سجل موظف وربطه بقسم.'
        }
        emptyAction={
          table.search ? (
            <Button type="button" variant="outline" onClick={() => table.setSearch('')}>
              مسح البحث
            </Button>
          ) : (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              إضافة موظف
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
        title={statusEmployee?.isActive ? 'إيقاف الموظف' : 'تفعيل الموظف'}
        description={
          statusEmployee
            ? statusEmployee.isActive
              ? `سيُعلَّم الموظف «${statusEmployee.fullNameAr}» كموقوف، ولن يظهر ضمن الموظفين الفعّالين للإسناد.`
              : `سيعود الموظف «${statusEmployee.fullNameAr}» فعّالًا ومتاحًا للإسناد.`
            : undefined
        }
        confirmLabel={statusEmployee?.isActive ? 'إيقاف الموظف' : 'تفعيل الموظف'}
        destructive={statusEmployee?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
