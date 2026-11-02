import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { GuideProvider, PageHeader } from '@/shared/ui'
import { DepartmentHierarchyTree } from '@/widgets/department-hierarchy-tree'

/** `/departments` — Module 01 organizational hierarchy. */
export function DepartmentsPage() {
  return (
    <RequirePermission permission={Permissions.DepartmentManage}>
      <GuideProvider pageId="departments">
        <div className="flex flex-col gap-6">
          <PageHeader
            title="الأقسام التنظيمية"
            description="إدارة الهيكل التنظيمي والأقسام ومديريها، مع عرض الموظفين والشجرة والمخططات والجدول."
            pageGuide="departments"
          />
          <DepartmentHierarchyTree />
        </div>
      </GuideProvider>
    </RequirePermission>
  )
}
