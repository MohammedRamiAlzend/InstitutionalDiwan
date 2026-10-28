import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { PageHeader } from '@/shared/ui'
import { RolesTable } from '@/widgets/roles-table'

/** `/roles` — Module 02 role and permission administration. */
export function RolesPage() {
  return (
    <RequirePermission permission={Permissions.RoleManage}>
      <div className="flex flex-col gap-6">
        <PageHeader
          title="الأدوار والصلاحيات"
          description="أنشئ الأدوار المؤسسية وحدّث بياناتها، وحدد الصلاحيات الممنوحة لكل دور من مصفوفة الصلاحيات."
        />
        <RolesTable />
      </div>
    </RequirePermission>
  )
}
