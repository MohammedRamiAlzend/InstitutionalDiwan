import { useMemo, useState } from 'react'
import {
  Copy,
  Eye,
  Hash,
  Pencil,
  Plus,
  RefreshCw,
  ShieldCheck,
  ShieldOff,
} from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import {
  findDuplicateActiveScopes,
  registryScopeRank,
  useNumberingRegistries,
  type NumberingRegistryResponse,
} from '@/entities/numbering-registry'
import { useSessionStore } from '@/entities/user'
import {
  RegistryFormDialog,
  useSetNumberingRegistryActiveMutation,
} from '@/features/numbering-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions, NUMBERING_RESET_POLICY, type LabelKey } from '@/shared/config'
import { formatDate, formatNumber, resolveLabel, t, useLabelsStore, useTableState } from '@/shared/lib'
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
import { RegistryDetailsDialog } from './RegistryDetailsDialog'

const resetPolicyKeys: Record<number, LabelKey> = {
  [NUMBERING_RESET_POLICY.Yearly]: 'numbering.reset-policy.yearly',
  [NUMBERING_RESET_POLICY.Monthly]: 'numbering.reset-policy.monthly',
  [NUMBERING_RESET_POLICY.Never]: 'numbering.reset-policy.never',
}

/** `/numbering` grid: registry scope, template, counters, and status actions. */
export function NumberingRegistryTable() {
  const table = useTableState({
    defaultSortKey: 'registry',
    defaultSortDirection: 'asc',
    filterKeys: ['status', 'resetPolicyId', 'scope'],
  })
  const overrides = useLabelsStore((state) => state.overrides)
  const registriesQuery = useNumberingRegistries()
  const registries = useMemo(() => registriesQuery.data ?? [], [registriesQuery.data])

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManage = hasPermission(Permissions.NumberingManage)

  const departmentsQuery = useDepartmentOptions()
  const documentTypesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)
  const directionsQuery = useLookupEntries(LOOKUP_SLUGS.DocumentDirections)
  const resetPoliciesQuery = useLookupEntries(LOOKUP_SLUGS.NumberingResetPolicies)

  const statusFilterOptions = useMemo(
    () => [
      { value: 'all', label: resolveLabel('numbering.filter.status-all', overrides) },
      { value: 'active', label: resolveLabel('numbering.status.active', overrides) },
      { value: 'inactive', label: resolveLabel('numbering.status.inactive', overrides) },
    ],
    [overrides],
  )

  const scopeFilterOptions = useMemo(
    () => [
      { value: 'all', label: resolveLabel('numbering.filter.scope-all', overrides) },
      { value: 'global', label: resolveLabel('numbering.filter.scope-global', overrides) },
      { value: 'department', label: resolveLabel('numbering.filter.scope-department', overrides) },
      { value: 'documentType', label: resolveLabel('numbering.filter.scope-document-type', overrides) },
    ],
    [overrides],
  )

  const departmentNames = useMemo(
    () =>
      new Map(
        (departmentsQuery.data ?? []).map((department) => [
          department.departmentId,
          department.nameAr,
        ]),
      ),
    [departmentsQuery.data],
  )
  const documentTypeLabels = useMemo(
    () =>
      new Map(
        (documentTypesQuery.data ?? []).map((entry) => [entry.id, entry.descAr]),
      ),
    [documentTypesQuery.data],
  )
  const directionLabels = useMemo(
    () =>
      new Map(
        (directionsQuery.data ?? []).map((entry) => [entry.id, entry.descAr]),
      ),
    [directionsQuery.data],
  )

  const resetPolicyOptions = useMemo(
    () => [
      { value: 'all', label: resolveLabel('numbering.filter.reset-policy-all', overrides) },
      ...(resetPoliciesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [resetPoliciesQuery.data, overrides],
  )

  const filteredRegistries = useMemo(
    () =>
      registries.filter((registry) => {
        const { status, resetPolicyId, scope } = table.filters
        if (status === 'active' && !registry.isActive) return false
        if (status === 'inactive' && registry.isActive) return false
        if (resetPolicyId && String(registry.resetPolicyId) !== resetPolicyId) return false
        if (scope === 'global' && registryScopeRank(registry) !== 0) return false
        if (scope === 'department' && registry.departmentId === null) return false
        if (scope === 'documentType' && registry.documentTypeId === null) return false
        return true
      }),
    [registries, table.filters],
  )

  const duplicateGroups = useMemo(
    () => findDuplicateActiveScopes(registries),
    [registries],
  )

  const duplicateCodesFor = (registry: NumberingRegistryResponse): string[] => {
    const group = duplicateGroups.find((candidate) =>
      candidate.registries.some((member) => member.registryId === registry.registryId),
    )
    return (
      group?.registries
        .filter((member) => member.registryId !== registry.registryId)
        .map((member) => member.code) ?? []
    )
  }

  const [formOpen, setFormOpen] = useState(false)
  const [formRegistry, setFormRegistry] = useState<NumberingRegistryResponse | null>(
    null,
  )
  const [detailsRegistryId, setDetailsRegistryId] = useState<string | null>(null)
  const [statusRegistryId, setStatusRegistryId] = useState<string | null>(null)

  const detailsRegistry =
    registries.find((registry) => registry.registryId === detailsRegistryId) ?? null
  const statusRegistry =
    registries.find((registry) => registry.registryId === statusRegistryId) ?? null

  const statusMutation = useSetNumberingRegistryActiveMutation()

  const openCreate = () => {
    setFormRegistry(null)
    setFormOpen(true)
  }

  const openEdit = (registry: NumberingRegistryResponse) => {
    setFormRegistry(registry)
    setFormOpen(true)
  }

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('numbering.toast.copied', { label }))
    } catch {
      toast.error(t('numbering.toast.copy-failed'))
    }
  }

  const handleToggleStatus = async () => {
    if (!statusRegistry) return
    try {
      await statusMutation.mutateAsync({
        registryId: statusRegistry.registryId,
        active: !statusRegistry.isActive,
      })
      toast.success(
        statusRegistry.isActive ? t('numbering.toast.suspended') : t('numbering.toast.activated'),
      )
      setStatusRegistryId(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<NumberingRegistryResponse>[]>(
    () => [
      {
        id: 'registry',
        headerKey: 'numbering.column.registry',
        hideable: false,
        sortable: true,
        sortValue: (registry) => registry.code,
        cell: (registry) => (
          <div className="flex flex-col gap-0.5">
            <code dir="ltr" className="text-xs font-semibold text-foreground">
              {registry.code}
            </code>
            <span className="text-xs text-muted">{registry.nameAr}</span>
          </div>
        ),
      },
      {
        id: 'scope',
        headerKey: 'numbering.column.scope',
        cell: (registry) => {
          const chips = [
            registry.departmentId
              ? departmentNames.get(registry.departmentId)
              : null,
            registry.documentTypeId
              ? documentTypeLabels.get(registry.documentTypeId)
              : null,
            registry.directionId ? directionLabels.get(registry.directionId) : null,
          ].filter((label): label is string => Boolean(label))

          return chips.length === 0 ? (
            <Badge variant="outline">{t('numbering.scope.global-badge')}</Badge>
          ) : (
            <div className="flex max-w-72 flex-wrap gap-1">
              {chips.map((label) => (
                <Badge key={label} variant="outline">
                  {label}
                </Badge>
              ))}
            </div>
          )
        },
      },
      {
        id: 'specificity',
        headerKey: 'numbering.column.specificity',
        sortable: true,
        sortValue: (registry) => registryScopeRank(registry),
        cell: (registry) => {
          const rank = registryScopeRank(registry)
          return rank === 0 ? (
            <Badge variant="outline">{t('numbering.scope.global-badge')}</Badge>
          ) : (
            <span className="whitespace-nowrap text-xs text-muted">
              {t('numbering.column.specificity-value', { rank: formatNumber(rank) })}
            </span>
          )
        },
      },
      {
        id: 'format',
        headerKey: 'numbering.column.format',
        cell: (registry) => (
          <code dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {registry.formatPattern}
          </code>
        ),
      },
      {
        id: 'resetPolicy',
        headerKey: 'numbering.column.reset-policy',
        cell: (registry) => (
          <span className="text-xs text-foreground">
            {resetPolicyKeys[registry.resetPolicyId]
              ? t(resetPolicyKeys[registry.resetPolicyId])
              : `#${registry.resetPolicyId}`}
          </span>
        ),
      },
      {
        id: 'counters',
        headerKey: 'numbering.column.counters',
        sortable: true,
        sortValue: (registry) => registry.counters.length,
        cell: (registry) => {
          const latest = registry.counters[0]

          if (!latest) {
            return <span className="text-xs text-muted">{t('numbering.column.no-counters')}</span>
          }

          return (
            <div className="flex flex-col gap-0.5 text-xs">
              <span className="text-foreground">
                <span dir="ltr">{latest.numberingYear}</span> ·{' '}
                <span dir="ltr">{formatNumber(latest.lastValue)}</span>
              </span>
              <span className="text-muted">
                {t('numbering.column.counters-years', { count: registry.counters.length })}
              </span>
            </div>
          )
        },
      },
      {
        id: 'status',
        headerKey: 'numbering.column.status',
        cell: (registry) => (
          <Badge variant={registry.isActive ? 'success' : 'danger'}>
            {registry.isActive ? t('numbering.status.active') : t('numbering.status.inactive')}
          </Badge>
        ),
      },
      {
        id: 'createdAt',
        headerKey: 'numbering.column.created-at',
        defaultHidden: true,
        sortable: true,
        sortValue: (registry) => registry.createdAt,
        cell: (registry) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(registry.createdAt)}
          </span>
        ),
      },
    ],
    // Label maps are dependencies of the scope column.
    [departmentNames, documentTypeLabels, directionLabels],
  )

  const buildRowMenu = (registry: NumberingRegistryResponse): MenuEntry[] => {
    const entries: MenuEntry[] = [
      {
        id: 'details',
        label: t('numbering.menu.details'),
        icon: <Eye className="size-4" aria-hidden />,
        onSelect: () => setDetailsRegistryId(registry.registryId),
      },
    ]

    if (canManage) {
      entries.push(
        {
          id: 'edit',
          label: t('numbering.menu.edit'),
          icon: <Pencil className="size-4" aria-hidden />,
          onSelect: () => openEdit(registry),
        },
        {
          id: 'toggle-active',
          label: registry.isActive ? t('numbering.menu.suspend') : t('numbering.menu.activate'),
          icon: registry.isActive ? (
            <ShieldOff className="size-4" aria-hidden />
          ) : (
            <ShieldCheck className="size-4" aria-hidden />
          ),
          danger: registry.isActive,
          onSelect: () => setStatusRegistryId(registry.registryId),
        },
      )
    }

    entries.push(
      { type: 'separator' },
      {
        id: 'copy-code',
        label: t('numbering.menu.copy-code'),
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(registry.code, t('numbering.copy.code')),
      },
      {
        id: 'copy-id',
        label: t('numbering.menu.copy-id'),
        icon: <Hash className="size-4" aria-hidden />,
        onSelect: () => void copyValue(registry.registryId, t('numbering.copy.id')),
      },
    )

    return entries
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: t('table.refresh'),
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void registriesQuery.refetch(),
  }
  const createEntry: MenuEntry = {
    id: 'create',
    label: t('numbering.create'),
    icon: <Plus className="size-4" aria-hidden />,
    onSelect: openCreate,
  }
  const headerMenu: MenuEntry[] = canManage
    ? [createEntry, refreshEntry]
    : [refreshEntry]

  const hasQuery = Boolean(table.search) || table.hasActiveFilters

  return (
    <>
      {duplicateGroups.length > 0 && (
        <div
          role="status"
          className="flex flex-col gap-1 rounded-lg border border-warning/40 bg-warning/10 px-3 py-2 text-xs text-warning"
        >
          <p className="font-semibold"><Label k="numbering.duplicate.title" /></p>
          {duplicateGroups.map((group) => (
            <p key={group.key}>
              {t('numbering.duplicate.item', {
                codes: group.registries.map((member) => member.code).join('، '),
              })}
            </p>
          ))}
          <p><Label k="numbering.duplicate.hint" /></p>
        </div>
      )}

      <DataTable
        tableId="numbering-registries"
        mode="client"
        caption={t('numbering.caption')}
        columns={columns}
        rows={filteredRegistries}
        rowKey={(registry) => registry.registryId}
        rowSearchText={(registry) =>
          [
            registry.code,
            registry.nameAr,
            registry.formatPattern,
            registry.departmentId ? departmentNames.get(registry.departmentId) : null,
            registry.documentTypeId
              ? documentTypeLabels.get(registry.documentTypeId)
              : null,
            registry.directionId ? directionLabels.get(registry.directionId) : null,
          ]
            .filter(Boolean)
            .join(' ')
            .toLocaleLowerCase('ar')
        }
        isLoading={registriesQuery.isLoading}
        error={registriesQuery.error}
        onRetry={() => void registriesQuery.refetch()}
        onRefresh={() => void registriesQuery.refetch()}
        isFetching={registriesQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={filteredRegistries.length}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        searchPlaceholder={t('numbering.search-placeholder')}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(registry) => registry.code}
        toolbar={
          canManage ? (
            <>
              <Button type="button" onClick={openCreate}>
                <Plus className="size-4" aria-hidden />
                <Label k="numbering.toolbar.create" />
              </Button>
              <GuideChip id="numbering.create" />
            </>
          ) : undefined
        }
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <GuideChip id="numbering.filters" />
            <Select
              aria-label={t('numbering.filter.status-aria')}
              value={table.filters.status || 'all'}
              onValueChange={(value) =>
                table.setFilter('status', value === 'all' ? null : value)
              }
              options={statusFilterOptions}
              className="w-40"
            />
            <Select
              aria-label={t('numbering.filter.reset-policy-aria')}
              value={table.filters.resetPolicyId || 'all'}
              onValueChange={(value) =>
                table.setFilter('resetPolicyId', value === 'all' ? null : value)
              }
              options={resetPolicyOptions}
              className="w-44"
            />
            <Select
              aria-label={t('numbering.filter.scope-aria')}
              value={table.filters.scope || 'all'}
              onValueChange={(value) =>
                table.setFilter('scope', value === 'all' ? null : value)
              }
              options={scopeFilterOptions}
              className="w-48"
            />
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                <Label k="numbering.filter.clear" />
              </Button>
            )}
          </div>
        }
        emptyTitle={
          hasQuery ? t('numbering.empty.search-title') : t('numbering.empty.title')
        }
        emptyDescription={
          hasQuery
            ? t('numbering.empty.search-description')
            : canManage
              ? t('numbering.empty.description')
              : t('numbering.empty.description-readonly')
        }
        emptyAction={
          hasQuery ? (
            <Button
              type="button"
              variant="outline"
              onClick={table.clearQuery}
            >
              <Label k="numbering.empty.clear" />
            </Button>
          ) : canManage ? (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              <Label k="numbering.empty.create" />
            </Button>
          ) : undefined
        }
      />

      {formOpen && (
        <RegistryFormDialog
          key={formRegistry?.registryId ?? 'create'}
          open
          registry={formRegistry}
          onOpenChange={setFormOpen}
        />
      )}

      {detailsRegistry && (
        <RegistryDetailsDialog
          open
          registry={detailsRegistry}
          departmentName={
            detailsRegistry.departmentId
              ? departmentNames.get(detailsRegistry.departmentId)
              : null
          }
          documentTypeLabel={
            detailsRegistry.documentTypeId
              ? documentTypeLabels.get(detailsRegistry.documentTypeId)
              : null
          }
          directionLabel={
            detailsRegistry.directionId
              ? directionLabels.get(detailsRegistry.directionId)
              : null
          }
          duplicateCodes={duplicateCodesFor(detailsRegistry)}
          onOpenChange={(open) => {
            if (!open) setDetailsRegistryId(null)
          }}
        />
      )}

      <ConfirmDialog
        open={statusRegistry !== null}
        onOpenChange={(open) => {
          if (!open) setStatusRegistryId(null)
        }}
        title={statusRegistry?.isActive ? t('numbering.confirm.suspend-title') : t('numbering.confirm.activate-title')}
        description={
          statusRegistry
            ? statusRegistry.isActive
              ? t('numbering.confirm.suspend-description', { code: statusRegistry.code })
              : t('numbering.confirm.activate-description', { code: statusRegistry.code })
            : undefined
        }
        confirmLabel={statusRegistry?.isActive ? t('numbering.menu.suspend') : t('numbering.menu.activate')}
        destructive={statusRegistry?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}