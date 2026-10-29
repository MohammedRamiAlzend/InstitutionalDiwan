import { Hash } from 'lucide-react'
import { NUMBERING_RESET_POLICY } from '@/shared/config'
import { cn, formatNumber } from '@/shared/lib'
import { Badge } from '@/shared/ui'
import type { NumberingRegistryResponse } from '../model/types'

const resetPolicyLabels: Record<number, string> = {
  [NUMBERING_RESET_POLICY.Yearly]: 'سنوي',
  [NUMBERING_RESET_POLICY.Monthly]: 'شهري',
  [NUMBERING_RESET_POLICY.Never]: 'لا يتصفّر',
}

interface RegistryScopeCardProps {
  registry: NumberingRegistryResponse
  /** Department name resolved by the caller; ids alone are not user-friendly. */
  departmentName?: string | null
  documentTypeLabel?: string | null
  directionLabel?: string | null
  className?: string
}

/** Read-only summary of a registry: scope, number template, and sequence counters. */
export function RegistryScopeCard({
  registry,
  departmentName,
  documentTypeLabel,
  directionLabel,
  className,
}: RegistryScopeCardProps) {
  const scopeChips = [
    departmentName ? { key: 'department', label: `الدائرة: ${departmentName}` } : null,
    documentTypeLabel ? { key: 'type', label: `النوع: ${documentTypeLabel}` } : null,
    directionLabel ? { key: 'direction', label: `الاتجاه: ${directionLabel}` } : null,
  ].filter((chip): chip is { key: string; label: string } => chip !== null)

  return (
    <section
      className={cn(
        'flex flex-col gap-4 rounded-xl border border-border bg-background p-4',
        className,
      )}
    >
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <code
              dir="ltr"
              className="rounded-md bg-surface-muted px-2 py-0.5 text-xs font-semibold text-foreground"
            >
              {registry.code}
            </code>
            <span className="text-sm font-semibold text-foreground">{registry.nameAr}</span>
          </div>
          <div className="flex flex-wrap items-center gap-1.5">
            {scopeChips.length === 0 ? (
              <Badge variant="outline">سجل شامل</Badge>
            ) : (
              scopeChips.map((chip) => (
                <Badge key={chip.key} variant="outline">
                  {chip.label}
                </Badge>
              ))
            )}
          </div>
        </div>
        <Badge variant={registry.isActive ? 'success' : 'danger'}>
          {registry.isActive ? 'نشط' : 'موقوف'}
        </Badge>
      </div>

      <dl className="grid gap-3 text-sm sm:grid-cols-2">
        <div className="flex flex-col gap-1">
          <dt className="text-xs text-muted">قالب الرقم</dt>
          <dd>
            <code dir="ltr" className="text-xs text-foreground">
              {registry.formatPattern}
            </code>
          </dd>
        </div>
        <div className="flex flex-col gap-1">
          <dt className="text-xs text-muted">سياسة التصفير</dt>
          <dd className="text-foreground">
            {resetPolicyLabels[registry.resetPolicyId] ?? `#${registry.resetPolicyId}`}
          </dd>
        </div>
      </dl>

      <div className="flex flex-col gap-2">
        <h3 className="flex items-center gap-1.5 text-xs font-semibold text-muted">
          <Hash className="size-3.5" aria-hidden />
          العدادات
        </h3>
        {registry.counters.length === 0 ? (
          <p className="text-xs text-muted">لم يُصدر أي رقم من هذا السجل بعد.</p>
        ) : (
          <ul className="flex flex-wrap gap-2">
            {registry.counters.map((counter) => (
              <li
                key={counter.counterId}
                className="flex items-center gap-2 rounded-lg border border-border px-2.5 py-1.5 text-xs"
              >
                <span dir="ltr" className="text-muted">
                  {counter.numberingYear}
                </span>
                <span dir="ltr" className="font-semibold text-foreground">
                  {formatNumber(counter.lastValue)}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  )
}
