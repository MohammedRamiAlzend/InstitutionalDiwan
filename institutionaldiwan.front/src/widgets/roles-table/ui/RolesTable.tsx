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
import { formatDate, t, useTableState } from '@/shared/lib'
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
      toast.success(t('role.toast.copied', { label }))
    } catch {
      toast.error(t('role.toast.copy-failed'))
    }
  }

  const handleToggleStatus = async () => {
    if (!statusRole) return
    try {
      await statusMutation.mutateAsync({
        roleId: statusRole.roleId,
        active: !statusRole.isActive,
      })
      toast.success(statusRole.isActive ? t('role.toast.suspended') : t('role.toast.activated'))
      setStatusRoleId(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<RoleResponse>[]>(
    () => [
      {
        id: 'role',
        headerKey: 'role.column.role',
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
        headerKey: 'role.column.description',
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
        headerKey: 'role.column.permissions',
        sortable: true,
        sortValue: (role) => role.permissions.length,
        cell: (role) => (
          <Badge variant={role.permissions.length > 0 ? 'accent' : 'outline'}>
            {t('role.permission-count', { count: role.permissions.length })}
          </Badge>
        ),
      },
      {
        id: 'status',
        headerKey: 'role.column.status',
        cell: (role) => (
          <Badge variant={role.isActive ? 'success' : 'danger'}>
            {role.isActive ? t('role.status.active') : t('role.status.suspended')}
          </Badge>
        ),
      },
      {
        id: 'kind',
        headerKey: 'role.column.kind',
        cell: (role) =>
          role.isSystemRole ? (
            <Badge variant="outline"><Label k="role.kind.system" /></Badge>
          ) : (
            <span className="text-xs text-muted"><Label k="role.kind.custom" /></span>
          ),
      },
      {
        id: 'createdAt',
        headerKey: 'role.column.created-at',
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
      label: t('role.menu.edit'),
      icon: <Pencil className="size-4" aria-hidden />,
      onSelect: () => openEdit(role),
    },
    {
      id: 'permissions',
      label: t('role.menu.permissions'),
      icon: <KeyRound className="size-4" aria-hidden />,
      onSelect: () => setPermissionsRoleId(role.roleId),
    },
    { type: 'separator' },
    {
      id: 'toggle-active',
      label: role.isActive ? t('role.menu.suspend') : t('role.menu.activate'),
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
      label: t('role.menu.copy-code'),
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyValue(role.code, t('role.copy.code')),
    },
    {
      id: 'copy-id',
      label: t('role.menu.copy-id'),
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyValue(role.roleId, t('role.copy.id')),
    },
  ]

  const createEntry: MenuEntry = {
    id: 'create',
    label: t('role.create'),
    icon: <Plus className="size-4" aria-hidden />,
    onSelect: openCreate,
  }
  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: t('table.refresh'),
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void rolesQuery.refetch(),
  }

  return (
    <>
      <DataTable
        tableId="roles-table"
        mode="client"
        caption={t('role.caption')}
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
        searchPlaceholder={t('role.search-placeholder')}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        rowMenu={buildRowMenu}
        headerMenu={[createEntry, refreshEntry]}
        emptyMenu={[createEntry, refreshEntry]}
        rowLabel={(role) => role.nameAr}
        toolbar={
          <>
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              <Label k="role.create" />
            </Button>
            <GuideChip id="roles.create" />
          </>
        }
        emptyTitle={table.search ? t('role.empty.search-title') : t('role.empty.title')}
        emptyDescription={
          table.search
            ? t('role.empty.search-description')
            : t('role.empty.description')
        }
        emptyAction={
          table.search ? (
            <Button type="button" variant="outline" onClick={() => table.setSearch('')}>
              <Label k="role.empty.clear-search" />
            </Button>
          ) : (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              <Label k="role.empty.create" />
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
        title={statusRole?.isActive ? t('role.confirm.suspend-title') : t('role.confirm.activate-title')}
        description={
          statusRole
            ? statusRole.isActive
              ? t('role.confirm.suspend-description', { name: statusRole.nameAr })
              : t('role.confirm.activate-description', { name: statusRole.nameAr })
            : undefined
        }
        confirmLabel={statusRole?.isActive ? t('role.confirm.suspend') : t('role.confirm.activate')}
        destructive={statusRole?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
