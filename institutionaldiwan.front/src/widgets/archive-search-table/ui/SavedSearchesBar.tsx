import { useState } from 'react'
import { Pencil, Star, Trash2 } from 'lucide-react'
import { useSavedSearches, type SavedSearchResponse } from '@/entities/archive'
import { DeleteSavedSearchDialog, SaveSearchDialog } from '@/features/archive-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { cn } from '@/shared/lib'
import { Button, GuideChip } from '@/shared/ui'

interface SavedSearchesBarProps {
  /** Criteria JSON of the grid as it is now; "update" re-captures it under an existing name. */
  currentCriteriaJson: string
  activeSearchId: string | null
  onApply: (search: SavedSearchResponse) => void
}

/**
 * The caller's personal saved searches as a row of chips. Applying one only rewrites the grid's URL
 * params — the server stores the criteria but never runs them.
 */
export function SavedSearchesBar({
  currentCriteriaJson,
  activeSearchId,
  onApply,
}: SavedSearchesBarProps) {
  const savedQuery = useSavedSearches()
  const [editing, setEditing] = useState<SavedSearchResponse | null>(null)
  const [deleting, setDeleting] = useState<SavedSearchResponse | null>(null)

  if (savedQuery.isError) {
    return (
      <p role="alert" className="flex items-center gap-2 text-xs text-danger">
        {toArabicErrorMessage(savedQuery.error)}
        <Button type="button" variant="outline" size="sm" onClick={() => void savedQuery.refetch()}>
          إعادة التحميل
        </Button>
      </p>
    )
  }

  const searches = savedQuery.data ?? []
  if (savedQuery.isLoading || searches.length === 0) return null

  return (
    <div className="flex flex-wrap items-center gap-2" aria-label="عمليات البحث المحفوظة">
      <GuideChip id="archive.saved-searches" />
      <span className="text-xs text-muted">عمليات بحثي:</span>
      {searches.map((search) => {
        const isActive = search.savedSearchId === activeSearchId

        return (
          <span
            key={search.savedSearchId}
            className={cn(
              'inline-flex items-center rounded-full border text-sm',
              isActive ? 'border-accent bg-accent/10' : 'border-border bg-surface',
            )}
          >
            <button
              type="button"
              onClick={() => onApply(search)}
              aria-pressed={isActive}
              className="inline-flex items-center gap-1.5 rounded-full py-1 ps-3 pe-2 text-foreground hover:text-accent"
            >
              {search.isDefault && (
                <Star className="size-3.5 text-warning" aria-label="البحث الافتراضي" />
              )}
              {search.name}
            </button>
            <button
              type="button"
              onClick={() => setEditing(search)}
              aria-label={`تعديل البحث «${search.name}»`}
              title="تعديل الاسم أو استبدال المرشحات بالحالية"
              className="p-1 text-muted hover:text-foreground"
            >
              <Pencil className="size-3.5" aria-hidden />
            </button>
            <button
              type="button"
              onClick={() => setDeleting(search)}
              aria-label={`حذف البحث «${search.name}»`}
              className="rounded-full p-1 pe-2 text-muted hover:text-danger"
            >
              <Trash2 className="size-3.5" aria-hidden />
            </button>
          </span>
        )
      })}

      {editing && (
        <SaveSearchDialog
          existing={editing}
          criteriaJson={currentCriteriaJson}
          open
          onOpenChange={(open) => !open && setEditing(null)}
        />
      )}
      {deleting && (
        <DeleteSavedSearchDialog
          search={deleting}
          open
          onOpenChange={(open) => !open && setDeleting(null)}
        />
      )}
    </div>
  )
}
