import { useSearchParams } from 'react-router-dom'
import { List, ShieldCheck } from 'lucide-react'
import { RequirePermission } from '@/features/auth'
import { Permissions } from '@/shared/config'
import { cn } from '@/shared/lib'
import { PageHeader } from '@/shared/ui'
import { AuditIntegrityTable, AuditLogTable } from '@/widgets/audit-log-table'

const PANELS = [
  {
    key: 'logs',
    label: 'السجل',
    hint: 'ابحث في سلسلة الأحداث وافرزها حسب الوحدة أو الإجراء',
    icon: List,
  },
  {
    key: 'integrity',
    label: 'تقارير السلامة',
    hint: 'نتائج فحص تكامل السلسلة المحفوظة',
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
      <AuditView />
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
        title="سجل التدقيق"
        description="سلسلة تدقيق غير قابلة للتعديل: كل إجراء يُخزَّن مع تجزئة تربطه بالسجل السابق. ابحث في السجلات وفُتش تكامل السلسلة عند الحاجة؛ كل عملية بحث تُسجَّل هي أيضًا."
      />

      <div role="tablist" aria-label="أقسام سجل التدقيق" className="flex flex-wrap items-center gap-2">
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
                {panel.label}
              </span>
              <span className="text-xs text-muted">{panel.hint}</span>
            </button>
          )
        })}
      </div>

      <div role="tabpanel">
        {activePanel === 'logs' ? <AuditLogTable /> : <AuditIntegrityTable />}
      </div>
    </div>
  )
}
