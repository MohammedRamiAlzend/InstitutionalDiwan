import { useEffect, useRef, useState, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { useIsFetching } from '@tanstack/react-query'

/** Labels resolve in the sheet's first moments; a request after this long must not block the print. */
const MAX_WAIT_MS = 8000
/** A label that just resolved can start another request; give it one frame's worth of grace. */
const SETTLE_MS = 250

interface PrintSheetProps {
  /** Becomes the file name the browser suggests when saving as PDF. */
  fileName: string
  orientation?: 'portrait' | 'landscape'
  /** False while the sheet's own data is still loading. */
  ready?: boolean
  /** Runs once the print dialog closes; the caller unmounts the sheet here. */
  onDone: () => void
  children: ReactNode
}

/**
 * Opens the browser's print dialog ("Save as PDF") for `children` alone. Printing through the
 * browser is what shapes Arabic correctly and needs no embedded font. The sheet is invisible on
 * screen and waits for every pending request, so names and lookups are resolved before it prints.
 */
export function PrintSheet({
  fileName,
  orientation = 'portrait',
  ready = true,
  onDone,
  children,
}: PrintSheetProps) {
  const fetching = useIsFetching()
  const [timedOut, setTimedOut] = useState(false)
  const printed = useRef(false)
  const onDoneRef = useRef(onDone)

  useEffect(() => {
    onDoneRef.current = onDone
  }, [onDone])

  useEffect(() => {
    const previousTitle = document.title
    document.title = fileName
    document.body.classList.add('printing')

    const pageStyle = document.createElement('style')
    pageStyle.textContent = `@page { size: A4 ${orientation}; margin: 14mm 12mm; @bottom-center { content: counter(page) " / " counter(pages); font-size: 9pt; } }`
    document.head.appendChild(pageStyle)

    const handleAfterPrint = () => onDoneRef.current()
    window.addEventListener('afterprint', handleAfterPrint)
    const deadline = window.setTimeout(() => setTimedOut(true), MAX_WAIT_MS)

    return () => {
      window.clearTimeout(deadline)
      window.removeEventListener('afterprint', handleAfterPrint)
      pageStyle.remove()
      document.body.classList.remove('printing')
      document.title = previousTitle
    }
  }, [fileName, orientation])

  useEffect(() => {
    if (printed.current || !(timedOut || (ready && fetching === 0))) return
    const timer = window.setTimeout(() => {
      printed.current = true
      window.print()
    }, SETTLE_MS)
    return () => window.clearTimeout(timer)
  }, [fetching, ready, timedOut])

  return createPortal(
    <div className="print-sheet" dir="rtl" lang="ar">
      {children}
    </div>,
    document.body,
  )
}
