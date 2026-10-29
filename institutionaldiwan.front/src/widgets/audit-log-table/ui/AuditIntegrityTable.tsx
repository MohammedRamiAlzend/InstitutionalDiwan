import { useMemo, useState } from 'react'
import { Copy, Hash, RefreshCw, ShieldCheck } from 'lucide-react'
import { toast } from 'sonner'
import {
  IntegrityStatusBadge,
  useAuditIntegrityReports,
  type AuditIntegrityCheckResponse,
} from '@/entities/audit-log'
import { useSessionStore } from '@/entities/user'
import { RunIntegrityCheckDialog } from '@/features/audit-manage'
import { Permissions } from '@/shared/config'
import { formatDateTime, formatNumber, formatShortId, useTableState } from '@/shared/lib'
import {
  Button,
  DataTable,
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
      toast.success(`تم نسخ ${label}.`)
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const columns = useMemo<DataTableColumn<AuditIntegrityCheckResponse>[]>(
    () => [
      {
        id: 'checkId',
        header: 'معرّف الفحص',
        hideable: false,
        cell: (report) => (
          <span dir="ltr" title={report.checkId} className="font-mono text-xs text-foreground">
            {formatShortId(report.checkId)}
          </span>
        ),
      },
      {
        id: 'executedAtUtc',
        header: 'وقت التنفيذ',
        hideable: false,
        cell: (report) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {formatDateTime(report.executedAtUtc)}
          </span>
        ),
      },
      {
        id: 'range',
        header: 'النطاق',
        cell: (report) => (
          <span dir="ltr" className="whitespace-nowrap font-mono text-xs text-muted">
            #{formatNumber(report.startAuditLogId)} – #{formatNumber(report.endAuditLogId)}
          </span>
        ),
      },
      {
        id: 'isValid',
        header: 'النتيجة',
        cell: (report) => <IntegrityStatusBadge isValid={report.isValid} />,
      },
      {
        id: 'firstCorruptedLogId',
        header: 'أول سجل متأثر',
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
        header: 'تفاصيل الفشل',
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
        header: 'المنفّذ',
        defaultHidden: true,
        cell: (report) => (
          <span dir="ltr" title={report.executedByUserId} className="font-mono text-xs text-muted">
            {formatShortId(report.executedByUserId)}
          </span>
        ),
      },
    ],
    [],
  )

  const buildRowMenu = (report: AuditIntegrityCheckResponse): MenuEntry[] => {
    const entries: MenuEntry[] = [
      {
        id: 'copy-check-id',
        label: 'نسخ معرّف الفحص',
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(report.checkId, 'معرّف الفحص'),
      },
      {
        id: 'copy-corrupted-id',
        label: 'نسخ معرّف أول سجل متأثر',
        icon: <Hash className="size-4" aria-hidden />,
        disabled: report.firstCorruptedLogId === null,
        onSelect: () =>
          void copyValue(String(report.firstCorruptedLogId), 'معرّف أول سجل متأثر'),
      },
    ]

    if (!canManage) return entries

    return [
      ...entries,
      { type: 'separator' },
      {
        id: 'rerun-range',
        label: 'إعادة فحص هذا النطاق',
        icon: <ShieldCheck className="size-4" aria-hidden />,
        onSelect: () => setCheckRange({ start: report.startAuditLogId, end: report.endAuditLogId }),
      },
    ]
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: 'تحديث البيانات',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void reportsQuery.refetch(),
  }

  const runEntry: MenuEntry = {
    id: 'run-integrity',
    label: 'فحص سلامة السلسلة',
    icon: <ShieldCheck className="size-4" aria-hidden />,
    onSelect: () => setCheckOpen(true),
  }

  const headerMenu: MenuEntry[] = canManage ? [runEntry, refreshEntry] : [refreshEntry]

  return (
    <>
      <DataTable
        tableId="audit-integrity-reports"
        caption="تقارير سلامة سلسلة التدقيق"
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
        rowLabel={(report) => `فحص ${formatShortId(report.checkId)}`}
        toolbar={
          canManage ? (
            <Button type="button" variant="outline" onClick={() => setCheckOpen(true)}>
              <ShieldCheck className="size-4" aria-hidden />
              فحص السلامة
            </Button>
          ) : undefined
        }
        emptyTitle="لا توجد تقارير سلامة بعد."
        emptyDescription="شغّل فحصًا للتحقق من تكامل السلسلة؛ سيُحفظ التقرير هنا."
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
