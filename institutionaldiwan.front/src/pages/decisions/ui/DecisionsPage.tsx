import { useSearchParams } from 'react-router-dom'
import { Scale, Shapes } from 'lucide-react'
import { useSessionStore } from '@/entities/user'
import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { cn } from '@/shared/lib'
import { GuideProvider, GuideRegion, PageHeader } from '@/shared/ui'
import { DecisionRegisterTable } from './DecisionRegisterTable'
import { DecisionTypesTable } from './DecisionTypesTable'

const TABS = [
  { key: 'register', label: 'سجل القرارات', icon: Scale },
  { key: 'types', label: 'أنواع القرارات', icon: Shapes },
] as const

type TabKey = (typeof TABS)[number]['key']

function resolveTab(raw: string | null, canManageTypes: boolean): TabKey {
  return raw === 'types' && canManageTypes ? 'types' : 'register'
}

/** `/decisions` — the decision register, and decision types for lookup administrators. */
export function DecisionsPage() {
  return (
    <RequirePermission permission={Permissions.DocumentView}>
      <GuideProvider pageId="decisions">
        <DecisionsView />
      </GuideProvider>
    </RequirePermission>
  )
}

function DecisionsView() {
  const [searchParams, setSearchParams] = useSearchParams()
  const canManageTypes = useSessionStore((state) => state.hasPermission(Permissions.LookupManage))
  const activeTab = resolveTab(searchParams.get('tab'), canManageTypes)

  const setTab = (tab: TabKey) => {
    setSearchParams(tab === 'register' ? {} : { tab }, { replace: true })
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="القرارات"
        description="القرارات الصادرة على الوثائق التي تملك الاطلاع عليها، وأنواع القرارات التي يُعدّها مدير القوائم."
        pageGuide="decisions"
      />

      {canManageTypes && (
        <GuideRegion id="decisions.tabs">
          <div role="tablist" aria-label="أقسام صفحة القرارات" className="flex flex-wrap items-center gap-2">
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
                    'inline-flex items-center gap-2 rounded-xl border px-4 py-2.5 text-sm transition-colors',
                    isActive
                      ? 'border-accent bg-accent/10 font-semibold'
                      : 'border-border bg-surface hover:bg-surface-muted/60',
                  )}
                >
                  <Icon className="size-4" aria-hidden />
                  {tab.label}
                </button>
              )
            })}
          </div>
        </GuideRegion>
      )}

      {activeTab === 'types' ? <DecisionTypesTable /> : <DecisionRegisterTable />}
    </div>
  )
}
