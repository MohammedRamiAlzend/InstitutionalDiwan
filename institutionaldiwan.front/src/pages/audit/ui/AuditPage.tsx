import { useSearchParams } from 'react-router-dom'
import { List, ShieldCheck } from 'lucide-react'
import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { cn, t } from '@/shared/lib'
import { GuideProvider, GuideRegion, Label, PageHeader } from '@/shared/ui'
import { AuditIntegrityTable, AuditLogTable } from '@/widgets/audit-log-table'

const PANELS = [
  {
    key: 'logs',
    labelKey: 'audit.tab.logs',
    hintKey: 'audit.tab.logs-hint',
    icon: List,
  },
  {
    key: 'integrity',
    labelKey: 'audit.tab.integrity',
    hintKey: 'audit.tab.integrity-hint',
    icon: ShieldCheck,
  },
] as const

type PanelKey = (typeof PANELS)[number]['key']

function resolvePanel(raw: string | null): PanelKey {
  return raw === 'integrity' ? 'integrity' : 'logs'
}

/**
 * `/audit` — tamper-evident audit search plus integrity reports. Reading the log
 * appends a self-audit row, so filters are debounced and there is no polling.
 */
export function AuditPage() {
  return (
    <RequirePermission permission={Permissions.AuditView}>
      <GuideProvider pageId="audit">
        <AuditView />
      </GuideProvider>
    </RequirePermission>
  )
}

function AuditView() {
  const [searchParams, setSearchParams] = useSearchParams()
  const activePanel = resolvePanel(searchParams.get('panel'))

  // Each panel owns its own table state, so switching starts from a clean page.
  const setPanel = (panel: PanelKey) => {
    setSearchParams(panel === 'logs' ? {} : { panel })
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titleKey="page.audit.title"
        descriptionKey="page.audit.description"
        pageGuide="audit"
      />

      <GuideRegion id="audit.tabs">
      <div role="tablist" aria-label={t('audit.page.tabs-aria')} className="flex flex-wrap items-center gap-2">
        {PANELS.map((panel) => {
          const Icon = panel.icon
          const isActive = activePanel === panel.key

          return (
            <button
              key={panel.key}
              type="button"
              role="tab"
              aria-selected={isActive}
              onClick={() => setPanel(panel.key)}
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
                <Label k={panel.labelKey} />
              </span>
              <span className="text-xs text-muted"><Label k={panel.hintKey} /></span>
            </button>
          )
        })}
      </div>
      </GuideRegion>

      <div role="tabpanel">
        {activePanel === 'logs' ? (
          <GuideRegion id="audit.logs">
            <AuditLogTable />
          </GuideRegion>
        ) : (
          <GuideRegion id="audit.integrity">
            <AuditIntegrityTable />
          </GuideRegion>
        )}
      </div>
    </div>
  )
}
