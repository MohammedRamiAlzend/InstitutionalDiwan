import { useEffect, useRef, useState } from 'react'
import { Search, X } from 'lucide-react'
import { useDebouncedValue } from '@/shared/lib'
import { Input } from '@/shared/ui'

interface DepartmentSearchProps {
  /** URL search value owned by `useTableState`. */
  value: string
  onChange: (value: string) => void
  placeholder: string
}

const SEARCH_DEBOUNCE_MS = 300

/** Debounced tree/mermaid search; the committed term stays in the URL. */
export function DepartmentSearch({ value, onChange, placeholder }: DepartmentSearchProps) {
  const [draft, setDraft] = useState(value)
  const debounced = useDebouncedValue(draft, SEARCH_DEBOUNCE_MS)
  const lastEmitted = useRef<string | undefined>(undefined)

  useEffect(() => {
    const next = debounced.trim()
    if (next === value.trim()) return
    lastEmitted.current = next
    onChange(debounced)
  }, [debounced, onChange, value])

  useEffect(() => {
    if (lastEmitted.current !== undefined && value === lastEmitted.current) {
      lastEmitted.current = undefined
      return
    }
    setDraft(value)
  }, [value])

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
