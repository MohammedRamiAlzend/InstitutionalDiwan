import { cn, formatShortId } from '@/shared/lib'

interface NameWithIdProps {
  /** The resolved display name; `null` while loading or when the caller may not read it. */
  name: string | null | undefined
  /** The full identifier; shown shortened and complete in the tooltip. */
  id: string
  className?: string
}

/**
 * A name followed by its short identifier («أحمد  9f55121c»). Without a name only the identifier
 * is shown, which is how references rendered before names were resolved. The identifier is a
 * Latin value, so it sits in a left-to-right isolate.
 */
export function NameWithId({ name, id, className }: NameWithIdProps) {
  return (
    <span className={cn('inline-flex flex-wrap items-baseline gap-x-1.5', className)} title={id}>
      {name ? <span className="text-foreground">{name}</span> : null}
      <bdi dir="ltr" className="font-mono text-xs text-muted">
        {formatShortId(id)}
      </bdi>
    </span>
  )
}
