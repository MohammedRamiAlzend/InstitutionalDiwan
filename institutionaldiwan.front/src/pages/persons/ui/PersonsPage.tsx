import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { GuideProvider, GuideRegion, PageHeader } from '@/shared/ui'
import { PersonsTable } from '@/widgets/persons-table'

/** `/persons` — everyone named on documents the caller may view. */
export function PersonsPage() {
  return (
    <RequirePermission permission={Permissions.PersonView}>
      <GuideProvider pageId="persons">
        <div className="flex flex-col gap-6">
          <PageHeader
            titleKey="page.persons.title"
            descriptionKey="page.persons.description"
            pageGuide="persons"
          />
          <GuideRegion id="persons.grid">
            <PersonsTable />
          </GuideRegion>
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
