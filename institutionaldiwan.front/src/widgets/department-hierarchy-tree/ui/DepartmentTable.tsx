import { useMemo } from 'react'
import { Copy, Eye, Hash, RefreshCw } from 'lucide-react'
import {
  DepartmentBadge,
  useDepartments,
  type DepartmentResponse,
} from '@/entities/department'
import {
  formatDate,
  formatDateTime,
  formatShortId,
  type TableState,
} from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'
import { copyDepartmentValue } from '../model/clipboard'

interface DepartmentTableProps {
  table: TableState
  query: ReturnType<typeof useDepartments>
  typeLabel: (departmentTypeId: number) => string
  resolveName: (departmentId: string) => string | undefined
  onOpen: (departmentId: string) => void
}

/** Flat client-paged grid of every department (same URL search/paging as the tree). */
export function DepartmentTable({
  table,
  query,
  typeLabel,
  resolveName,
  onOpen,
}: DepartmentTableProps) {
  const columns = useMemo<DataTableColumn<DepartmentResponse>[]>(
    () => [
      {
        id: 'name',
        header: 'القسم',
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
        header: 'النوع',
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
        header: 'القسم الأعلى',
        sortable: true,
        sortValue: (department) =>
          (department.parentDepartmentId && resolveName(department.parentDepartmentId)) ?? '',
        cell: (department) => {
          if (!department.parentDepartmentId) return <span className="text-muted">—</span>
          const parentName = resolveName(department.parentDepartmentId)
          return parentName ? (
            <span className="text-xs text-foreground">{parentName}</span>
          ) : (
            <span
              dir="ltr"
              title={department.parentDepartmentId}
              className="font-mono text-xs text-muted"
            >
              {formatShortId(department.parentDepartmentId)}
            </span>
          )
        },
      },
      {
        id: 'status',
        header: 'الحالة',
        sortable: true,
        sortValue: (department) => (department.isActive ? 1 : 0),
        cell: (department) => (
          <Badge variant={department.isActive ? 'success' : 'danger'}>
            {department.isActive ? 'نشط' : 'موقوف'}
          </Badge>
        ),
      },
      {
        id: 'canSendExternal',
        header: 'إرسال خارجي',
        defaultHidden: true,
        cell: (department) => (
          <Badge variant={department.canSendExternal ? 'success' : 'outline'}>
            {department.canSendExternal ? 'نعم' : 'لا'}
          </Badge>
        ),
      },
      {
        id: 'canReceiveExternal',
        header: 'استقبال خارجي',
        defaultHidden: true,
        cell: (department) => (
          <Badge variant={department.canReceiveExternal ? 'success' : 'outline'}>
            {department.canReceiveExternal ? 'نعم' : 'لا'}
          </Badge>
        ),
      },
      {
        id: 'manager',
        header: 'مدير القسم',
        defaultHidden: true,
        cell: (department) =>
          department.managerEmployeeId ? (
            <span
              dir="ltr"
              title={department.managerEmployeeId}
              className="font-mono text-xs text-muted"
            >
              {formatShortId(department.managerEmployeeId)}
            </span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'notes',
        header: 'ملاحظات',
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
        header: 'تاريخ الإنشاء',
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
        header: 'آخر تحديث',
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
      label: 'تفاصيل القسم',
      icon: <Eye className="size-4" aria-hidden />,
      onSelect: () => onOpen(department.departmentId),
    },
    { type: 'separator' },
    {
      id: 'copy-code',
      label: 'نسخ رمز القسم',
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyDepartmentValue(department.code, 'رمز القسم'),
    },
    {
      id: 'copy-id',
      label: 'نسخ المعرّف',
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyDepartmentValue(department.departmentId, 'المعرّف'),
    },
  ]

  const headerMenu: MenuEntry[] = [
    {
      id: 'refresh',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void query.refetch(),
    },
  ]

  return (
    <DataTable
      tableId="departments"
      mode="client"
      caption="أقسام التنظيم"
      columns={columns}
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
      searchPlaceholder="ابحث باسم القسم أو رمزه…"
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
      emptyTitle={table.search ? 'لا توجد نتائج مطابقة.' : 'لا توجد أقسام بعد.'}
      emptyDescription={
        table.search
          ? 'جرّب تعديل عبارة البحث أو امسحها لعرض كل الأقسام.'
          : 'لم تُسجَّل أقسام تنظيمية في الدليل بعد.'
      }
      emptyAction={
        table.search ? (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => table.setSearch('')}
          >
            مسح البحث
          </Button>
        ) : undefined
      }
    />
  )
}
