import { useMemo, useState } from 'react'
import {
  Copy,
  Hash,
  KeyRound,
  LockKeyhole,
  Pencil,
  Plus,
  RefreshCw,
  ShieldCheck,
  UserCheck,
  UserX,
} from 'lucide-react'
import { toast } from 'sonner'
import { DepartmentBadge, useDepartmentOptions } from '@/entities/department'
import { useRoleOptions } from '@/entities/role'
import {
  useSessionStore,
  useUsers,
  UserScopeBadge,
  type UserResponse,
} from '@/entities/user'
import {
  ResetPasswordDialog,
  useSetUserActiveMutation,
  UserAccessDialog,
  UserFormDialog,
  UserRolesDialog,
} from '@/features/user-manage'
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

/** Module 02 users administration grid (search, paging, row actions). */
export function UsersTable() {
  const table = useTableState({ defaultSortKey: 'username', defaultSortDirection: 'asc' })
  const usersQuery = useUsers({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    sortBy: table.sortKey ?? 'username',
    sortDescending: table.sortDirection === 'desc',
  })

  const currentUserId = useSessionStore((state) => state.user?.userId)
  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManageUsers = hasPermission(Permissions.UserManage)

  const departmentsQuery = useDepartmentOptions({ enabled: canManageUsers })
  const rolesQuery = useRoleOptions({ enabled: canManageUsers })

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

  const roleByCode = useMemo(
    () => new Map((rolesQuery.data ?? []).map((role) => [role.code, role])),
    [rolesQuery.data],
  )

  const [formOpen, setFormOpen] = useState(false)
  const [formUser, setFormUser] = useState<UserResponse | null>(null)
  const [rolesUser, setRolesUser] = useState<UserResponse | null>(null)
  const [accessUser, setAccessUser] = useState<UserResponse | null>(null)
  const [resetUser, setResetUser] = useState<UserResponse | null>(null)
  const [statusUser, setStatusUser] = useState<UserResponse | null>(null)

  const statusMutation = useSetUserActiveMutation()

  const openCreate = () => {
    setFormUser(null)
    setFormOpen(true)
  }

  const openEdit = (user: UserResponse) => {
    setFormUser(user)
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
    if (!statusUser) return
    try {
      await statusMutation.mutateAsync({
        userId: statusUser.userId,
        active: !statusUser.isActive,
      })
      toast.success(statusUser.isActive ? 'تم إيقاف الحساب.' : 'تم تفعيل الحساب.')
      setStatusUser(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<UserResponse>[]>(
    () => [
      {
        id: 'username',
        header: 'اسم المستخدم',
        sortable: true,
        hideable: false,
        cell: (user) => (
          <div className="flex items-center gap-2">
            <span dir="ltr" className="font-mono text-sm text-foreground">
              {user.username}
            </span>
            {user.userId === currentUserId && <Badge variant="outline">أنت</Badge>}
          </div>
        ),
      },
      {
        id: 'department',
        header: 'القسم الأساسي',
        cell: (user) => {
          const department = departmentById.get(user.primaryDepartmentId)
          return department ? (
            <DepartmentBadge
              nameAr={department.nameAr}
              code={department.code}
              isActive={department.isActive}
            />
          ) : (
            <span
              dir="ltr"
              title={user.primaryDepartmentId}
              className="font-mono text-xs text-muted"
            >
              {formatShortId(user.primaryDepartmentId)}
            </span>
          )
        },
      },
      {
        id: 'scope',
        header: 'نطاق الوصول',
        cell: (user) => <UserScopeBadge scopeTypeId={user.scopeTypeId} />,
      },
      {
        id: 'roles',
        header: 'الأدوار',
        cell: (user) =>
          user.roles.length === 0 ? (
            <span className="text-muted">—</span>
          ) : (
            <span className="flex flex-wrap gap-1">
              {user.roles.map((code) => (
                <Badge key={code} variant="accent" title={code}>
                  {roleByCode.get(code)?.nameAr ?? code}
                </Badge>
              ))}
            </span>
          ),
      },
      {
        id: 'status',
        header: 'الحالة',
        sortable: true,
        sortKey: 'isActive',
        cell: (user) => (
          <Badge variant={user.isActive ? 'success' : 'danger'}>
            {user.isActive ? 'نشط' : 'موقوف'}
          </Badge>
        ),
      },
      {
        id: 'mustChangePassword',
        header: 'تغيير كلمة المرور',
        defaultHidden: true,
        cell: (user) =>
          user.mustChangePassword ? (
            <Badge variant="warning">مطلوب</Badge>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'employee',
        header: 'الموظف المرتبط',
        defaultHidden: true,
        cell: (user) =>
          user.employeeId ? (
            <span
              dir="ltr"
              title={user.employeeId}
              className="font-mono text-xs text-foreground"
            >
              {formatShortId(user.employeeId)}
            </span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'lastLoginAt',
        header: 'آخر دخول',
        sortable: true,
        cell: (user) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(user.lastLoginAt)}
          </span>
        ),
      },
      {
        id: 'createdAt',
        header: 'تاريخ الإنشاء',
        sortable: true,
        defaultHidden: true,
        cell: (user) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(user.createdAt)}
          </span>
        ),
      },
      {
        id: 'updatedAt',
        header: 'آخر تحديث',
        defaultHidden: true,
        cell: (user) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(user.updatedAt)}
          </span>
        ),
      },
    ],
    [currentUserId, departmentById, roleByCode],
  )

  const buildRowMenu = (user: UserResponse): MenuEntry[] => {
    const isSelf = user.userId === currentUserId

    return [
      {
        id: 'edit',
        label: 'تعديل البيانات',
        icon: <Pencil className="size-4" aria-hidden />,
        onSelect: () => openEdit(user),
      },
      {
        id: 'roles',
        label: 'إدارة الأدوار',
        icon: <ShieldCheck className="size-4" aria-hidden />,
        disabled: !canManageUsers,
        onSelect: () => setRolesUser(user),
      },
      {
        id: 'access',
        label: 'الصلاحيات والإسنادات',
        icon: <KeyRound className="size-4" aria-hidden />,
        onSelect: () => setAccessUser(user),
      },
      {
        id: 'reset-password',
        label: 'إعادة تعيين كلمة المرور',
        icon: <LockKeyhole className="size-4" aria-hidden />,
        disabled: isSelf,
        onSelect: () => setResetUser(user),
      },
      { type: 'separator' },
      {
        id: 'toggle-active',
        label: user.isActive ? 'إيقاف الحساب' : 'تفعيل الحساب',
        icon: user.isActive ? (
          <UserX className="size-4" aria-hidden />
        ) : (
          <UserCheck className="size-4" aria-hidden />
        ),
        danger: user.isActive,
        disabled: isSelf,
        onSelect: () => setStatusUser(user),
      },
      { type: 'separator' },
      {
        id: 'copy-username',
        label: 'نسخ اسم المستخدم',
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(user.username, 'اسم المستخدم'),
      },
      {
        id: 'copy-id',
        label: 'نسخ المعرّف',
        icon: <Hash className="size-4" aria-hidden />,
        onSelect: () => void copyValue(user.userId, 'المعرّف'),
      },
    ]
  }

  const headerMenu: MenuEntry[] = [
    {
      id: 'create',
      label: 'مستخدم جديد',
      icon: <Plus className="size-4" aria-hidden />,
      onSelect: openCreate,
    },
    {
      id: 'refresh',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void usersQuery.refetch(),
    },
  ]

  const emptyMenu: MenuEntry[] = [
    {
      id: 'create',
      label: 'مستخدم جديد',
      icon: <Plus className="size-4" aria-hidden />,
      onSelect: openCreate,
    },
    {
      id: 'refresh',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void usersQuery.refetch(),
    },
  ]

  return (
    <>
      <DataTable
        tableId="users-table"
        caption="قائمة المستخدمين"
        columns={columns}
        rows={usersQuery.data?.items}
        rowKey={(user) => user.userId}
        isLoading={usersQuery.isLoading}
        error={usersQuery.error}
        onRetry={() => void usersQuery.refetch()}
        onRefresh={() => void usersQuery.refetch()}
        isFetching={usersQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={usersQuery.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        searchPlaceholder="ابحث باسم المستخدم…"
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={emptyMenu}
        rowLabel={(user) => user.username}
        toolbar={
          <Button type="button" onClick={openCreate}>
            <Plus className="size-4" aria-hidden />
            مستخدم جديد
          </Button>
        }
        emptyTitle={table.search ? 'لا توجد نتائج مطابقة.' : 'لا يوجد مستخدمون بعد.'}
        emptyDescription={
          table.search
            ? 'جرّب تعديل عبارة البحث أو امسحها لعرض كل المستخدمين.'
            : 'ابدأ بإنشاء أول حساب مستخدم في النظام.'
        }
        emptyAction={
          table.search ? (
            <Button type="button" variant="outline" onClick={() => table.setSearch('')}>
              مسح البحث
            </Button>
          ) : (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              إنشاء مستخدم
            </Button>
          )
        }
      />

      {formOpen && (
        <UserFormDialog
          key={formUser?.userId ?? 'create'}
          open
          onOpenChange={setFormOpen}
          user={formUser}
        />
      )}

      {rolesUser && (
        <UserRolesDialog
          open
          onOpenChange={(open) => {
            if (!open) setRolesUser(null)
          }}
          user={rolesUser}
        />
      )}

      {accessUser && (
        <UserAccessDialog
          open
          onOpenChange={(open) => {
            if (!open) setAccessUser(null)
          }}
          user={accessUser}
        />
      )}

      {resetUser && (
        <ResetPasswordDialog
          open
          onOpenChange={(open) => {
            if (!open) setResetUser(null)
          }}
          user={resetUser}
        />
      )}

      <ConfirmDialog
        open={statusUser !== null}
        onOpenChange={(open) => {
          if (!open) setStatusUser(null)
        }}
        title={statusUser?.isActive ? 'إيقاف حساب المستخدم' : 'تفعيل حساب المستخدم'}
        description={
          statusUser
            ? statusUser.isActive
              ? `سيمنع «${statusUser.username}» من تسجيل الدخول حتى إعادة تفعيله.`
              : `سيعود «${statusUser.username}» قادرًا على تسجيل الدخول من جديد.`
            : undefined
        }
        confirmLabel={statusUser?.isActive ? 'إيقاف الحساب' : 'تفعيل الحساب'}
        destructive={statusUser?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
