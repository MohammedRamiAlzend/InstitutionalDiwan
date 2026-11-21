import { useState } from 'react'
import { Check, X } from 'lucide-react'
import { toast } from 'sonner'
import {
  useDriveDeletionRequests,
  type DriveDeletionRequestResponse,
} from '@/entities/drive-item'
import { useCancelDriveDeletionMutation } from '@/features/drive-deletion'
import { ReviewDeletionDialog } from '@/features/drive-deletion'
import { toArabicErrorMessage } from '@/shared/api'
import { DRIVE_DELETION_STATUS } from '@/shared/config'
import { formatDateTime, t, useTableState } from '@/shared/lib'
import { Button, DataTable, Label, type DataTableColumn } from '@/shared/ui'

export function DriveDeletionInbox() {
  const table = useTableState({ filterKeys: ['scope'] })
  const scope = table.filters.scope ?? 'mine'
  const query = useDriveDeletionRequests({
    page: table.page,
    pageSize: table.pageSize,
    mineOnly: scope === 'mine',
  })
  const cancel = useCancelDriveDeletionMutation()
  const [review, setReview] = useState<{ request: DriveDeletionRequestResponse; action: 'approve' | 'reject' } | null>(null)

  const cancelRequest = async (requestId: string) => {
    try {
      await cancel.mutateAsync(requestId)
      toast.success(t('drive.toast.deletion-cancelled'))
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns: DataTableColumn<DriveDeletionRequestResponse>[] = [
    { id: 'reason', headerKey: 'drive.column.reason', cell: (row) => <span className="text-sm">{row.reason}</span> },
    { id: 'status', headerKey: 'drive.column.status', cell: (row) => <Label k={row.statusId === DRIVE_DELETION_STATUS.Approved ? 'drive.deletion-status.approved' : row.statusId === DRIVE_DELETION_STATUS.Rejected ? 'drive.deletion-status.rejected' : row.statusId === DRIVE_DELETION_STATUS.Cancelled ? 'drive.deletion-status.cancelled' : 'drive.deletion-status.pending'} /> },
    { id: 'created', headerKey: 'drive.column.requested-at', cell: (row) => <span dir="ltr">{formatDateTime(row.createdAt)}</span> },
    {
      id: 'actions',
      headerKey: 'common.action.save',
      cell: (row) => row.statusId !== DRIVE_DELETION_STATUS.Pending ? null : (
        <div className="flex flex-wrap gap-2">
          {scope !== 'mine' && <><Button size="sm" onClick={() => setReview({ request: row, action: 'approve' })}><Check className="size-4" aria-hidden /><Label k="common.action.confirm" /></Button><Button size="sm" variant="outline" onClick={() => setReview({ request: row, action: 'reject' })}><X className="size-4" aria-hidden /><Label k="drive.toast.deletion-rejected" /></Button></>}
          {scope === 'mine' && <Button size="sm" variant="outline" onClick={() => void cancelRequest(row.requestId)} loading={cancel.isPending}><Label k="drive.toast.deletion-cancelled" /></Button>}
        </div>
      ),
    },
  ]

  return (
    <>
      <DataTable
        tableId="drive-deletion-requests"
        mode="server"
        caption={t('drive.view.deletion-requests')}
        columns={columns}
        rows={query.data?.items ?? []}
        rowKey={(row) => row.requestId}
        isLoading={query.isLoading}
        error={query.error}
        onRetry={() => void query.refetch()}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={query.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        toolbar={<div className="flex gap-2"><Button variant={scope === 'mine' ? 'primary' : 'outline'} onClick={() => table.setFilter('scope', 'mine')}><Label k="drive.deletion.mine-only" /></Button><Button variant={scope === 'review' ? 'primary' : 'outline'} onClick={() => table.setFilter('scope', 'review')}><Label k="drive.deletion.reviewable" /></Button></div>}
        emptyTitle={t('drive.deletion.empty-title')}
        emptyDescription={t('drive.deletion.empty-description')}
      />
      {review && <ReviewDeletionDialog request={review.request} action={review.action} open onOpenChange={(open) => { if (!open) setReview(null) }} />}
    </>
  )
}
