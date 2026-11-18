import { useSearchParams } from 'react-router-dom'
import { Inbox, Send } from 'lucide-react'
import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { cn, t } from '@/shared/lib'
import { GuideProvider, GuideRegion, Label, PageHeader } from '@/shared/ui'
import { MovementQueueTable } from '@/widgets/movement-inbox'

const TABS = [
  {
    key: 'inbox',
    labelKey: 'movements.tab.inbox',
    hintKey: 'movements.tab.inbox-hint',
    icon: Inbox,
  },
  {
    key: 'outbox',
    labelKey: 'movements.tab.outbox',
    hintKey: 'movements.tab.outbox-hint',
    icon: Send,
  },
] as const

type TabKey = (typeof TABS)[number]['key']

function resolveTab(raw: string | null): TabKey {
  return raw === 'outbox' ? 'outbox' : 'inbox'
}

/** `/movements` — the caller's department inbox and outbox. */
export function MovementsPage() {
  return (
    <RequirePermission permission={Permissions.DocumentView}>
      <GuideProvider pageId="movements">
        <MovementsView />
      </GuideProvider>
    </RequirePermission>
  )
}

function MovementsView() {
  const [searchParams, setSearchParams] = useSearchParams()
  const activeTab = resolveTab(searchParams.get('tab'))

  const setTab = (tab: TabKey) => {
    setSearchParams(tab === 'inbox' ? {} : { tab })
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titleKey="page.movements.title"
        descriptionKey="page.movements.description"
        pageGuide="movements"
      />

      <GuideRegion id="movements.tabs">
      <div
        role="tablist"
        aria-label={t('movements.tabs-aria')}
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
        <GuideRegion id="movements.queue">
          <MovementQueueTable kind={activeTab} />
        </GuideRegion>
      </div>
    </div>
  )
}
