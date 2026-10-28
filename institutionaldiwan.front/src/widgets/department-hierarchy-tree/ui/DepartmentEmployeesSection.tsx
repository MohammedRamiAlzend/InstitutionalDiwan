import { useState } from 'react'
import { Users } from 'lucide-react'
import { useEmployees } from '@/entities/employee'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { formatNumber } from '@/shared/lib'
import { Badge, Button, Checkbox, Skeleton } from '@/shared/ui'

/** The dialog shows the first page only; departments hold far fewer people than this. */
const PAGE_SIZE = 50

interface DepartmentEmployeesSectionProps {
  departmentId: string
}

/**
 * Read-only roster for one department, sourced from
 * `GET /api/employees?departmentId=…&includeSubDepartments=…`. Hidden when the caller lacks
 * `EMPLOYEE_MANAGE`, because the endpoint is gated on it.
 */
export function DepartmentEmployeesSection({ departmentId }: DepartmentEmployeesSectionProps) {
  const canViewEmployees = useSessionStore((state) =>
    state.hasPermission(Permissions.EmployeeManage),
  )
  const [includeSubDepartments, setIncludeSubDepartments] = useState(true)

  const query = useEmployees(
    { page: 1, pageSize: PAGE_SIZE, departmentId, includeSubDepartments },
    { enabled: canViewEmployees },
  )

  if (!canViewEmployees) return null

  const employees = query.data?.items ?? []
  const totalCount = query.data?.totalCount ?? 0

  return (
    <section className="flex flex-col rounded-xl border border-border">
      <header className="flex flex-wrap items-center justify-between gap-3 border-b border-border px-3 py-2">
        <div className="flex items-center gap-2">
          <Users className="size-4 text-muted" aria-hidden />
          <h3 className="text-sm font-medium text-foreground">الموظفون</h3>
          {query.data && (
            <Badge variant="outline">{formatNumber(totalCount)}</Badge>
          )}
        </div>
        <Checkbox
          checked={includeSubDepartments}
          onCheckedChange={setIncludeSubDepartments}
          label="تضمين الأقسام الفرعية"
          disabled={query.isLoading}
        />
      </header>

      {query.isLoading ? (
        <div className="flex flex-col gap-2 p-3">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-10 w-full" />
          ))}
        </div>
      ) : query.error ? (
        <div
          role="alert"
          className="m-3 flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
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
      ) : employees.length === 0 ? (
        <div className="flex flex-col gap-1 px-3 py-6 text-center">
          <p className="text-sm text-foreground">لا يوجد موظفون مسجّلون هنا.</p>
          <p className="text-xs text-muted">
            {includeSubDepartments
              ? 'لم يُسجَّل أي موظف في هذا القسم أو في الأقسام التابعة له.'
              : 'فعّل «تضمين الأقسام الفرعية» لعرض موظفي الأقسام التابعة.'}
          </p>
        </div>
      ) : (
        <>
          <ul className="divide-y divide-border">
            {employees.map((employee) => (
              <li
                key={employee.employeeId}
                className="flex flex-wrap items-center justify-between gap-2 px-3 py-2"
              >
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-foreground">
                    {employee.fullNameAr}
                  </p>
                  <p className="truncate text-xs text-muted">
                    {employee.jobTitleAr ?? 'بدون مسمى وظيفي'}
                  </p>
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  {employee.employeeNumber && (
                    <span dir="ltr" className="font-mono text-xs text-muted">
                      {employee.employeeNumber}
                    </span>
                  )}
                  <Badge variant={employee.isActive ? 'success' : 'danger'}>
                    {employee.isActive ? 'نشط' : 'موقوف'}
                  </Badge>
                </div>
              </li>
            ))}
          </ul>
          {totalCount > employees.length && (
            <p className="border-t border-border px-3 py-2 text-xs text-muted">
              يُعرض أول <span dir="ltr">{formatNumber(employees.length)}</span> من أصل{' '}
              <span dir="ltr">{formatNumber(totalCount)}</span> موظفاً.
            </p>
          )}
        </>
      )}
    </section>
  )
}
