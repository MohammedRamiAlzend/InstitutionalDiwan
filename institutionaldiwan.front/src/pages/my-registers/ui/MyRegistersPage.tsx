import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { GuideProvider, PageHeader } from '@/shared/ui'
import { MyRegistersTable } from '@/widgets/my-registers-table'

/** `/my-registers` — the registers of the caller's own department, one at a time. */
export function MyRegistersPage() {
  return (
    <RequirePermission permission={Permissions.NumberingView}>
      <GuideProvider pageId="my-registers">
        <div className="flex flex-col gap-6">
          <PageHeader
            title="سجلاتي"
            description="اختر أحد سجلات قسمك لتتصفّح أسطره: ابحث برقم السجل أو الوثيقة، وصفِّ بالسنة والحالة والتاريخ، واعرض بيانات نموذج سجل القسم حسب إصداره."
            pageGuide="my-registers"
          />
          <MyRegistersTable />
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
