import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { PageHeader } from '@/shared/ui'
import { UsersTable } from '@/widgets/users-table'

/** `/users` — Module 02 user administration. */
export function UsersPage() {
  return (
    <RequirePermission permission={Permissions.UserManage}>
      <div className="flex flex-col gap-6">
        <PageHeader
          title="إدارة المستخدمين"
          description="أنشئ حسابات المستخدمين، وحدّث أقسامهم ونطاقات وصولهم، وأسند الأدوار وإسنادات الأقسام."
        />
        <UsersTable />
      </div>
    </RequirePermission>
  )
}
