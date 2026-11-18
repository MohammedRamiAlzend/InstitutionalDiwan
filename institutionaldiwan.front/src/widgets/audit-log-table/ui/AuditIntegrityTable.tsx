import { useMemo, useState } from 'react'
import { Copy, Hash, RefreshCw, ShieldCheck } from 'lucide-react'
import { toast } from 'sonner'
import {
  IntegrityStatusBadge,
  useAuditIntegrityReports,
  type AuditIntegrityCheckResponse,
} from '@/entities/audit-log'
import { useSessionStore, UserLabel } from '@/entities/user'
import { RunIntegrityCheckDialog } from '@/features/audit-manage'
import { Permissions } from '@/shared/config'
import { formatDateTime, formatNumber, formatShortId, t, useTableState } from '@/shared/lib'
import {
  Button,
  DataTable,
  GuideChip,
  Label,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

/** Persisted integrity reports (`audit-integrity-reports`), newest first. */
export function AuditIntegrityTable() {
  const table = useTableState()
  const reportsQuery = useAuditIntegrityReports({
    page: table.page,
    pageSize: table.pageSize,
  })

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManage = hasPermission(Permissions.AuditManage)

  const [checkOpen, setCheckOpen] = useState(false)
  const [checkRange, setCheckRange] = useState<{ start: number; end: number } | null>(null)

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('audit.toast.copied', { label }))
    } catch {
      toast.error(t('audit.toast.copy-failed'))
    }
  }

  const columns = useMemo<DataTableColumn<AuditIntegrityCheckResponse>[]>(
    () => [
      {
        id: 'checkId',
        headerKey: 'audit.integrity.column.check-id',
        hideable: false,
        cell: (report) => (
          <span dir="ltr" title={report.checkId} className="font-mono text-xs text-foreground">
            {formatShortId(report.checkId)}
          </span>
        ),
      },
      {
        id: 'executedAtUtc',
        headerKey: 'audit.integrity.column.executed-at',
        hideable: false,
        cell: (report) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {formatDateTime(report.executedAtUtc)}
          </span>
        ),
      },
      {
        id: 'range',
        headerKey: 'audit.integrity.column.range',
        cell: (report) => (
          <span dir="ltr" className="whitespace-nowrap font-mono text-xs text-muted">
            #{formatNumber(report.startAuditLogId)} – #{formatNumber(report.endAuditLogId)}
          </span>
        ),
      },
      {
        id: 'isValid',
        headerKey: 'audit.integrity.column.result',
        cell: (report) => <IntegrityStatusBadge isValid={report.isValid} />,
      },
      {
        id: 'firstCorruptedLogId',
        headerKey: 'audit.integrity.column.first-corrupted',
        cell: (report) =>
          report.firstCorruptedLogId === null ? (
            <span className="text-muted">—</span>
          ) : (
            <span dir="ltr" className="font-mono text-xs text-danger">
              #{formatNumber(report.firstCorruptedLogId)}
            </span>
          ),
      },
      {
        id: 'failureDetails',
        headerKey: 'audit.integrity.column.failure-details',
        defaultHidden: true,
        cell: (report) =>
          report.failureDetails ? (
            <span
              dir="ltr"
              title={report.failureDetails}
              className="line-clamp-2 font-mono text-xs text-danger"
            >
              {report.failureDetails}
            </span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'executedByUserId',
        headerKey: 'audit.integrity.column.executed-by',
        defaultHidden: true,
        cell: (report) => <UserLabel userId={report.executedByUserId} />,
      },
    ],
    [],
  )

  const buildRowMenu = (report: AuditIntegrityCheckResponse): MenuEntry[] => {
    const entries: MenuEntry[] = [
      {
        id: 'copy-check-id',
        label: t('audit.integrity.menu.copy-check-id'),
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(report.checkId, t('audit.integrity.column.check-id')),
      },
      {
        id: 'copy-corrupted-id',
        label: t('audit.integrity.menu.copy-corrupted-id'),
        icon: <Hash className="size-4" aria-hidden />,
        disabled: report.firstCorruptedLogId === null,
        onSelect: () =>
          void copyValue(String(report.firstCorruptedLogId), t('audit.integrity.copy.corrupted-id')),
      },
    ]

    if (!canManage) return entries

    return [
      ...entries,
      { type: 'separator' },
      {
        id: 'rerun-range',
        label: t('audit.integrity.menu.rerun-range'),
        icon: <ShieldCheck className="size-4" aria-hidden />,
        onSelect: () => setCheckRange({ start: report.startAuditLogId, end: report.endAuditLogId }),
      },
    ]
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: t('table.refresh'),
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void reportsQuery.refetch(),
  }

  const runEntry: MenuEntry = {
    id: 'run-integrity',
    label: t('audit.menu.run-integrity'),
    icon: <ShieldCheck className="size-4" aria-hidden />,
    onSelect: () => setCheckOpen(true),
  }

  const headerMenu: MenuEntry[] = canManage ? [runEntry, refreshEntry] : [refreshEntry]

  return (
    <>
      <DataTable
        tableId="audit-integrity-reports"
        caption={t('audit.integrity.caption')}
        columns={columns}
        rows={reportsQuery.data?.items}
        rowKey={(report) => report.checkId}
        isLoading={reportsQuery.isLoading}
        error={reportsQuery.error}
        onRetry={() => void reportsQuery.refetch()}
        onRefresh={() => void reportsQuery.refetch()}
        isFetching={reportsQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={reportsQuery.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(report) => t('audit.integrity.row-label', { id: formatShortId(report.checkId) })}
        toolbar={
          canManage ? (
            <>
              <Button type="button" variant="outline" onClick={() => setCheckOpen(true)}>
                <ShieldCheck className="size-4" aria-hidden />
                <Label k="audit.toolbar.check" />
              </Button>
              <GuideChip id="audit.check" />
            </>
          ) : undefined
        }
        emptyTitle={t('audit.integrity.empty.title')}
        emptyDescription={t('audit.integrity.empty.description')}
      />

      {(checkOpen || checkRange !== null) && (
        <RunIntegrityCheckDialog
          open
          startAuditLogId={checkRange?.start}
          endAuditLogId={checkRange?.end}
          onOpenChange={(open) => {
            if (open) return
            setCheckOpen(false)
            setCheckRange(null)
          }}
        />
      )}
    </>
  )
}
