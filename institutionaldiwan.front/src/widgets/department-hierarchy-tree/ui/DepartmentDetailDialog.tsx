import type { ReactNode } from 'react'
import { Copy, Hash, Pencil } from 'lucide-react'
import { useDepartmentById, DepartmentLabel } from '@/entities/department'
import { useEmployeeById, EmployeeLabel } from '@/entities/employee'
import { LookupLabel, LOOKUP_SLUGS } from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { FEATURES, Permissions } from '@/shared/config'
import { formatDate, formatDateTime, t } from '@/shared/lib'
import { Badge, Button, Dialog, Label, Skeleton } from '@/shared/ui'
import { copyDepartmentValue } from '../model/clipboard'
import { DepartmentEmployeesSection } from './DepartmentEmployeesSection'

interface DepartmentDetailDialogProps {
  /** `null` keeps the dialog closed. */
  departmentId: string | null
  onOpenChange: (open: boolean) => void
  resolveName: (departmentId: string) => string | undefined
  onEdit?: (departmentId: string) => void
}

/** Department card sourced from `GET /api/departments/{id}`. */
export function DepartmentDetailDialog({
  departmentId,
  onOpenChange,
  resolveName,
  onEdit,
}: DepartmentDetailDialogProps) {
  const query = useDepartmentById(departmentId)
  const department = query.data
  const parentName = department?.parentDepartmentId
    ? resolveName(department.parentDepartmentId)
    : undefined

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canViewEmployees = hasPermission(Permissions.EmployeeManage)
  const managerQuery = useEmployeeById(department?.managerEmployeeId ?? null, {
    enabled: canViewEmployees && Boolean(department?.managerEmployeeId),
  })
  const managerName = managerQuery.data?.fullNameAr
  const managerJobTitle = managerQuery.data?.jobTitleAr

  return (
    <Dialog
      open={departmentId !== null}
      onOpenChange={onOpenChange}
      title={department?.nameAr ?? t('department.detail.title')}
      description={t('department.detail.description')}
      size="lg"
      footer={
        department ? (
          <>
            {onEdit && (
              <Button
                type="button"
                onClick={() => onEdit(department.departmentId)}
              >
                <Pencil className="size-4" aria-hidden />
                <Label k="department.menu.edit" />
              </Button>
            )}
            <Button
              type="button"
              variant="outline"
              onClick={() => void copyDepartmentValue(department.departmentId, t('department.copy.id'))}
            >
              <Hash className="size-4" aria-hidden />
              <Label k="department.menu.copy-id" />
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => void copyDepartmentValue(department.code, t('department.copy.code'))}
            >
              <Copy className="size-4" aria-hidden />
              <Label k="department.detail.copy-code" />
            </Button>
            <Button type="button" onClick={() => onOpenChange(false)}>
              <Label k="common.action.close" />
            </Button>
          </>
        ) : (
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            <Label k="common.action.close" />
          </Button>
        )
      }
    >
      {query.isLoading ? (
        <div className="flex flex-col gap-3">
          {Array.from({ length: 5 }, (_, index) => (
            <Skeleton key={index} className="h-9 w-full" />
          ))}
        </div>
      ) : query.error ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
        >
          <span>{toArabicErrorMessage(query.error)}</span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => void query.refetch()}
          >
            <Label k="common.action.retry" />
          </Button>
        </div>
      ) : department ? (
        <>
          <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <DetailItem label={t('department.field.code')}>
              <span dir="ltr" className="font-mono text-xs">
                {department.code}
              </span>
            </DetailItem>
            <DetailItem label={t('department.field.type')}>
              <LookupLabel lookup={LOOKUP_SLUGS.DepartmentTypes} id={department.departmentTypeId} />
            </DetailItem>
            <DetailItem label={t('department.field.name-ar')}>{department.nameAr}</DetailItem>
            <DetailItem label={t('department.field.name-en')}>
              {department.nameEn ? (
                <span dir="ltr" className="block truncate">
                  {department.nameEn}
                </span>
              ) : (
                <span className="text-muted">—</span>
              )}
            </DetailItem>
            <DetailItem label={t('department.field.parent')}>
              {department.parentDepartmentId ? (
                parentName ? (
                  parentName
                ) : (
                  <DepartmentLabel departmentId={department.parentDepartmentId} />
                )
              ) : (
                <span className="text-muted">{t('department.detail.no-parent')}</span>
              )}
            </DetailItem>
            <DetailItem label={t('department.field.manager')}>
              {department.managerEmployeeId ? (
                managerName ? (
                  <span className="flex flex-col">
                    <span>{managerName}</span>
                    {managerJobTitle && (
                      <span className="text-xs text-muted">{managerJobTitle}</span>
                    )}
                  </span>
                ) : (
                  <EmployeeLabel employeeId={department.managerEmployeeId} />
                )
              ) : (
                <span className="text-muted">{t('department.detail.no-manager')}</span>
              )}
            </DetailItem>
            <DetailItem label={t('department.field.status')}>
              <Badge variant={department.isActive ? 'success' : 'danger'}>
                {department.isActive ? t('department.status.active') : t('department.status.suspended')}
              </Badge>
            </DetailItem>
            {FEATURES.externalOrganizations && (
            <DetailItem label={t('department.field.external')}>
              <span className="flex flex-wrap gap-1.5">
                <Badge variant={department.canSendExternal ? 'success' : 'outline'}>
                  {department.canSendExternal ? t('department.detail.send-allowed') : t('department.detail.send-blocked')}
                </Badge>
                <Badge variant={department.canReceiveExternal ? 'success' : 'outline'}>
                  {department.canReceiveExternal ? t('department.detail.receive-allowed') : t('department.detail.receive-blocked')}
                </Badge>
              </span>
            </DetailItem>
            )}
            <DetailItem label={t('department.field.created-at')}>
              <span dir="ltr">{formatDate(department.createdAt)}</span>
            </DetailItem>
            <DetailItem label={t('department.field.updated-at')}>
              <span dir="ltr">{formatDateTime(department.updatedAt)}</span>
            </DetailItem>
            <DetailItem label={t('department.field.notes')} className="sm:col-span-2">
              {department.notes ? (
                <span className="whitespace-pre-wrap">{department.notes}</span>
              ) : (
                <span className="text-muted">{t('department.detail.no-notes')}</span>
              )}
            </DetailItem>
          </dl>

          <DepartmentEmployeesSection departmentId={department.departmentId} />
        </>
      ) : null}
    </Dialog>
  )
}

function DetailItem({
  label,
  children,
  className,
}: {
  label: string
  children: ReactNode
  className?: string
}) {
  return (
    <div className={className}>
      <dt className="text-xs text-muted">{label}</dt>
      <dd className="mt-0.5 text-sm text-foreground">{children}</dd>
    </div>
  )
}
