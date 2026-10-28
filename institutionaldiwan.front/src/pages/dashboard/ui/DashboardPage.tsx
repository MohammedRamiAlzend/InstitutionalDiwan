import { KeyRound, ShieldCheck, Sparkles } from 'lucide-react'
import { useSessionStore } from '@/entities/user'
import { Badge, PageHeader } from '@/shared/ui'

/**
 * Authenticated landing content; the app shell owns navigation and chrome.
 * KPI and recent-activity sections arrive with the Correspondence module.
 */
export function DashboardPage() {
  const user = useSessionStore((state) => state.user)

  const stats = [
    {
      id: 'permissions',
      label: 'الصلاحيات الفعّالة',
      value: user?.permissions.length ?? 0,
      icon: KeyRound,
    },
    {
      id: 'roles',
      label: 'الأدوار',
      value: user?.roles.join('، ') || '—',
      icon: ShieldCheck,
      ltr: true,
    },
  ]

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="لوحة التحكم"
        description="نظرة سريعة على حسابك وما هو متاح في مساحة العمل."
        badge={<Badge variant="outline">النسخة الأولية</Badge>}
      />

      <section className="rounded-2xl border border-border bg-surface p-6 sm:p-8">
        <p className="text-sm text-muted">مرحبًا بك في الديوان المؤسسي</p>
        <h2 className="mt-1 font-heading text-2xl font-bold text-foreground">
          {user?.username ?? '—'}
        </h2>
        <p className="mt-2 max-w-2xl text-sm leading-relaxed text-muted">
          تم تسجيل دخولك بنجاح. تُفعّل وحدات المراسلات والتنظيم والرقابة تدريجيًا،
          وستظهر روابطها في القائمة الجانبية عند إتاحتها لحسابك.
        </p>
      </section>

      <section className="grid gap-4 sm:grid-cols-2">
        {stats.map(({ id, label, value, icon: Icon, ltr }) => (
          <article
            key={id}
            className="flex items-center gap-4 rounded-xl border border-border bg-surface p-5"
          >
            <span className="flex size-11 shrink-0 items-center justify-center rounded-lg bg-accent/10 text-accent">
              <Icon className="size-5" aria-hidden />
            </span>
            <div className="min-w-0">
              <p className="text-xs text-muted">{label}</p>
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

      <section className="rounded-xl border border-dashed border-border bg-surface/60 p-6">
        <h2 className="flex items-center gap-2 font-heading text-base font-semibold text-foreground">
          <Sparkles className="size-4 text-accent" aria-hidden />
          ما التالي؟
        </h2>
        <p className="mt-2 max-w-2xl text-sm leading-relaxed text-muted">
          تُضاف قوائم المستخدمين والأدوار والموظفين ضمن وحدة الهوية، ثم سجل
          الوثائق والحركات وسجل التدقيق. تظهر العناصر غير الجاهزة في القائمة
          الجانبية بوسم «قريبًا» بدل فتح صفحات فارغة.
        </p>
      </section>
    </div>
  )
}
