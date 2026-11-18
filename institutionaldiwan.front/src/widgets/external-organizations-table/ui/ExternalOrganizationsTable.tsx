import { useMemo, useState } from 'react'
import { Copy, Hash, Pencil, Plus, RefreshCw, UserCheck, UserX } from 'lucide-react'
import { toast } from 'sonner'
import {
  useExternalOrganizations,
  type ExternalOrganizationResponse,
} from '@/entities/external-organization'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import {
  ExternalOrganizationFormDialog,
  useSetExternalOrganizationActiveMutation,
} from '@/features/external-organization-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { formatDateTime, t, useLabel, useTableState } from '@/shared/lib'
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

/** External correspondents administration grid (server search + paging). */
export function ExternalOrganizationsTable() {
  const table = useTableState({ filterKeys: ['include'] })
  const includeInactive = table.filters.include === 'all'
  const activeLabel = useLabel('external-org.filter.active')
  const allLabel = useLabel('external-org.filter.all')
  const includeOptions = useMemo(
    () => [
      { value: 'active', label: activeLabel },
      { value: 'all', label: allLabel },
    ],
    [activeLabel, allLabel],
  )

  const organizationsQuery = useExternalOrganizations({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    includeInactive,
  })

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canEdit = hasPermission(Permissions.DocumentEdit)

  const [formOpen, setFormOpen] = useState(false)
  const [formOrganization, setFormOrganization] =
    useState<ExternalOrganizationResponse | null>(null)
  const [statusOrganization, setStatusOrganization] =
    useState<ExternalOrganizationResponse | null>(null)

  const statusMutation = useSetExternalOrganizationActiveMutation()

  const openCreate = () => {
    setFormOrganization(null)
    setFormOpen(true)
  }

  const openEdit = (organization: ExternalOrganizationResponse) => {
    setFormOrganization(organization)
    setFormOpen(true)
  }

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('external-org.toast.copied', { label }))
    } catch {
      toast.error(t('external-org.toast.copy-failed'))
    }
  }

  const handleToggleStatus = async () => {
    if (!statusOrganization) return
    try {
      await statusMutation.mutateAsync({
        externalOrganizationId: statusOrganization.externalOrganizationId,
        active: !statusOrganization.isActive,
      })
      toast.success(
        statusOrganization.isActive
          ? t('external-org.toast.suspended')
          : t('external-org.toast.activated'),
      )
      setStatusOrganization(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<ExternalOrganizationResponse>[]>(
    () => [
      {
        id: 'code',
        headerKey: 'external-org.column.code',
        hideable: false,
        cell: (organization) => (
          <span dir="ltr" className="font-mono text-xs text-foreground">
            {organization.code}
          </span>
        ),
      },
      {
        id: 'organization',
        headerKey: 'external-org.column.organization',
        hideable: false,
        cell: (organization) => (
          <div className="flex flex-col gap-0.5">
            <span className="text-sm font-medium text-foreground">
              {organization.nameAr}
            </span>
            {organization.nameEn && (
              <span dir="ltr" className="text-xs text-muted">
                {organization.nameEn}
              </span>
            )}
          </div>
        ),
      },
      {
        id: 'type',
        headerKey: 'external-org.column.type',
        cell: (organization) => (
          <LookupLabel
            lookup={LOOKUP_SLUGS.OrganizationTypes}
            id={organization.organizationTypeId}
            className="text-xs"
          />
        ),
      },
      {
        id: 'contact',
        headerKey: 'external-org.column.contact',
        defaultHidden: true,
        cell: (organization) =>
          organization.contactInfo ? (
            <span
              dir="ltr"
              title={organization.contactInfo}
              className="block max-w-64 truncate text-xs text-foreground"
            >
              {organization.contactInfo}
            </span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'status',
        headerKey: 'external-org.column.status',
        cell: (organization) => (
          <Badge variant={organization.isActive ? 'success' : 'danger'}>
            {organization.isActive ? t('external-org.status.active') : t('external-org.status.suspended')}
          </Badge>
        ),
      },
      {
        id: 'updatedAt',
        headerKey: 'external-org.column.updated-at',
        defaultHidden: true,
        cell: (organization) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {organization.updatedAt
              ? formatDateTime(organization.updatedAt)
              : '—'}
          </span>
        ),
      },
      {
        id: 'createdAt',
        headerKey: 'external-org.column.created-at',
        defaultHidden: true,
        cell: (organization) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(organization.createdAt)}
          </span>
        ),
      },
    ],
    [],
  )

  const buildRowMenu = (organization: ExternalOrganizationResponse): MenuEntry[] => {
    const menu: MenuEntry[] = []

    if (canEdit) {
      menu.push(
        {
          id: 'edit',
          label: t('external-org.menu.edit'),
          icon: <Pencil className="size-4" aria-hidden />,
          onSelect: () => openEdit(organization),
        },
        { type: 'separator' },
        {
          id: 'toggle-active',
          label: organization.isActive ? t('external-org.menu.suspend') : t('external-org.menu.activate'),
          icon: organization.isActive ? (
            <UserX className="size-4" aria-hidden />
          ) : (
            <UserCheck className="size-4" aria-hidden />
          ),
          danger: organization.isActive,
          onSelect: () => setStatusOrganization(organization),
        },
        { type: 'separator' },
      )
    }

    menu.push(
      {
        id: 'copy-code',
        label: t('external-org.menu.copy-code'),
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(organization.code, t('external-org.copy.code')),
      },
      {
        id: 'copy-id',
        label: t('external-org.menu.copy-id'),
        icon: <Hash className="size-4" aria-hidden />,
        onSelect: () => void copyValue(organization.externalOrganizationId, t('external-org.copy.id')),
      },
    )

    return menu
  }

  const headerMenu: MenuEntry[] = [
    ...(canEdit
      ? [
          {
            id: 'create',
            label: t('external-org.create'),
            icon: <Plus className="size-4" aria-hidden />,
            onSelect: openCreate,
          } satisfies MenuEntry,
          { type: 'separator' } satisfies MenuEntry,
        ]
      : []),
    {
      id: 'refresh',
      label: t('table.refresh'),
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void organizationsQuery.refetch(),
    },
  ]

  return (
    <>
      <DataTable
        tableId="external-organizations-table"
        caption={t('external-org.caption')}
        columns={columns}
        rows={organizationsQuery.data?.items}
        rowKey={(organization) => organization.externalOrganizationId}
        isLoading={organizationsQuery.isLoading}
        error={organizationsQuery.error}
        onRetry={() => void organizationsQuery.refetch()}
        onRefresh={() => void organizationsQuery.refetch()}
        isFetching={organizationsQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={organizationsQuery.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        filters={
          <Select
            aria-label={t('external-org.filter.aria')}
            value={table.filters.include === 'all' ? 'all' : 'active'}
            onValueChange={(value) =>
              table.setFilter('include', value === 'all' ? 'all' : null)
            }
            options={includeOptions}
          />
        }
        searchPlaceholder={t('external-org.search-placeholder')}
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(organization) => organization.nameAr}
        toolbar={
          canEdit ? (
            <>
              <Button type="button" onClick={openCreate}>
                <Plus className="size-4" aria-hidden />
                <Label k="external-org.create" />
              </Button>
              <GuideChip id="documents.org-create" />
            </>
          ) : undefined
        }
        emptyTitle={
          table.search || includeInactive
            ? t('external-org.empty.search-title')
            : t('external-org.empty.title')
        }
        emptyDescription={
          table.search || includeInactive
            ? t('external-org.empty.search-description')
            : t('external-org.empty.description')
        }
        emptyAction={
          table.search || includeInactive ? (
            <Button
              type="button"
              variant="outline"
              onClick={() => table.clearQuery()}
            >
              <Label k="external-org.empty.clear" />
            </Button>
          ) : canEdit ? (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              <Label k="external-org.empty.add" />
            </Button>
          ) : undefined
        }
      />

      {formOpen && (
        <ExternalOrganizationFormDialog
          key={formOrganization?.externalOrganizationId ?? 'create'}
          open
          onOpenChange={setFormOpen}
          organization={formOrganization}
        />
      )}

      <ConfirmDialog
        open={statusOrganization !== null}
        onOpenChange={(open) => {
          if (!open) setStatusOrganization(null)
        }}
        title={statusOrganization?.isActive ? t('external-org.confirm.suspend-title') : t('external-org.confirm.activate-title')}
        description={
          statusOrganization
            ? statusOrganization.isActive
              ? t('external-org.confirm.suspend-description', { name: statusOrganization.nameAr })
              : t('external-org.confirm.activate-description', { name: statusOrganization.nameAr })
            : undefined
        }
        confirmLabel={statusOrganization?.isActive ? t('external-org.menu.suspend') : t('external-org.menu.activate')}
        destructive={statusOrganization?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
