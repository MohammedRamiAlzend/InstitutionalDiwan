import { KeyRound, ShieldCheck, Sparkles, type LucideIcon } from 'lucide-react'
import { BrandName } from '@/entities/branding'
import { useSessionStore } from '@/entities/user'
import type { LabelKey } from '@/shared/config'
import { Badge, GuideProvider, GuideRegion, Label, PageHeader } from '@/shared/ui'

/**
 * Authenticated landing content; the app shell owns navigation and chrome.
 * KPI and recent-activity sections arrive with the Correspondence module.
 */
export function DashboardPage() {
  return (
    <GuideProvider pageId="dashboard">
      <DashboardContent />
    </GuideProvider>
  )
}

function DashboardContent() {
  const user = useSessionStore((state) => state.user)

  const stats: { id: string; labelKey: LabelKey; value: string | number; icon: LucideIcon; ltr?: boolean }[] = [
    {
      id: 'permissions',
      labelKey: 'dashboard.stats.permissions',
      value: user?.permissions.length ?? 0,
      icon: KeyRound,
    },
    {
      id: 'roles',
      labelKey: 'dashboard.stats.roles',
      value: user?.roles.join('، ') || '—',
      icon: ShieldCheck,
      ltr: true,
    },
  ]

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titleKey="page.dashboard.title"
        descriptionKey="page.dashboard.description"
        badge={<Badge variant="outline"><Label k="dashboard.badge" /></Badge>}
        pageGuide="dashboard"
      />

      <GuideRegion id="dashboard.welcome">
      <section className="rounded-2xl border border-border bg-surface p-6 sm:p-8">
        <p className="text-sm text-muted">
          <Label k="dashboard.welcome" /> <BrandName />
        </p>
        <h2 className="mt-1 font-heading text-2xl font-bold text-foreground">
          {user?.username ?? '—'}
        </h2>
        <p className="mt-2 max-w-2xl text-sm leading-relaxed text-muted">
          <Label k="dashboard.welcome-body" />
        </p>
      </section>
      </GuideRegion>

      <GuideRegion id="dashboard.stats">
      <section className="grid gap-4 sm:grid-cols-2">
        {stats.map(({ id, labelKey, value, icon: Icon, ltr }) => (
          <article
            key={id}
            className="flex items-center gap-4 rounded-xl border border-border bg-surface p-5"
          >
            <span className="flex size-11 shrink-0 items-center justify-center rounded-lg bg-accent/10 text-accent">
              <Icon className="size-5" aria-hidden />
            </span>
            <div className="min-w-0">
              <p className="text-xs text-muted"><Label k={labelKey} /></p>
              <p
                className="truncate font-heading text-xl font-bold text-foreground"
                dir={ltr ? 'ltr' : undefined}
              >
                {value}
              </p>
            </div>
          </article>
        ))}
      </section>
      </GuideRegion>

      <GuideRegion id="dashboard.next">
      <section className="rounded-xl border border-dashed border-border bg-surface/60 p-6">
        <h2 className="flex items-center gap-2 font-heading text-base font-semibold text-foreground">
          <Sparkles className="size-4 text-accent" aria-hidden />
          <Label k="dashboard.next-title" />
        </h2>
        <p className="mt-2 max-w-2xl text-sm leading-relaxed text-muted">
          <Label k="dashboard.next-body" />
        </p>
      </section>
      </GuideRegion>
    </div>
  )
}
