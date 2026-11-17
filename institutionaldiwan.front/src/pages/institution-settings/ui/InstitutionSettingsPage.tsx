import { RequirePermission } from '@/features/auth'
import { InstitutionSettingsForm } from '@/features/institution-settings-manage'
import { Permissions } from '@/shared/config'
import { GuideProvider, PageHeader } from '@/shared/ui'

export function InstitutionSettingsPage() {
  return (
    <RequirePermission permission={Permissions.DepartmentManage}>
      <GuideProvider pageId="institution-settings">
        <div className="flex flex-col gap-6">
          <PageHeader
            titleKey="page.institution-settings.title"
            descriptionKey="page.institution-settings.description"
            pageGuide="institution-settings"
          />
          <InstitutionSettingsForm />
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
