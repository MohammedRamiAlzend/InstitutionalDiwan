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
            title="مخططات الحقول"
            description="حقول إضافية لكل نوع وثيقة تظهر في نموذج الإنشاء. كل تغيير يمرّ عبر نسخة جديدة تُنشر ولا تؤثر في الوثائق الموجودة."
            pageGuide="document-schemas"
          />
          <DocumentSchemaEditor />
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
