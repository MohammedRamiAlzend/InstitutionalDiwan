import { useState } from 'react'
import { Search } from 'lucide-react'
import {
  DocumentStatusBadge,
  OfficialNumberTag,
  useDocuments,
  type DocumentResponse,
} from '@/entities/document'
import { DOCUMENT_STATUS } from '@/shared/config'
import { cn, useDebouncedValue } from '@/shared/lib'
import { Input, Skeleton } from '@/shared/ui'

const PAGE_SIZE = 8

interface DocumentPickerProps {
  /** The document being linked from; it can never be its own target. */
  excludeDocumentId: string
  selected: DocumentResponse | null
  onSelect: (document: DocumentResponse | null) => void
  invalid?: boolean
}

/** Searches the document registry (access-filtered by the API) and picks one link target. */
export function DocumentPicker({
  excludeDocumentId,
  selected,
  onSelect,
  invalid,
}: DocumentPickerProps) {
  const [term, setTerm] = useState('')
  const search = useDebouncedValue(term.trim(), 300)
  const query = useDocuments(
    { search: search || undefined, page: 1, pageSize: PAGE_SIZE },
    { enabled: !selected },
  )

  const candidates = (query.data?.items ?? []).filter(
    (document) =>
      document.documentId !== excludeDocumentId && document.statusId !== DOCUMENT_STATUS.Canceled,
  )

  if (selected) {
    return (
      <div className="flex items-center justify-between gap-3 rounded-lg border border-border bg-surface px-3 py-2">
        <div className="min-w-0">
          <p className="truncate text-sm font-medium">{selected.title}</p>
          <OfficialNumberTag officialNumberText={selected.officialNumberText} />
        </div>
        <button
          type="button"
          className="shrink-0 text-sm text-accent hover:underline"
          onClick={() => onSelect(null)}
        >
          تغيير
        </button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-2">
      <Input
        aria-label="بحث عن وثيقة"
        placeholder="ابحث بالعنوان أو رقم النظام"
        startIcon={<Search className="size-4" aria-hidden />}
        value={term}
        invalid={invalid}
        onChange={(event) => setTerm(event.target.value)}
      />
      <ul
        aria-label="نتائج البحث عن وثيقة"
        className="max-h-56 overflow-y-auto rounded-lg border border-border"
      >
        {query.isLoading && (
          <li className="p-3">
            <Skeleton className="h-8 w-full" />
          </li>
        )}
        {!query.isLoading && candidates.length === 0 && (
          <li className="p-3 text-sm text-muted">لا توجد وثائق مطابقة.</li>
        )}
        {candidates.map((document) => (
          <li key={document.documentId}>
            <button
              type="button"
              className={cn(
                'flex w-full items-center justify-between gap-3 px-3 py-2 text-start transition-colors',
                'hover:bg-surface-muted focus-visible:bg-surface-muted focus-visible:outline-none',
              )}
              onClick={() => onSelect(document)}
            >
              <span className="min-w-0">
                <span className="block truncate text-sm font-medium">{document.title}</span>
                <OfficialNumberTag officialNumberText={document.officialNumberText} />
              </span>
              <DocumentStatusBadge statusId={document.statusId} />
            </button>
          </li>
        ))}
      </ul>
    </div>
  )
}
