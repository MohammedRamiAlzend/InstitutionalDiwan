import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { PageHeader } from '@/shared/ui'
import { DocumentRegistryTable } from '@/widgets/document-registry-table'

/** `/documents` — Module 03 correspondence registry. */
export function DocumentsPage() {
  return (
    <RequirePermission permission={Permissions.DocumentView}>
      <div className="flex flex-col gap-6">
        <PageHeader
          title="سجل الوثائق"
          description="استعرض الوثائق الصادرة والواردة والداخلية، أو أنشئ وثيقة جديدة كمسودة أو برقم رسمي."
        />
        <DocumentRegistryTable />
      </div>
    </RequirePermission>
  )
}
