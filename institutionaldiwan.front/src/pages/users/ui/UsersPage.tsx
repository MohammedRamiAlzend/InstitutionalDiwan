import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { GuideRegion, GuideProvider, PageHeader } from '@/shared/ui'
import { UsersTable } from '@/widgets/users-table'

/** `/users` — Module 02 user administration. */
export function UsersPage() {
  return (
    <RequirePermission permission={Permissions.UserManage}>
      <GuideProvider pageId="users">
        <div className="flex flex-col gap-6">
          <PageHeader
            title="إدارة المستخدمين"
            description="أنشئ حسابات المستخدمين، وحدّث أقسامهم ونطاقات وصولهم، وأسند الأدوار وإسنادات الأقسام."
            pageGuide="users"
          />
          <GuideRegion id="users.grid">
            <UsersTable />
          </GuideRegion>
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
