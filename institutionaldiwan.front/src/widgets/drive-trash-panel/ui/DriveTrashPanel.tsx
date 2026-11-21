import { useState } from 'react'
import { RotateCcw, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { useDriveTrash, type DriveItemResponse } from '@/entities/drive-item'
import { usePurgeDriveTrashMutation, useRestoreDriveTrashMutation } from '@/features/drive-deletion'
import { toArabicErrorMessage } from '@/shared/api'
import { formatDateTime, formatFileSize, t, useTableState } from '@/shared/lib'
import { Button, ConfirmDialog, DataTable, Label, type DataTableColumn } from '@/shared/ui'

export function DriveTrashPanel() {
  const table = useTableState({ filterKeys: ['space'] })
  const query = useDriveTrash({ page: table.page, pageSize: table.pageSize, spaceId: table.filters.space })
  const restore = useRestoreDriveTrashMutation()
  const purge = usePurgeDriveTrashMutation()
  const [purgeTarget, setPurgeTarget] = useState<DriveItemResponse | null>(null)

  const restoreItem = async (itemId: string) => {
    try {
      await restore.mutateAsync(itemId)
      toast.success(t('drive.toast.restored'))
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const purgeItem = async () => {
    if (!purgeTarget) return
    try {
      await purge.mutateAsync(purgeTarget.itemId)
      toast.success(t('drive.toast.purged'))
      setPurgeTarget(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns: DataTableColumn<DriveItemResponse>[] = [
    { id: 'name', headerKey: 'drive.column.name', hideable: false, cell: (row) => <span className="font-medium">{row.name}</span> },
    { id: 'size', headerKey: 'drive.column.size', cell: (row) => <span dir="ltr">{formatFileSize(row.sizeBytes)}</span> },
    { id: 'trashed', headerKey: 'drive.column.trashed-at', cell: (row) => <span dir="ltr">{formatDateTime(row.trashedAt)}</span> },
    {
      id: 'actions',
      headerKey: 'common.action.save',
      cell: (row) => <div className="flex gap-2"><Button size="sm" variant="outline" onClick={() => void restoreItem(row.itemId)} loading={restore.isPending}><RotateCcw className="size-4" aria-hidden /><Label k="drive.action.restore" /></Button><Button size="sm" variant="destructive" onClick={() => setPurgeTarget(row)}><Trash2 className="size-4" aria-hidden /><Label k="drive.action.permanent-delete" /></Button></div>,
    },
  ]

  return (
    <>
      <div className="flex flex-col gap-3">
        <p className="rounded-lg border border-warning/40 bg-warning/10 px-3 py-2 text-sm text-muted">{t('drive.trash.retention', { days: 30 })}</p>
        <DataTable
          tableId="drive-trash"
          mode="server"
          caption={t('drive.trash')}
          columns={columns}
          rows={query.data?.items ?? []}
          rowKey={(row) => row.itemId}
          isLoading={query.isLoading}
          error={query.error}
          onRetry={() => void query.refetch()}
          page={table.page}
          pageSize={table.pageSize}
          totalCount={query.data?.totalCount ?? 0}
          onPageChange={table.setPage}
          onPageSizeChange={table.setPageSize}
          emptyTitle={t('drive.trash.empty-title')}
          emptyDescription={t('drive.trash.empty-description')}
        />
      </div>
      <ConfirmDialog
        open={Boolean(purgeTarget)}
        onOpenChange={(open) => { if (!open) setPurgeTarget(null) }}
        title={t('drive.confirm.permanent-delete-title')}
        description={t('drive.confirm.permanent-delete-description')}
        confirmLabel={t('drive.action.permanent-delete')}
        onConfirm={() => void purgeItem()}
        loading={purge.isPending}
        destructive
      />
    </>
  )
}
