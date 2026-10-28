import type { ReactNode } from 'react'
import { Copy, Hash, Pencil } from 'lucide-react'
import { useDepartmentById } from '@/entities/department'
import { useEmployeeById } from '@/entities/employee'
import { LookupLabel, LOOKUP_SLUGS } from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { formatDate, formatDateTime, formatShortId } from '@/shared/lib'
import { Badge, Button, Dialog, Skeleton } from '@/shared/ui'
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
      title={department?.nameAr ?? 'تفاصيل القسم'}
      description="بيانات القسم التنظيمي والموظفون التابعون له."
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
                تعديل القسم
              </Button>
            )}
            <Button
              type="button"
              variant="outline"
              onClick={() => void copyDepartmentValue(department.departmentId, 'المعرّف')}
            >
              <Hash className="size-4" aria-hidden />
              نسخ المعرّف
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => void copyDepartmentValue(department.code, 'رمز القسم')}
            >
              <Copy className="size-4" aria-hidden />
              نسخ الرمز
            </Button>
            <Button type="button" onClick={() => onOpenChange(false)}>
              إغلاق
            </Button>
          </>
        ) : (
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            إغلاق
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
            إعادة المحاولة
          </Button>
        </div>
      ) : department ? (
        <>
          <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <DetailItem label="رمز القسم">
              <span dir="ltr" className="font-mono text-xs">
                {department.code}
              </span>
            </DetailItem>
            <DetailItem label="نوع القسم">
              <LookupLabel lookup={LOOKUP_SLUGS.DepartmentTypes} id={department.departmentTypeId} />
            </DetailItem>
            <DetailItem label="الاسم بالعربية">{department.nameAr}</DetailItem>
            <DetailItem label="الاسم بالإنجليزية">
              {department.nameEn ? (
                <span dir="ltr" className="block truncate">
                  {department.nameEn}
                </span>
              ) : (
                <span className="text-muted">—</span>
              )}
            </DetailItem>
            <DetailItem label="القسم الأعلى">
              {department.parentDepartmentId ? (
                parentName ? (
                  parentName
                ) : (
                  <span
                    dir="ltr"
                    title={department.parentDepartmentId}
                    className="font-mono text-xs text-muted"
                  >
                    {formatShortId(department.parentDepartmentId)}
                  </span>
                )
              ) : (
                <span className="text-muted">لا يتبع قسمًا أعلى</span>
              )}
            </DetailItem>
            <DetailItem label="مدير القسم">
              {department.managerEmployeeId ? (
                managerName ? (
                  <span className="flex flex-col">
                    <span>{managerName}</span>
                    {managerJobTitle && (
                      <span className="text-xs text-muted">{managerJobTitle}</span>
                    )}
                  </span>
                ) : (
                  <span
                    dir="ltr"
                    title={department.managerEmployeeId}
                    className="font-mono text-xs"
                  >
                    {formatShortId(department.managerEmployeeId)}
                  </span>
                )
              ) : (
                <span className="text-muted">غير محدد</span>
              )}
            </DetailItem>
            <DetailItem label="الحالة">
              <Badge variant={department.isActive ? 'success' : 'danger'}>
                {department.isActive ? 'نشط' : 'موقوف'}
              </Badge>
            </DetailItem>
            <DetailItem label="المراسلات الخارجية">
              <span className="flex flex-wrap gap-1.5">
                <Badge variant={department.canSendExternal ? 'success' : 'outline'}>
                  {department.canSendExternal ? 'إرسال مسموح' : 'إرسال محظور'}
                </Badge>
                <Badge variant={department.canReceiveExternal ? 'success' : 'outline'}>
                  {department.canReceiveExternal ? 'استقبال مسموح' : 'استقبال محظور'}
                </Badge>
              </span>
            </DetailItem>
            <DetailItem label="تاريخ الإنشاء">
              <span dir="ltr">{formatDate(department.createdAt)}</span>
            </DetailItem>
            <DetailItem label="آخر تحديث">
              <span dir="ltr">{formatDateTime(department.updatedAt)}</span>
            </DetailItem>
            <DetailItem label="ملاحظات" className="sm:col-span-2">
              {department.notes ? (
                <span className="whitespace-pre-wrap">{department.notes}</span>
              ) : (
                <span className="text-muted">لا توجد ملاحظات.</span>
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
