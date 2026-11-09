import { useEffect, useRef, useState } from 'react'
import { Hash } from 'lucide-react'
import { Input } from '@/shared/ui'

const COMMIT_DELAY_MS = 300

interface RegisterNumberFilterProps {
  value: string
  onChange: (value: string | null) => void
}

/** Types freely and commits to the URL after a pause, so each keystroke is not a new request. */
export function RegisterNumberFilter({ value, onChange }: RegisterNumberFilterProps) {
  const [text, setText] = useState(value)
  const [committed, setCommitted] = useState(value)
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

  // The value also changes from outside: «مسح المرشحات» or the browser's back button.
  if (value !== committed) {
    setCommitted(value)
    setText(value)
  }

  useEffect(() => () => clearTimeout(timer.current), [])

  return (
    <div className="w-40">
      <Input
        dir="ltr"
        aria-label="تصفية برقم السجل"
        placeholder="رقم السجل"
        startIcon={<Hash className="size-4" aria-hidden />}
        value={text}
        onChange={(event) => {
          const next = event.target.value
          setText(next)
          clearTimeout(timer.current)
          timer.current = setTimeout(() => onChange(next.trim() || null), COMMIT_DELAY_MS)
        }}
      />
    </div>
  )
}
