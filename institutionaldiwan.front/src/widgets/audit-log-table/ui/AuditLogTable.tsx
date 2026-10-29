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
import { useSessionStore } from '@/entities/user'
import {
  CreateCheckpointButton,
  CreateCheckpointDialog,
  RunIntegrityCheckDialog,
} from '@/features/audit-manage'
import { Permissions } from '@/shared/config'
import { formatDateTime, formatNumber, formatShortId, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  Input,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

const MODULE_OPTIONS = [
  { value: 'all', label: 'كل الوحدات' },
  ...AUDIT_MODULES.map((module) => ({ value: module.code, label: module.label })),
]

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

  const actionTypesQuery = useLookupEntries(LOOKUP_SLUGS.AuditActionTypes)

  const actionOptions = useMemo(
    () => [
      { value: 'all', label: 'كل الإجراءات' },
      ...(actionTypesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [actionTypesQuery.data],
  )

  const [checkpointTarget, setCheckpointTarget] = useState<number | null>(null)
  const [integrityStart, setIntegrityStart] = useState<number | null>(null)
  const [integrityOpen, setIntegrityOpen] = useState(false)

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(`تم نسخ ${label}.`)
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const columns = useMemo<DataTableColumn<AuditLogResponse>[]>(
    () => [
      {
        id: 'auditLogId',
        header: 'المعرّف',
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
        header: 'وقت الحدوث',
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
        header: 'الوحدة',
        sortable: true,
        sortKey: 'module',
        cell: (log) => <Badge variant="outline">{auditModuleLabel(log.moduleName)}</Badge>,
      },
      {
        id: 'actionTypeId',
        header: 'الإجراء',
        sortable: true,
        sortKey: 'action',
        cell: (log) => <LookupLabel lookup={LOOKUP_SLUGS.AuditActionTypes} id={log.actionTypeId} />,
      },
      {
        id: 'entityTypeId',
        header: 'نوع الكيان',
        sortable: true,
        sortKey: 'entity',
        cell: (log) => (
          <LookupLabel lookup={LOOKUP_SLUGS.AuditEntityTypes} id={log.entityTypeId} />
        ),
      },
      {
        id: 'entityId',
        header: 'معرّف الكيان',
        cell: (log) => (
          <span dir="ltr" title={log.entityId} className="font-mono text-xs text-muted">
            {compactId(log.entityId)}
          </span>
        ),
      },
      {
        id: 'eventHash',
        header: 'تجزئة الحدث',
        cell: (log) => <HashChainPill hash={log.eventHash} label="تجزئة الحدث" />,
      },
      {
        id: 'previousHash',
        header: 'تجزئة السجل السابق',
        defaultHidden: true,
        cell: (log) => <HashChainPill hash={log.previousHash} label="تجزئة السجل السابق" />,
      },
      {
        id: 'actorUserId',
        header: 'المستخدم المنفّذ',
        defaultHidden: true,
        cell: (log) => (
          <span
            dir="ltr"
            title={log.actorUserId ?? undefined}
            className="font-mono text-xs text-muted"
          >
            {log.actorUserId ? formatShortId(log.actorUserId) : '—'}
          </span>
        ),
      },
      {
        id: 'actorDepartmentId',
        header: 'قسم المنفّذ',
        defaultHidden: true,
        cell: (log) => (
          <span
            dir="ltr"
            title={log.actorDepartmentId ?? undefined}
            className="font-mono text-xs text-muted"
          >
            {log.actorDepartmentId ? formatShortId(log.actorDepartmentId) : '—'}
          </span>
        ),
      },
      {
        id: 'relatedDocumentId',
        header: 'الوثيقة المرتبطة',
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
        header: 'عنوان IP',
        defaultHidden: true,
        cell: (log) => (
          <span dir="ltr" className="font-mono text-xs text-muted">
            {log.ipAddress ?? '—'}
          </span>
        ),
      },
      {
        id: 'correlationId',
        header: 'معرّف الارتباط',
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
        label: 'نسخ تجزئة الحدث',
        icon: <Hash className="size-4" aria-hidden />,
        onSelect: () => void copyValue(log.eventHash, 'تجزئة الحدث'),
      },
      {
        id: 'copy-audit-id',
        label: 'نسخ معرّف السجل',
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(String(log.auditLogId), 'معرّف السجل'),
      },
      {
        id: 'copy-entity-id',
        label: 'نسخ معرّف الكيان',
        icon: <Copy className="size-4" aria-hidden />,
        disabled: !log.entityId,
        onSelect: () => void copyValue(log.entityId, 'معرّف الكيان'),
      },
    ]

    if (!canManage) return entries

    return [
      ...entries,
      { type: 'separator' },
      {
        id: 'checkpoint-to-row',
        label: 'نقطة تحقق حتى هذا السجل',
        icon: <Stamp className="size-4" aria-hidden />,
        onSelect: () => setCheckpointTarget(log.auditLogId),
      },
      {
        id: 'verify-from-row',
        label: 'فحص السلامة من هذا السجل',
        icon: <ShieldCheck className="size-4" aria-hidden />,
        onSelect: () => setIntegrityStart(log.auditLogId),
      },
    ]
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: 'تحديث البيانات',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void auditQuery.refetch(),
  }

  const manageEntries: MenuEntry[] = canManage
    ? [
        {
          id: 'run-integrity',
          label: 'فحص سلامة السلسلة',
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
        caption="سجل التدقيق"
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
        searchPlaceholder="ابحث بالوحدة أو معرّف الكيان أو الارتباط أو IP…"
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <Select
              aria-label="تصفية حسب الوحدة"
              value={table.filters.moduleName || 'all'}
              onValueChange={(value) =>
                table.setFilter('moduleName', value === 'all' ? null : value)
              }
              options={MODULE_OPTIONS}
              className="w-44"
            />
            <Select
              aria-label="تصفية حسب الإجراء"
              value={actionTypeId ? String(actionTypeId) : 'all'}
              onValueChange={(value) =>
                table.setFilter('actionTypeId', value === 'all' ? null : value)
              }
              options={actionOptions}
              disabled={actionTypesQuery.isLoading}
              className="w-40"
            />
            <label className="flex items-center gap-1.5 text-xs text-muted">
              من تاريخ
              <Input
                type="date"
                aria-label="من تاريخ"
                dir="ltr"
                value={table.filters.fromUtc ?? ''}
                onChange={(event) => table.setFilter('fromUtc', event.target.value || null)}
                className="w-44"
              />
            </label>
            <label className="flex items-center gap-1.5 text-xs text-muted">
              إلى تاريخ
              <Input
                type="date"
                aria-label="إلى تاريخ"
                dir="ltr"
                value={table.filters.toUtc ?? ''}
                onChange={(event) => table.setFilter('toUtc', event.target.value || null)}
                className="w-44"
              />
            </label>
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                مسح المرشحات
              </Button>
            )}
          </div>
        }
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(log) => `السجل #${log.auditLogId}`}
        toolbar={
          canManage ? (
            <div className="flex flex-wrap items-center gap-2">
              <CreateCheckpointButton />
              <Button type="button" variant="outline" onClick={() => setIntegrityOpen(true)}>
                <ShieldCheck className="size-4" aria-hidden />
                فحص السلامة
              </Button>
            </div>
          ) : undefined
        }
        emptyTitle={hasQuery ? 'لا توجد سجلات مطابقة.' : 'لا توجد سجلات تدقيق بعد.'}
        emptyDescription={
          hasQuery
            ? 'جرّب تعديل البحث أو المرشحات أو نطاق التاريخ.'
            : 'ستظهر السجلات هنا بمجرد تنفيذ أول إجراء في النظام.'
        }
        emptyAction={
          hasQuery ? (
            <Button type="button" variant="outline" onClick={table.clearQuery}>
              مسح البحث والمرشحات
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
