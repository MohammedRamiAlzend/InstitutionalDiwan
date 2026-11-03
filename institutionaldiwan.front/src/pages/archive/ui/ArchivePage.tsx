import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { GuideProvider, GuideRegion, PageHeader } from '@/shared/ui'
import { ArchiveSearchTable } from '@/widgets/archive-search-table'

/**
 * `/archive` — search of the archived documents the caller may view, with personal saved searches.
 * Archiving and unarchiving happen on the document details page.
 */
export function ArchivePage() {
  return (
    <RequirePermission permission={Permissions.DocumentView}>
      <GuideProvider pageId="archive">
        <div className="flex flex-col gap-6">
          <PageHeader
            title="الأرشيف"
            description="الوثائق المؤرشفة التي يحق لك الاطلاع عليها. الوثيقة المؤرشفة مجمّدة أمام التعديل حتى تُلغى أرشفتها من صفحة تفاصيلها."
            pageGuide="archive"
          />
          <GuideRegion id="archive.grid">
            <ArchiveSearchTable />
          </GuideRegion>
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
