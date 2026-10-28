import { cn } from '@/shared/lib'

interface OfficialNumberTagProps {
  officialNumberText: string | null | undefined
  /** Numbering year, when the caller has it; rendered after the number. */
  numberingYear?: number | null
  className?: string
}

/** Official number in LTR monospace; Arabic UI keeps it readable as a code, never RTL-reordered. */
export function OfficialNumberTag({
  officialNumberText,
  numberingYear,
  className,
}: OfficialNumberTagProps) {
  if (!officialNumberText) {
    return <span className={cn('text-sm text-muted', className)}>بدون رقم</span>
  }

  return (
    <span
      dir="ltr"
      className={cn('inline-flex items-baseline gap-1 font-mono text-sm text-foreground', className)}
    >
      <span>{officialNumberText}</span>
      {numberingYear ? <span className="text-muted">/ {numberingYear}</span> : null}
    </span>
  )
}
