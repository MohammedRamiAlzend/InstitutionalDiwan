import { useSearchParams } from 'react-router-dom'
import { Building2, ScrollText } from 'lucide-react'
import { RequirePermission } from '@/features/auth'
import { FEATURES, Permissions } from '@/shared/config'
import { cn, t } from '@/shared/lib'
import { GuideProvider, GuideRegion, Label, PageHeader } from '@/shared/ui'
import { DocumentRegistryTable } from '@/widgets/document-registry-table'
import { ExternalOrganizationsTable } from '@/widgets/external-organizations-table'

const TABS = [
  {
    key: 'registry',
    labelKey: 'documents.registry-title',
    hintKey: 'documents.registry-hint',
    icon: ScrollText,
  },
  {
    key: 'organizations',
    labelKey: 'documents.organizations-title',
    hintKey: 'documents.organizations-hint',
    icon: Building2,
  },
] as const

type TabKey = (typeof TABS)[number]['key']

function resolveTab(raw: string | null): TabKey {
  return raw === 'organizations' && FEATURES.externalOrganizations ? 'organizations' : 'registry'
}

/** `/documents` — Module 03 correspondence registry plus the external-organizations admin tab. */
export function DocumentsPage() {
  return (
    <RequirePermission permission={Permissions.DocumentView}>
      <GuideProvider pageId="documents">
        <DocumentsView />
      </GuideProvider>
    </RequirePermission>
  )
}

function DocumentsView() {
  const [searchParams, setSearchParams] = useSearchParams()
  const activeTab = resolveTab(searchParams.get('tab'))

  // Each tab owns its own table state, so switching starts from a clean query.
  const setTab = (tab: TabKey) => {
    setSearchParams(tab === 'registry' ? {} : { tab })
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titleKey="page.documents.title"
        description={`${t('documents.page-description')}${FEATURES.drafts ? t('documents.page-description-drafts') : ''}${FEATURES.externalOrganizations ? t('documents.page-description-external') : ''}.`}
        pageGuide="documents"
      />

      {FEATURES.externalOrganizations && (
      <GuideRegion id="documents.tabs">
      <div
        role="tablist"
        aria-label={t('documents.tabs-aria')}
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
      )}

      <div role="tabpanel">
        {activeTab === 'registry' ? (
          <GuideRegion id="documents.registry">
            <DocumentRegistryTable />
          </GuideRegion>
        ) : (
          <GuideRegion id="documents.organizations">
            <ExternalOrganizationsTable />
          </GuideRegion>
        )}
      </div>
    </div>
  )
}
