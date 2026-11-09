import { useId, useState, type KeyboardEvent } from 'react'
import { UserPlus, UserRound, X } from 'lucide-react'
import { cn, formatDate, useDebouncedValue } from '@/shared/lib'
import { Badge, Input } from '@/shared/ui'
import { normalizePersonName } from '../model/normalizeName'
import { usePersonSuggestions } from '../model/queries'
import type { PersonNameEntry, PersonSuggestionResponse } from '../model/types'

interface PersonNamesInputProps {
  value: PersonNameEntry[]
  onChange: (value: PersonNameEntry[]) => void
  disabled?: boolean
  invalid?: boolean
  placeholder?: string
  id?: string
}

function suggestionDetails(suggestion: PersonSuggestionResponse): string {
  const parts = [`أُضيف ${formatDate(suggestion.createdAt)}`]
  if (suggestion.birthYear !== null) parts.unshift(`مواليد ${suggestion.birthYear}`)
  if (suggestion.nationalNumberLast4 !== null) parts.unshift(`رقم وطني ينتهي بـ ${suggestion.nationalNumberLast4}`)
  return parts.join(' · ')
}

/**
 * Collects any number of people by full name. Typing offers existing people; a name not picked from
 * the list is added as a new person, because two people can share a name. Text left in the box counts
 * as entered when focus moves on, so a name is not lost by going straight to the save button.
 */
export function PersonNamesInput({
  value,
  onChange,
  disabled = false,
  invalid = false,
  placeholder = 'اكتب الاسم الكامل ثم اضغط Enter…',
  id,
}: PersonNamesInputProps) {
  const listId = useId()
  const [text, setText] = useState('')
  const [open, setOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(0)
  const [duplicateName, setDuplicateName] = useState<string | null>(null)

  const debouncedText = useDebouncedValue(text)
  const suggestionsQuery = usePersonSuggestions(debouncedText)
  const chosenIds = new Set(value.map((entry) => entry.personId))
  const suggestions = (suggestionsQuery.data ?? []).filter(
    (suggestion) => !chosenIds.has(suggestion.personId),
  )

  const typedName = text.trim()
  const newOptionIndex = suggestions.length
  const showList = open && typedName.length > 0
  const optionCount = suggestions.length + 1

  const reset = () => {
    setText('')
    setActiveIndex(0)
    setOpen(false)
  }

  const addNew = () => {
    if (!typedName) return
    const normalized = normalizePersonName(typedName)
    if (value.some((entry) => entry.personId === null && normalizePersonName(entry.fullName) === normalized)) {
      setDuplicateName(typedName)
      reset()
      return
    }
    setDuplicateName(null)
    onChange([...value, { personId: null, fullName: typedName }])
    reset()
  }

  const addExisting = (suggestion: PersonSuggestionResponse) => {
    setDuplicateName(null)
    onChange([...value, { personId: suggestion.personId, fullName: suggestion.displayName }])
    reset()
  }

  const commitActive = () => {
    const suggestion = suggestions[activeIndex]
    if (suggestion) addExisting(suggestion)
    else addNew()
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      // Enter belongs to this field; it must not submit the surrounding form.
      event.preventDefault()
      commitActive()
    } else if (event.key === 'ArrowDown' && showList) {
      event.preventDefault()
      setActiveIndex((index) => (index + 1) % optionCount)
    } else if (event.key === 'ArrowUp' && showList) {
      event.preventDefault()
      setActiveIndex((index) => (index - 1 + optionCount) % optionCount)
    } else if (event.key === 'Escape' && showList) {
      // Only the list closes; the dialog around this field stays open.
      event.preventDefault()
      event.stopPropagation()
      setOpen(false)
    }
  }

  const optionClass = (active: boolean) =>
    cn(
      'flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-start text-sm',
      active ? 'bg-accent/10 text-foreground' : 'text-foreground hover:bg-surface-muted',
    )

  return (
    <div className="flex flex-col gap-2">
      <div className="relative">
        <Input
          id={id}
          role="combobox"
          aria-expanded={showList}
          aria-controls={listId}
          aria-autocomplete="list"
          autoComplete="off"
          value={text}
          placeholder={placeholder}
          disabled={disabled}
          invalid={invalid}
          startIcon={<UserRound className="size-4" aria-hidden />}
          onChange={(event) => {
            setText(event.target.value)
            setActiveIndex(0)
            setOpen(true)
            setDuplicateName(null)
          }}
          onKeyDown={handleKeyDown}
          onBlur={() => {
            if (typedName) addNew()
            setOpen(false)
          }}
        />

        {showList && (
          <ul
            id={listId}
            role="listbox"
            aria-label="الأشخاص المطابقون"
            className="absolute inset-x-0 top-full z-20 mt-1 flex max-h-64 flex-col gap-0.5 overflow-y-auto rounded-lg border border-border bg-surface p-1 shadow-lg"
          >
            {suggestions.map((suggestion, index) => (
              <li key={suggestion.personId} role="option" aria-selected={index === activeIndex}>
                <button
                  type="button"
                  tabIndex={-1}
                  // Keeps focus in the input so its blur handler does not add the typed text too.
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => addExisting(suggestion)}
                  className={optionClass(index === activeIndex)}
                >
                  <UserRound className="size-4 shrink-0 text-muted" aria-hidden />
                  <span className="flex min-w-0 flex-col">
                    <span className="truncate font-medium">{suggestion.displayName}</span>
                    <span className="truncate text-xs text-muted">{suggestionDetails(suggestion)}</span>
                  </span>
                </button>
              </li>
            ))}
            <li role="option" aria-selected={activeIndex === newOptionIndex}>
              <button
                type="button"
                tabIndex={-1}
                onMouseDown={(event) => event.preventDefault()}
                onClick={addNew}
                className={optionClass(activeIndex === newOptionIndex)}
              >
                <UserPlus className="size-4 shrink-0 text-accent" aria-hidden />
                <span className="truncate">إضافة «{typedName}» كشخص جديد</span>
              </button>
            </li>
          </ul>
        )}
      </div>

      {duplicateName && (
        <p role="alert" className="text-xs text-danger">
          الاسم «{duplicateName}» مضاف مسبقًا إلى القائمة.
        </p>
      )}

      {/* Room is held while typing: leaving the field adds a chip, and if that moved the form the
          click on its save button, already under way, would land on nothing. */}
      {(value.length > 0 || typedName.length > 0) && (
        <ul className="flex min-h-8 flex-wrap gap-2" aria-label="الأشخاص المضافون">
          {value.map((entry, index) => (
            <li
              key={entry.personId ?? `new-${entry.fullName}`}
              className="flex items-center gap-1.5 rounded-full border border-border bg-surface-muted/40 py-1 pe-1 ps-3 text-sm text-foreground"
            >
              <span>{entry.fullName}</span>
              {entry.personId === null && <Badge variant="outline">جديد</Badge>}
              <button
                type="button"
                disabled={disabled}
                aria-label={`إزالة ${entry.fullName}`}
                onClick={() => onChange(value.filter((_, position) => position !== index))}
                className="flex size-6 items-center justify-center rounded-full text-muted transition-colors hover:bg-surface-muted hover:text-foreground"
              >
                <X className="size-3.5" aria-hidden />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
