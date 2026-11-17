import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { GuideProvider, GuideRegion, PageHeader } from '@/shared/ui'
import { RolesTable } from '@/widgets/roles-table'

/** `/roles` — Module 02 role and permission administration. */
export function RolesPage() {
  return (
    <RequirePermission permission={Permissions.RoleManage}>
      <GuideProvider pageId="roles">
        <div className="flex flex-col gap-6">
          <PageHeader
            titleKey="page.roles.title"
            descriptionKey="page.roles.description"
            pageGuide="roles"
          />
          <GuideRegion id="roles.grid">
            <RolesTable />
          </GuideRegion>
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
