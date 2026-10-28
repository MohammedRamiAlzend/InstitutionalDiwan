import { useEffect, useState } from 'react'
import { Search, X } from 'lucide-react'
import { Input } from '@/shared/ui'

interface DepartmentSearchProps {
  /** URL search value owned by `useTableState`. */
  value: string
  onChange: (value: string) => void
  placeholder: string
}

const SEARCH_DEBOUNCE_MS = 300

/** Debounced hierarchy/employee search; URL changes cancel any pending draft commit. */
export function DepartmentSearch({ value, onChange, placeholder }: DepartmentSearchProps) {
  const [draft, setDraft] = useState(value)
  const [previousValue, setPreviousValue] = useState(value)
  // Sync external URL navigation before committing children/effects, without a cascading effect.
  if (previousValue !== value) {
    setPreviousValue(value)
    setDraft(value)
  }

  useEffect(() => {
    const timer = window.setTimeout(() => {
      if (draft.trim() !== value.trim()) onChange(draft)
    }, SEARCH_DEBOUNCE_MS)
    return () => window.clearTimeout(timer)
  }, [draft, onChange, value])

  return (
    <Input
      type="search"
      value={draft}
      onChange={(event) => setDraft(event.target.value)}
      placeholder={placeholder}
      aria-label={placeholder}
      startIcon={<Search className="size-4" aria-hidden />}
      endIcon={
        draft ? (
          <button
            type="button"
            onClick={() => {
              setDraft('')
              onChange('')
            }}
            className="cursor-pointer p-1 hover:text-foreground"
            aria-label="مسح البحث"
          >
            <X className="size-4" aria-hidden />
          </button>
        ) : undefined
      }
      className="h-10 w-full sm:w-72"
    />
  )
}
