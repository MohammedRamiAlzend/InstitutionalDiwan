import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { PageHeader } from '@/shared/ui'
import { NumberingRegistryTable } from '@/widgets/numbering-registry-table'
import { DepartmentRegisterSection } from './DepartmentRegisterSection'

/** `/numbering` — official numbering registries, counters, and department register books. */
export function NumberingPage() {
  return (
    <RequirePermission permission={Permissions.NumberingView}>
      <div className="flex flex-col gap-6">
        <PageHeader
          title="الترقيم"
          description="أنشئ سجلات الترقيم وحدّد نطاقها وقالب أرقامها وسياسة تصفيرها، وتابع العدادات وسجل أرقام الدوائر. تُحفظ كل الأرقام الصادرة ولا تُحذف."
        />
        <NumberingRegistryTable />
        <DepartmentRegisterSection />
      </div>
    </RequirePermission>
  )
}
