import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { GuideProvider, PageHeader } from '@/shared/ui'
import { DocumentSchemaEditor } from '@/widgets/document-schema-editor'

/** `/document-schemas` — per-document-type dynamic field schemas and their versions. */
export function DocumentSchemasPage() {
  return (
    <RequirePermission permission={Permissions.LookupManage}>
      <GuideProvider pageId="document-schemas">
        <div className="flex flex-col gap-6">
          <PageHeader
            titleKey="page.document-schemas.title"
            descriptionKey="page.document-schemas.description"
            pageGuide="document-schemas"
          />
          <DocumentSchemaEditor />
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
