import { useMemo, useState } from 'react'
import { Copy, Hash, RefreshCw, ShieldCheck, Stamp } from 'lucide-react'
import { toast } from 'sonner'
import {
  AUDIT_MODULES,
  auditModuleLabel,
  HashChainPill,
  useAuditLogs,
  type AuditLogResponse,
  type AuditLogSortKey,
} from '@/entities/audit-log'
import { LOOKUP_SLUGS, LookupLabel, useLookupEntries } from '@/entities/lookup'
import { useSessionStore, UserLabel } from '@/entities/user'
import {
  CreateCheckpointButton,
  CreateCheckpointDialog,
  RunIntegrityCheckDialog,
} from '@/features/audit-manage'
import { Permissions } from '@/shared/config'
import { formatDateTime, formatNumber, formatShortId, resolveLabel, t, useLabelsStore, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  GuideChip,
  Input,
  Label,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'
import { DepartmentLabel } from '@/entities/department'

function optionalId(value: string | undefined): number | undefined {
  if (!value) return undefined
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined
}

function toSortKey(value: string | null): AuditLogSortKey {
  return value === 'module' || value === 'action' || value === 'entity' ? value : 'occurredAt'
}

/** `yyyy-MM-dd` from the date input promoted to a UTC day boundary. */
function dayBoundary(value: string | undefined, edge: 'start' | 'end'): string | undefined {
  if (!value) return undefined
  return `${value}T${edge === 'start' ? '00:00:00' : '23:59:59'}Z`
}

function compactId(value: string): string {
  return value.length <= 24 ? value : `${value.slice(0, 10)}…${value.slice(-6)}`
}

/**
 * Module 09 audit search grid: server-side filters/sort/paging in the URL,
 * LTR hash columns, and `AUDIT_MANAGE` checkpoint/verify actions in the toolbar
 * and permission-filtered row/header menus.
 */
export function AuditLogTable() {
  const table = useTableState({
    defaultSortKey: 'occurredAt',
    defaultSortDirection: 'desc',
    filterKeys: ['moduleName', 'actionTypeId', 'fromUtc', 'toUtc'],
  })

  const auditQuery = useAuditLogs({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    sortBy: toSortKey(table.sortKey),
    sortDescending: table.sortDirection === 'desc',
    moduleName: table.filters.moduleName || undefined,
    actionTypeId: optionalId(table.filters.actionTypeId),
    fromUtc: dayBoundary(table.filters.fromUtc, 'start'),
    toUtc: dayBoundary(table.filters.toUtc, 'end'),
  })

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManage = hasPermission(Permissions.AuditManage)
  const overrides = useLabelsStore((state) => state.overrides)

  const moduleOptions = [
    { value: 'all', label: resolveLabel('audit.filter.module-all', overrides) },
    ...AUDIT_MODULES.map((module) => ({ value: module.code, label: resolveLabel(module.labelKey, overrides) })),
  ]

  const actionTypesQuery = useLookupEntries(LOOKUP_SLUGS.AuditActionTypes)

  const actionOptions = useMemo(
    () => [
      { value: 'all', label: resolveLabel('audit.filter.action-all', overrides) },
      ...(actionTypesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [actionTypesQuery.data, overrides],
  )

  const [checkpointTarget, setCheckpointTarget] = useState<number | null>(null)
  const [integrityStart, setIntegrityStart] = useState<number | null>(null)
  const [integrityOpen, setIntegrityOpen] = useState(false)

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('audit.toast.copied', { label }))
    } catch {
      toast.error(t('audit.toast.copy-failed'))
    }
  }

  const columns = useMemo<DataTableColumn<AuditLogResponse>[]>(
    () => [
      {
        id: 'auditLogId',
        headerKey: 'audit.column.id',
        hideable: false,
        width: '5.5rem',
        cell: (log) => (
          <span dir="ltr" className="font-mono text-xs text-foreground">
            #{formatNumber(log.auditLogId)}
          </span>
        ),
      },
      {
        id: 'occurredAtUtc',
        headerKey: 'audit.column.occurred-at',
        sortable: true,
        sortKey: 'occurredAt',
        hideable: false,
        cell: (log) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {formatDateTime(log.occurredAtUtc)}
          </span>
        ),
      },
      {
        id: 'moduleName',
        headerKey: 'audit.column.module',
        sortable: true,
        sortKey: 'module',
        cell: (log) => <Badge variant="outline">{auditModuleLabel(log.moduleName)}</Badge>,
      },
      {
        id: 'actionTypeId',
        headerKey: 'audit.column.action',
        sortable: true,
        sortKey: 'action',
        cell: (log) => <LookupLabel lookup={LOOKUP_SLUGS.AuditActionTypes} id={log.actionTypeId} />,
      },
      {
        id: 'entityTypeId',
        headerKey: 'audit.column.entity-type',
        sortable: true,
        sortKey: 'entity',
        cell: (log) => (
          <LookupLabel lookup={LOOKUP_SLUGS.AuditEntityTypes} id={log.entityTypeId} />
        ),
      },
      {
        id: 'entityId',
        headerKey: 'audit.column.entity-id',
        cell: (log) => (
          <span dir="ltr" title={log.entityId} className="font-mono text-xs text-muted">
            {compactId(log.entityId)}
          </span>
        ),
      },
      {
        id: 'eventHash',
        headerKey: 'audit.column.event-hash',
        cell: (log) => <HashChainPill hash={log.eventHash} label={t('audit.column.event-hash')} />,
      },
      {
        id: 'previousHash',
        headerKey: 'audit.column.previous-hash',
        defaultHidden: true,
        cell: (log) => <HashChainPill hash={log.previousHash} label={t('audit.column.previous-hash')} />,
      },
      {
        id: 'actorUserId',
        headerKey: 'audit.column.actor',
        defaultHidden: true,
        cell: (log) => <UserLabel userId={log.actorUserId} />,
      },
      {
        id: 'actorDepartmentId',
        headerKey: 'audit.column.actor-department',
        defaultHidden: true,
        cell: (log) => <DepartmentLabel departmentId={log.actorDepartmentId} />,
      },
      {
        id: 'relatedDocumentId',
        headerKey: 'audit.column.related-document',
        defaultHidden: true,
        cell: (log) => (
          <span
            dir="ltr"
            title={log.relatedDocumentId ?? undefined}
            className="font-mono text-xs text-muted"
          >
            {log.relatedDocumentId ? formatShortId(log.relatedDocumentId) : '—'}
          </span>
        ),
      },
      {
        id: 'ipAddress',
        headerKey: 'audit.column.ip',
        defaultHidden: true,
        cell: (log) => (
          <span dir="ltr" className="font-mono text-xs text-muted">
            {log.ipAddress ?? '—'}
          </span>
        ),
      },
      {
        id: 'correlationId',
        headerKey: 'audit.column.correlation-id',
        defaultHidden: true,
        cell: (log) => (
          <span dir="ltr" className="font-mono text-xs text-muted">
            {log.correlationId ?? '—'}
          </span>
        ),
      },
    ],
    [],
  )

  const buildRowMenu = (log: AuditLogResponse): MenuEntry[] => {
    const entries: MenuEntry[] = [
      {
        id: 'copy-event-hash',
        label: t('audit.menu.copy-event-hash'),
        icon: <Hash className="size-4" aria-hidden />,
        onSelect: () => void copyValue(log.eventHash, t('audit.column.event-hash')),
      },
      {
        id: 'copy-audit-id',
        label: t('audit.menu.copy-audit-id'),
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(String(log.auditLogId), t('audit.copy.audit-id')),
      },
      {
        id: 'copy-entity-id',
        label: t('audit.menu.copy-entity-id'),
        icon: <Copy className="size-4" aria-hidden />,
        disabled: !log.entityId,
        onSelect: () => void copyValue(log.entityId, t('audit.column.entity-id')),
      },
    ]

    if (!canManage) return entries

    return [
      ...entries,
      { type: 'separator' },
      {
        id: 'checkpoint-to-row',
        label: t('audit.menu.checkpoint-to-row'),
        icon: <Stamp className="size-4" aria-hidden />,
        onSelect: () => setCheckpointTarget(log.auditLogId),
      },
      {
        id: 'verify-from-row',
        label: t('audit.menu.verify-from-row'),
        icon: <ShieldCheck className="size-4" aria-hidden />,
        onSelect: () => setIntegrityStart(log.auditLogId),
      },
    ]
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: t('table.refresh'),
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void auditQuery.refetch(),
  }

  const manageEntries: MenuEntry[] = canManage
    ? [
        {
          id: 'run-integrity',
          label: t('audit.menu.run-integrity'),
          icon: <ShieldCheck className="size-4" aria-hidden />,
          onSelect: () => setIntegrityOpen(true),
        },
      ]
    : []

  const headerMenu: MenuEntry[] = [...manageEntries, refreshEntry]

  const actionTypeId = optionalId(table.filters.actionTypeId)
  const hasQuery =
    Boolean(table.search) || table.hasActiveFilters

  return (
    <>
      <DataTable
        tableId="audit-logs"
        caption={t('audit.caption')}
        columns={columns}
        rows={auditQuery.data?.items}
        rowKey={(log) => String(log.auditLogId)}
        isLoading={auditQuery.isLoading}
        error={auditQuery.error}
        onRetry={() => void auditQuery.refetch()}
        onRefresh={() => void auditQuery.refetch()}
        isFetching={auditQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={auditQuery.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        searchPlaceholder={t('audit.search-placeholder')}
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <GuideChip id="audit.filters" />
            <Select
              aria-label={t('audit.filter.module-aria')}
              value={table.filters.moduleName || 'all'}
              onValueChange={(value) =>
                table.setFilter('moduleName', value === 'all' ? null : value)
              }
              options={moduleOptions}
              className="w-44"
            />
            <Select
              aria-label={t('audit.filter.action-aria')}
              value={actionTypeId ? String(actionTypeId) : 'all'}
              onValueChange={(value) =>
                table.setFilter('actionTypeId', value === 'all' ? null : value)
              }
              options={actionOptions}
              disabled={actionTypesQuery.isLoading}
              className="w-40"
            />
            <label className="flex items-center gap-1.5 text-xs text-muted">
              <Label k="audit.filter.from" />
              <Input
                type="date"
                aria-label={t('audit.filter.from')}
                dir="ltr"
                value={table.filters.fromUtc ?? ''}
                onChange={(event) => table.setFilter('fromUtc', event.target.value || null)}
                className="w-44"
              />
            </label>
            <label className="flex items-center gap-1.5 text-xs text-muted">
              <Label k="audit.filter.to" />
              <Input
                type="date"
                aria-label={t('audit.filter.to')}
                dir="ltr"
                value={table.filters.toUtc ?? ''}
                onChange={(event) => table.setFilter('toUtc', event.target.value || null)}
                className="w-44"
              />
            </label>
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                <Label k="audit.filter.clear" />
              </Button>
            )}
          </div>
        }
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(log) => t('audit.row-label', { id: log.auditLogId })}
        toolbar={
          canManage ? (
            <div className="flex flex-wrap items-center gap-2">
              <CreateCheckpointButton />
              <GuideChip id="audit.checkpoint" />
              <Button type="button" variant="outline" onClick={() => setIntegrityOpen(true)}>
                <ShieldCheck className="size-4" aria-hidden />
                <Label k="audit.toolbar.check" />
              </Button>
              <GuideChip id="audit.check" />
            </div>
          ) : undefined
        }
        emptyTitle={hasQuery ? t('audit.empty.search-title') : t('audit.empty.title')}
        emptyDescription={
          hasQuery
            ? t('audit.empty.search-description')
            : t('audit.empty.description')
        }
        emptyAction={
          hasQuery ? (
            <Button type="button" variant="outline" onClick={table.clearQuery}>
              <Label k="audit.empty.clear" />
            </Button>
          ) : undefined
        }
      />

      {checkpointTarget !== null && (
        <CreateCheckpointDialog
          open
          toAuditLogId={checkpointTarget}
          onOpenChange={(open) => {
            if (!open) setCheckpointTarget(null)
          }}
        />
      )}

      {(integrityOpen || integrityStart !== null) && (
        <RunIntegrityCheckDialog
          open
          startAuditLogId={integrityStart ?? undefined}
          onOpenChange={(open) => {
            if (open) return
            setIntegrityOpen(false)
            setIntegrityStart(null)
          }}
        />
      )}
    </>
  )
}
