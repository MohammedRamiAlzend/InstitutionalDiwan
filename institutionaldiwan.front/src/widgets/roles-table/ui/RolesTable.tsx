import { useMemo, useState } from 'react'
import {
  Copy,
  Hash,
  KeyRound,
  Pencil,
  Plus,
  RefreshCw,
  ShieldCheck,
  ShieldOff,
} from 'lucide-react'
import { toast } from 'sonner'
import { RoleBadge, useRoles, type RoleResponse } from '@/entities/role'
import {
  RoleFormDialog,
  RolePermissionsDialog,
  useSetRoleActiveMutation,
} from '@/features/role-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { formatDate, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  ConfirmDialog,
  DataTable,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

/** Module 02 roles grid with permission-matrix and status actions. */
export function RolesTable() {
  const table = useTableState({
    defaultSortKey: 'role',
    defaultSortDirection: 'asc',
  })
  const rolesQuery = useRoles()
  const roles = rolesQuery.data ?? []

  const [formOpen, setFormOpen] = useState(false)
  const [formRole, setFormRole] = useState<RoleResponse | null>(null)
  const [permissionsRoleId, setPermissionsRoleId] = useState<string | null>(null)
  const [statusRoleId, setStatusRoleId] = useState<string | null>(null)

  const permissionsRole =
    roles.find((role) => role.roleId === permissionsRoleId) ?? null
  const statusRole = roles.find((role) => role.roleId === statusRoleId) ?? null

  const statusMutation = useSetRoleActiveMutation()

  const openCreate = () => {
    setFormRole(null)
    setFormOpen(true)
  }

  const openEdit = (role: RoleResponse) => {
    setFormRole(role)
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
    if (!statusRole) return
    try {
      await statusMutation.mutateAsync({
        roleId: statusRole.roleId,
        active: !statusRole.isActive,
      })
      toast.success(statusRole.isActive ? 'تم إيقاف الدور.' : 'تم تفعيل الدور.')
      setStatusRoleId(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<RoleResponse>[]>(
    () => [
      {
        id: 'role',
        header: 'الدور',
        hideable: false,
        sortable: true,
        sortValue: (role) => role.nameAr,
        cell: (role) => (
          <RoleBadge
            nameAr={role.nameAr}
            code={role.code}
            isSystemRole={role.isSystemRole}
          />
        ),
      },
      {
        id: 'description',
        header: 'الوصف',
        cell: (role) => (
          <span
            className="line-clamp-2 max-w-72 text-xs text-muted"
            title={role.description ?? undefined}
          >
            {role.description ?? '—'}
          </span>
        ),
      },
      {
        id: 'permissions',
        header: 'الصلاحيات',
        sortable: true,
        sortValue: (role) => role.permissions.length,
        cell: (role) => (
          <Badge variant={role.permissions.length > 0 ? 'accent' : 'outline'}>
            {role.permissions.length} صلاحية
          </Badge>
        ),
      },
      {
        id: 'status',
        header: 'الحالة',
        cell: (role) => (
          <Badge variant={role.isActive ? 'success' : 'danger'}>
            {role.isActive ? 'نشط' : 'موقوف'}
          </Badge>
        ),
      },
      {
        id: 'kind',
        header: 'النوع',
        cell: (role) =>
          role.isSystemRole ? (
            <Badge variant="outline">نظام</Badge>
          ) : (
            <span className="text-xs text-muted">مخصص</span>
          ),
      },
      {
        id: 'createdAt',
        header: 'تاريخ الإنشاء',
        defaultHidden: true,
        sortable: true,
        sortValue: (role) => role.createdAt,
        cell: (role) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(role.createdAt)}
          </span>
        ),
      },
    ],
    [],
  )

  const buildRowMenu = (role: RoleResponse): MenuEntry[] => [
    {
      id: 'edit',
      label: 'تعديل البيانات',
      icon: <Pencil className="size-4" aria-hidden />,
      onSelect: () => openEdit(role),
    },
    {
      id: 'permissions',
      label: 'مصفوفة الصلاحيات',
      icon: <KeyRound className="size-4" aria-hidden />,
      onSelect: () => setPermissionsRoleId(role.roleId),
    },
    { type: 'separator' },
    {
      id: 'toggle-active',
      label: role.isActive ? 'إيقاف الدور' : 'تفعيل الدور',
      icon: role.isActive ? (
        <ShieldOff className="size-4" aria-hidden />
      ) : (
        <ShieldCheck className="size-4" aria-hidden />
      ),
      danger: role.isActive,
      disabled: role.isSystemRole,
      onSelect: () => setStatusRoleId(role.roleId),
    },
    { type: 'separator' },
    {
      id: 'copy-code',
      label: 'نسخ رمز الدور',
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyValue(role.code, 'رمز الدور'),
    },
    {
      id: 'copy-id',
      label: 'نسخ المعرّف',
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyValue(role.roleId, 'المعرّف'),
    },
  ]

  const createEntry: MenuEntry = {
    id: 'create',
    label: 'دور جديد',
    icon: <Plus className="size-4" aria-hidden />,
    onSelect: openCreate,
  }
  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: 'تحديث البيانات',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void rolesQuery.refetch(),
  }

  return (
    <>
      <DataTable
        tableId="roles-table"
        mode="client"
        caption="قائمة الأدوار"
        columns={columns}
        rows={rolesQuery.data}
        rowKey={(role) => role.roleId}
        rowSearchText={(role) =>
          [role.nameAr, role.code, role.nameEn ?? '', role.description ?? '']
            .join(' ')
            .toLocaleLowerCase('ar')
        }
        isLoading={rolesQuery.isLoading}
        error={rolesQuery.error}
        onRetry={() => void rolesQuery.refetch()}
        onRefresh={() => void rolesQuery.refetch()}
        isFetching={rolesQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={roles.length}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        searchPlaceholder="ابحث باسم الدور أو رمزه…"
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        rowMenu={buildRowMenu}
        headerMenu={[createEntry, refreshEntry]}
        emptyMenu={[createEntry, refreshEntry]}
        rowLabel={(role) => role.nameAr}
        toolbar={
          <Button type="button" onClick={openCreate}>
            <Plus className="size-4" aria-hidden />
            دور جديد
          </Button>
        }
        emptyTitle={table.search ? 'لا توجد أدوار مطابقة.' : 'لا توجد أدوار بعد.'}
        emptyDescription={
          table.search
            ? 'جرّب تعديل عبارة البحث أو امسحها لعرض كل الأدوار.'
            : 'أنشئ أول دور مخصص ثم امنحه الصلاحيات المناسبة.'
        }
        emptyAction={
          table.search ? (
            <Button type="button" variant="outline" onClick={() => table.setSearch('')}>
              مسح البحث
            </Button>
          ) : (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              إنشاء دور
            </Button>
          )
        }
      />

      {formOpen && (
        <RoleFormDialog
          key={formRole?.roleId ?? 'create'}
          open
          onOpenChange={setFormOpen}
          role={formRole}
        />
      )}

      {permissionsRole && (
        <RolePermissionsDialog
          open
          onOpenChange={(open) => {
            if (!open) setPermissionsRoleId(null)
          }}
          role={permissionsRole}
        />
      )}

      <ConfirmDialog
        open={statusRole !== null}
        onOpenChange={(open) => {
          if (!open) setStatusRoleId(null)
        }}
        title={statusRole?.isActive ? 'إيقاف الدور' : 'تفعيل الدور'}
        description={
          statusRole
            ? statusRole.isActive
              ? `سيُعلَّم الدور «${statusRole.nameAr}» كموقوف، ولن يظهر ضمن الأدوار الفعّالة للإسناد.`
              : `سيعود الدور «${statusRole.nameAr}» فعّالًا ومتاحًا للإسناد.`
            : undefined
        }
        confirmLabel={statusRole?.isActive ? 'إيقاف الدور' : 'تفعيل الدور'}
        destructive={statusRole?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
