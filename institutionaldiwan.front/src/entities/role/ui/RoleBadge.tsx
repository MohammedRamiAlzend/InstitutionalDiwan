import { Lock } from 'lucide-react'
import { cn } from '@/shared/lib'

interface RoleBadgeProps {
  nameAr: string
  code: string
  isSystemRole?: boolean
  className?: string
}

/** Role name with its LTR code and a system-role lock marker. */
export function RoleBadge({
  nameAr,
  code,
  isSystemRole = false,
  className,
}: RoleBadgeProps) {
  return (
    <span className={cn('inline-flex min-w-0 items-center gap-2', className)}>
      <span className="truncate text-sm font-medium text-foreground">{nameAr}</span>
      <span
        dir="ltr"
        className="shrink-0 rounded border border-border px-1.5 py-0.5 font-mono text-[11px] leading-none text-muted"
      >
        {code}
      </span>
      {isSystemRole && (
        <Lock
          role="img"
          className="size-3.5 shrink-0 text-muted"
          aria-label="دور معرّف من النظام"
        />
      )}
    </span>
  )
}
