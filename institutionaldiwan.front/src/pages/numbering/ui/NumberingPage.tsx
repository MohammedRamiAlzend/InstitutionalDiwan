import { useSearchParams } from 'react-router-dom'
import { BookOpen, ScrollText } from 'lucide-react'
import { RequirePermission } from '@/features/auth'
import { Permissions, type LabelKey } from '@/shared/config'
import { cn, t } from '@/shared/lib'
import { GuideProvider, GuideRegion, Label, PageHeader } from '@/shared/ui'
import { DepartmentRegisterBook } from '@/widgets/department-register-book'
import { NumberingRegistryTable } from '@/widgets/numbering-registry-table'

const TABS = [
  {
    key: 'registries',
    labelKey: 'numbering.tabs.registries',
    hintKey: 'numbering.tabs.registries-hint',
    icon: ScrollText,
  },
  {
    key: 'register',
    labelKey: 'numbering.tabs.register',
    hintKey: 'numbering.tabs.register-hint',
    icon: BookOpen,
  },
] as const satisfies readonly { key: string; labelKey: LabelKey; hintKey: LabelKey; icon: typeof ScrollText }[]

type TabKey = (typeof TABS)[number]['key']

function resolveTab(raw: string | null): TabKey {
  return raw === 'register' ? 'register' : 'registries'
}

/** `/numbering` — official numbering registries, counters, and department register books. */
export function NumberingPage() {
  return (
    <RequirePermission permission={Permissions.NumberingView}>
      <GuideProvider pageId="numbering">
        <NumberingView />
      </GuideProvider>
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
        titleKey="page.numbering.title"
        descriptionKey="page.numbering.description"
        pageGuide="numbering"
      />

      <GuideRegion id="numbering.tabs">
      <div
        role="tablist"
        aria-label={t('numbering.tabs.aria')}
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
                <Label k={tab.labelKey} />
              </span>
              <span className="text-xs text-muted"><Label k={tab.hintKey} /></span>
            </button>
          )
        })}
      </div>
      </GuideRegion>

      <div role="tabpanel">
        {activeTab === 'registries' ? (
          <GuideRegion id="numbering.registries">
            <NumberingRegistryTable />
          </GuideRegion>
        ) : (
          <GuideRegion id="numbering.register-book">
            <DepartmentRegisterBook />
          </GuideRegion>
        )}
      </div>
    </div>
  )
}
