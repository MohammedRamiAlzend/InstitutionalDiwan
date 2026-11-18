import { Lock } from 'lucide-react'
import { t, useLabel } from '@/shared/lib'

/** Stands in for the title on a queue or register row whose document the caller cannot view. */
export function RestrictedDocumentLabel() {
  const label = useLabel('documents.restricted.label')
  return (
    <span
      title={t('documents.restricted.tooltip')}
      className="inline-flex items-center gap-1.5 text-sm text-muted"
    >
      <Lock className="size-3.5 shrink-0" aria-hidden />
      {label}
    </span>
  )
}