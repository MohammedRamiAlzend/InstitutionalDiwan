import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { PageHeader } from '@/shared/ui'
import { EmployeesTable } from '@/widgets/employees-table'

/** `/employees` — Module 02 employee record administration. */
export function EmployeesPage() {
  return (
    <RequirePermission permission={Permissions.EmployeeManage}>
      <div className="flex flex-col gap-6">
        <PageHeader
          title="إدارة الموظفين"
          description="أضف سجلات الموظفين واربطها بالأقسام، وحدّث بياناتهم الوظيفية وحالة تفعيلهم."
        />
        <EmployeesTable />
      </div>
    </RequirePermission>
  )
}
