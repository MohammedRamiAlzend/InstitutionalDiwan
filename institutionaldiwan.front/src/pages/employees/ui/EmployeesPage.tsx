import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { GuideProvider, GuideRegion, PageHeader } from '@/shared/ui'
import { EmployeesTable } from '@/widgets/employees-table'

/** `/employees` — Module 02 employee record administration. */
export function EmployeesPage() {
  return (
    <RequirePermission permission={Permissions.EmployeeManage}>
      <GuideProvider pageId="employees">
        <div className="flex flex-col gap-6">
          <PageHeader
            title="إدارة الموظفين"
            description="أضف سجلات الموظفين واربطها بالأقسام، وحدّث بياناتهم الوظيفية وحالة تفعيلهم."
            pageGuide="employees"
          />
          <GuideRegion id="employees.grid">
            <EmployeesTable />
          </GuideRegion>
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
