import type { ReactNode } from 'react'
import { useEmployeeById } from '@/entities/employee'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { formatDate, formatDateTime, t } from '@/shared/lib'
import { Badge, Button, Dialog, Label, Skeleton } from '@/shared/ui'

interface DepartmentEmployeeDetailDialogProps {
  employeeId: string | null
  onOpenChange: (open: boolean) => void
}

/** Read-only employee details; never imports another widget or an identity surface. */
export function DepartmentEmployeeDetailDialog({
  employeeId,
  onOpenChange,
}: DepartmentEmployeeDetailDialogProps) {
  const canViewEmployees = useSessionStore((state) =>
    state.hasPermission(Permissions.EmployeeManage),
  )
  const query = useEmployeeById(employeeId, { enabled: canViewEmployees })
  const employee = query.data

  if (!canViewEmployees) return null

  return (
    <Dialog
      open={employeeId !== null}
      onOpenChange={onOpenChange}
      title={employee?.fullNameAr ?? t('department.employee-detail.title')}
      description={t('department.employee-detail.description')}
      footer={<Button type="button" onClick={() => onOpenChange(false)}><Label k="common.action.close" /></Button>}
    >
      {query.isLoading ? (
        <div role="status" aria-label={t('department.employee-detail.loading-aria')} className="flex flex-col gap-3">
          {Array.from({ length: 4 }, (_, index) => <Skeleton key={index} className="h-9 w-full" />)}
        </div>
      ) : query.error ? (
        <div role="alert" className="flex flex-wrap items-center gap-3 rounded-lg border border-danger/40 bg-danger/10 p-3 text-sm text-danger">
          <span>{toArabicErrorMessage(query.error)}</span>
          <Button type="button" variant="outline" size="sm" onClick={() => void query.refetch()}>
            <Label k="common.action.retry" />
          </Button>
        </div>
      ) : employee ? (
        <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <DetailItem label={t('department.employee-detail.name-ar')}>{employee.fullNameAr}</DetailItem>
          <DetailItem label={t('department.employee-detail.name-en')}><span dir="ltr">{employee.fullNameEn ?? '—'}</span></DetailItem>
          <DetailItem label={t('department.employee-detail.number')}><span dir="ltr">{employee.employeeNumber ?? '—'}</span></DetailItem>
          <DetailItem label={t('department.employee-detail.job-title')}>{employee.jobTitleAr ?? '—'}</DetailItem>
          <DetailItem label={t('department.employee-detail.status')}>
            <Badge variant={employee.isActive ? 'success' : 'danger'}>{employee.isActive ? t('department.status.active') : t('department.status.suspended')}</Badge>
          </DetailItem>
          <DetailItem label={t('department.employee-detail.department-id')}><span dir="ltr" className="font-mono text-xs">{employee.departmentId}</span></DetailItem>
          <DetailItem label={t('department.employee-detail.email')}><span dir="ltr">{employee.email ?? '—'}</span></DetailItem>
          <DetailItem label={t('department.employee-detail.phone')}><span dir="ltr">{employee.phone ?? '—'}</span></DetailItem>
          <DetailItem label={t('department.employee-detail.created-at')}><span dir="ltr">{formatDate(employee.createdAt)}</span></DetailItem>
          <DetailItem label={t('department.employee-detail.updated-at')}><span dir="ltr">{formatDateTime(employee.updatedAt)}</span></DetailItem>
          <DetailItem label={t('department.employee-detail.employee-id')}><span dir="ltr" className="font-mono text-xs">{employee.employeeId}</span></DetailItem>
        </dl>
      ) : <p className="text-sm text-muted">{t('department.employee-detail.empty')}</p>}
    </Dialog>
  )
}

function DetailItem({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="min-w-0">
      <dt className="text-xs text-muted">{label}</dt>
      <dd className="mt-0.5 break-words text-sm text-foreground">{children}</dd>
    </div>
  )
}
