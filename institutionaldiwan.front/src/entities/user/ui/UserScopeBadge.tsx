import { Building2, Globe2, Landmark, Network } from 'lucide-react'
import type { LabelKey } from '@/shared/config'
import { cn, useLabel } from '@/shared/lib'

const scopeMeta: Record<number, { labelKey: LabelKey; icon: typeof Globe2 }> = {
  1: { labelKey: 'user.scope.department', icon: Building2 },
  2: { labelKey: 'user.scope.department-tree', icon: Network },
  3: { labelKey: 'user.scope.institution', icon: Landmark },
  4: { labelKey: 'user.scope.system', icon: Globe2 },
}

interface UserScopeBadgeProps {
  /** Backend `ScopeTypeId` (1–4). */
  scopeTypeId: number
  className?: string
}

export function UserScopeBadge({ scopeTypeId, className }: UserScopeBadgeProps) {
  const meta = scopeMeta[scopeTypeId]
  const Icon = meta?.icon
  const text = useLabel(
    meta ? meta.labelKey : 'user.scope.fallback',
    meta ? undefined : { id: scopeTypeId },
  )

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
          {text}
        </>
      ) : (
        text
      )}
    </span>
  )
}