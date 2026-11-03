import { Lock } from 'lucide-react'
import { RESTRICTED_DOCUMENT_LABEL } from '../model/restricted'

/** Stands in for the title on a queue or register row whose document the caller cannot view. */
export function RestrictedDocumentLabel() {
  return (
    <span
      title="لا تملك صلاحية الاطلاع على هذه الوثيقة"
      className="inline-flex items-center gap-1.5 text-sm text-muted"
    >
      <Lock className="size-3.5 shrink-0" aria-hidden />
      {RESTRICTED_DOCUMENT_LABEL}
    </span>
  )
}
