import { useSearchParams } from 'react-router-dom'
import { BookOpen, ScrollText } from 'lucide-react'
import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { cn } from '@/shared/lib'
import { PageHeader } from '@/shared/ui'
import { DepartmentRegisterBook } from '@/widgets/department-register-book'
import { NumberingRegistryTable } from '@/widgets/numbering-registry-table'

const TABS = [
  {
    key: 'registries',
    label: 'سجلات الترقيم',
    hint: 'نطاقات الأرقام وقالبها وعداداتها',
    icon: ScrollText,
  },
  {
    key: 'register',
    label: 'سجل الدوائر',
    hint: 'الأرقام التي كتبتها الدوائر في سجلاتها',
    icon: BookOpen,
  },
] as const

type TabKey = (typeof TABS)[number]['key']

function resolveTab(raw: string | null): TabKey {
  return raw === 'register' ? 'register' : 'registries'
}

/** `/numbering` — official numbering registries, counters, and department register books. */
export function NumberingPage() {
  return (
    <RequirePermission permission={Permissions.NumberingView}>
      <NumberingView />
    </RequirePermission>
  )
}

function NumberingView() {
  const [searchParams, setSearchParams] = useSearchParams()
  const activeTab = resolveTab(searchParams.get('tab'))

  // Each tab owns its own table state, so switching starts from a clean query.
  const setTab = (tab: TabKey) => {
    setSearchParams(tab === 'registries' ? {} : { tab })
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="الترقيم"
        description="أنشئ سجلات الترقيم وحدّد نطاقها وقالب أرقامها وسياسة تصفيرها، وتابع العدادات وسجل أرقام الدوائر. تُحفظ كل الأرقام الصادرة ولا تُحذف."
      />

      <div
        role="tablist"
        aria-label="أقسام صفحة الترقيم"
        className="flex flex-wrap items-center gap-2"
      >
        {TABS.map((tab) => {
          const Icon = tab.icon
          const isActive = activeTab === tab.key

          return (
            <button
              key={tab.key}
              type="button"
              role="tab"
              aria-selected={isActive}
              onClick={() => setTab(tab.key)}
              className={cn(
                'flex flex-col items-start gap-0.5 rounded-xl border px-4 py-2.5 text-start transition-colors',
                isActive
                  ? 'border-accent bg-accent/10'
                  : 'border-border bg-surface hover:bg-surface-muted/60',
              )}
            >
              <span
                className={cn(
                  'flex items-center gap-1.5 text-sm font-semibold',
                  isActive ? 'text-accent' : 'text-foreground',
                )}
              >
                <Icon className="size-4" aria-hidden />
                {tab.label}
              </span>
              <span className="text-xs text-muted">{tab.hint}</span>
            </button>
          )
        })}
      </div>

      <div role="tabpanel">
        {activeTab === 'registries' ? (
          <NumberingRegistryTable />
        ) : (
          <DepartmentRegisterBook />
        )}
      </div>
    </div>
  )
}
