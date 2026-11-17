import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { GuideProvider, GuideRegion, PageHeader } from '@/shared/ui'
import { SystemErrorsTable } from '@/widgets/system-errors-table'

/** `/system-errors` — captured server and browser errors, for the developer. */
export function SystemErrorsPage() {
  return (
    <RequirePermission permission={Permissions.SystemErrorsView}>
      <GuideProvider pageId="system-errors">
        <div className="flex flex-col gap-6">
          <PageHeader
            titleKey="page.system-errors.title"
            descriptionKey="page.system-errors.description"
            pageGuide="system-errors"
          />
          <GuideRegion id="system-errors.grid">
            <SystemErrorsTable />
          </GuideRegion>
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
