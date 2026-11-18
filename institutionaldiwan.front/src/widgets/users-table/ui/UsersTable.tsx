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
import { DepartmentBadge, useDepartmentOptions, DepartmentLabel } from '@/entities/department'
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
  t,
  useTableState,
} from '@/shared/lib'
import {
  Badge,
  Button,
  ConfirmDialog,
  DataTable,
  GuideChip,
  Label,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'
import { EmployeeLabel } from '@/entities/employee'

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
      toast.success(t('user.toast.copied', { label }))
    } catch {
      toast.error(t('user.toast.copy-failed'))
    }
  }

  const handleToggleStatus = async () => {
    if (!statusUser) return
    try {
      await statusMutation.mutateAsync({
        userId: statusUser.userId,
        active: !statusUser.isActive,
      })
      toast.success(statusUser.isActive ? t('user.toast.suspended') : t('user.toast.activated'))
      setStatusUser(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<UserResponse>[]>(
    () => [
      {
        id: 'username',
        headerKey: 'user.column.username',
        sortable: true,
        hideable: false,
        cell: (user) => (
          <div className="flex items-center gap-2">
            <span dir="ltr" className="font-mono text-sm text-foreground">
              {user.username}
            </span>
            {user.userId === currentUserId && <Badge variant="outline"><Label k="user.self-badge" /></Badge>}
          </div>
        ),
      },
      {
        id: 'department',
        headerKey: 'user.column.department',
        cell: (user) => {
          const department = departmentById.get(user.primaryDepartmentId)
          return department ? (
            <DepartmentBadge
              nameAr={department.nameAr}
              code={department.code}
              isActive={department.isActive}
            />
          ) : (
            <DepartmentLabel departmentId={user.primaryDepartmentId} />
          )
        },
      },
      {
        id: 'scope',
        headerKey: 'user.column.scope',
        cell: (user) => <UserScopeBadge scopeTypeId={user.scopeTypeId} />,
      },
      {
        id: 'roles',
        headerKey: 'user.column.roles',
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
        headerKey: 'user.column.status',
        sortable: true,
        sortKey: 'isActive',
        cell: (user) => (
          <Badge variant={user.isActive ? 'success' : 'danger'}>
            {user.isActive ? t('user.status.active') : t('user.status.suspended')}
          </Badge>
        ),
      },
      {
        id: 'mustChangePassword',
        headerKey: 'user.column.must-change',
        defaultHidden: true,
        cell: (user) =>
          user.mustChangePassword ? (
            <Badge variant="warning"><Label k="user.must-change.required" /></Badge>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'employee',
        headerKey: 'user.column.employee',
        defaultHidden: true,
        cell: (user) =>
          user.employeeId ? (
            <EmployeeLabel employeeId={user.employeeId} />
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'lastLoginAt',
        headerKey: 'user.column.last-login',
        sortable: true,
        cell: (user) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(user.lastLoginAt)}
          </span>
        ),
      },
      {
        id: 'createdAt',
        headerKey: 'user.column.created-at',
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
        headerKey: 'user.column.updated-at',
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
        label: t('user.menu.edit'),
        icon: <Pencil className="size-4" aria-hidden />,
        onSelect: () => openEdit(user),
      },
      {
        id: 'roles',
        label: t('user.menu.roles'),
        icon: <ShieldCheck className="size-4" aria-hidden />,
        disabled: !canManageUsers,
        onSelect: () => setRolesUser(user),
      },
      {
        id: 'access',
        label: t('user.menu.access'),
        icon: <KeyRound className="size-4" aria-hidden />,
        onSelect: () => setAccessUser(user),
      },
      {
        id: 'reset-password',
        label: t('user.menu.reset-password'),
        icon: <LockKeyhole className="size-4" aria-hidden />,
        disabled: isSelf,
        onSelect: () => setResetUser(user),
      },
      { type: 'separator' },
      {
        id: 'toggle-active',
        label: user.isActive ? t('user.menu.suspend') : t('user.menu.activate'),
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
        label: t('user.menu.copy-username'),
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(user.username, t('user.copy.username')),
      },
      {
        id: 'copy-id',
        label: t('user.menu.copy-id'),
        icon: <Hash className="size-4" aria-hidden />,
        onSelect: () => void copyValue(user.userId, t('user.copy.id')),
      },
    ]
  }

  const headerMenu: MenuEntry[] = [
    {
      id: 'create',
      label: t('user.create'),
      icon: <Plus className="size-4" aria-hidden />,
      onSelect: openCreate,
    },
    {
      id: 'refresh',
      label: t('table.refresh'),
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void usersQuery.refetch(),
    },
  ]

  const emptyMenu: MenuEntry[] = [
    {
      id: 'create',
      label: t('user.create'),
      icon: <Plus className="size-4" aria-hidden />,
      onSelect: openCreate,
    },
    {
      id: 'refresh',
      label: t('table.refresh'),
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void usersQuery.refetch(),
    },
  ]

  return (
    <>
      <DataTable
        tableId="users-table"
        caption={t('user.caption')}
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
        searchPlaceholder={t('user.search-placeholder')}
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={emptyMenu}
        rowLabel={(user) => user.username}
        toolbar={
          <>
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              <Label k="user.create" />
            </Button>
            <GuideChip id="users.create" />
          </>
        }
        emptyTitle={table.search ? t('user.empty.search-title') : t('user.empty.title')}
        emptyDescription={
          table.search
            ? t('user.empty.search-description')
            : t('user.empty.description')
        }
        emptyAction={
          table.search ? (
            <Button type="button" variant="outline" onClick={() => table.setSearch('')}>
              <Label k="user.empty.clear-search" />
            </Button>
          ) : (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              <Label k="user.empty.create" />
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
        title={statusUser?.isActive ? t('user.confirm.suspend-title') : t('user.confirm.activate-title')}
        description={
          statusUser
            ? statusUser.isActive
              ? t('user.confirm.suspend-description', { name: statusUser.username })
              : t('user.confirm.activate-description', { name: statusUser.username })
            : undefined
        }
        confirmLabel={statusUser?.isActive ? t('user.confirm.suspend') : t('user.confirm.activate')}
        destructive={statusUser?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
