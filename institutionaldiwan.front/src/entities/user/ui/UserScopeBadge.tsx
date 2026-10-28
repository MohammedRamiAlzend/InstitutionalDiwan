import { Building2, Globe2, Landmark, Network } from 'lucide-react'
import { cn } from '@/shared/lib'

const scopeMeta: Record<number, { label: string; icon: typeof Globe2 }> = {
  1: { label: 'الدائرة', icon: Building2 },
  2: { label: 'الدائرة والفروع', icon: Network },
  3: { label: 'المؤسسة', icon: Landmark },
  4: { label: 'النظام', icon: Globe2 },
}

interface UserScopeBadgeProps {
  /** Backend `ScopeTypeId` (1–4). */
  scopeTypeId: number
  className?: string
}

export function UserScopeBadge({ scopeTypeId, className }: UserScopeBadgeProps) {
  const meta = scopeMeta[scopeTypeId]
  const Icon = meta?.icon

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 whitespace-nowrap rounded-full bg-surface-muted px-2.5 py-0.5 text-xs font-medium text-muted',
        className,
      )}
    >
      {meta && Icon ? (
        <>
          <Icon className="size-3.5" aria-hidden />
          {meta.label}
        </>
      ) : (
        `نطاق #${scopeTypeId}`
      )}
    </span>
  )
}
