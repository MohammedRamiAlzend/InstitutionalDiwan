import { Badge } from '@/shared/ui'
import { cn } from '@/shared/lib'

interface DepartmentBadgeProps {
  nameAr: string
  code: string
  isActive?: boolean
  className?: string
}

/** Department name with its LTR code and an optional inactive marker. */
export function DepartmentBadge({
  nameAr,
  code,
  isActive = true,
  className,
}: DepartmentBadgeProps) {
  return (
    <span className={cn('inline-flex min-w-0 items-center gap-2', className)}>
      <span className="truncate text-sm font-medium text-foreground">{nameAr}</span>
      <span
        dir="ltr"
        className="shrink-0 rounded border border-border px-1.5 py-0.5 font-mono text-[11px] leading-none text-muted"
      >
        {code}
      </span>
      {!isActive && <Badge variant="danger">موقوف</Badge>}
    </span>
  )
}
