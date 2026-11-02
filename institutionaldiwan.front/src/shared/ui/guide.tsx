import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  type ReactNode,
} from 'react'
import { CircleHelp } from 'lucide-react'
import { getGuideEntry, getPageGuide } from '@/shared/config'
import { cn, useTutorial } from '@/shared/lib'
import { Button } from './button'
import { destroyGuideTour, showGuidePopover, showGuideTour } from './guide-tour'

interface GuideContextValue {
  pageId: string
  markVisible: (id: string) => () => void
  openPageGuide: () => void
}

const GuideContext = createContext<GuideContextValue | null>(null)

interface GuideProviderProps {
  /** A `PAGE_GUIDES` id; the page must be wrapped even when it has no guide yet. */
  pageId: string
  children: ReactNode
}

/**
 * Collects which guide entries are on screen right now, so the page walkthrough explains
 * what this user can actually see — a control gated by a missing permission is not in the
 * DOM, so it is not in the guide either.
 */
export function GuideProvider({ pageId, children }: GuideProviderProps) {
  const tutorial = useTutorial()
  // Visibility is read only when the tour starts, so it lives in a ref: keeping it in state
  // would rebuild the context on every chip mount and loop through the chips' effects.
  const visible = useRef(new Map<string, number>())

  const markVisible = useCallback((id: string) => {
    const counts = visible.current
    counts.set(id, (counts.get(id) ?? 0) + 1)
    return () => {
      const remaining = (counts.get(id) ?? 1) - 1
      if (remaining > 0) counts.set(id, remaining)
      else counts.delete(id)
    }
  }, [])

  const guide = tutorial === 'on' ? getPageGuide(pageId) : undefined

  // Every run starts from the page introduction; the steps are the entries on screen now.
  const openPageGuide = useCallback(() => {
    if (!guide) return
    const entries = guide.entryIds.flatMap((id) => {
      const entry = getGuideEntry(id)
      // A region the user cannot see right now has nothing to explain about it.
      return entry && visible.current.has(entry.id) ? [entry] : []
    })
    showGuideTour({ id: guide.pageId, title: 'عن هذه الصفحة', body: guide.intro }, entries)
  }, [guide])

  // A tour must not outlive its page, nor the mode being switched off.
  useEffect(() => destroyGuideTour, [])
  useEffect(() => {
    if (tutorial === 'off') destroyGuideTour()
  }, [tutorial])

  const value = useMemo(
    () => ({ pageId, markVisible, openPageGuide }),
    [pageId, markVisible, openPageGuide],
  )

  return <GuideContext.Provider value={value}>{children}</GuideContext.Provider>
}

interface GuideChipProps {
  /** A `GUIDES` entry id; an unknown or missing id renders nothing. */
  id?: string
  className?: string
}

/** Inline «؟» control that opens one entry's popover. Never placed inside a menu. */
export function GuideChip({ id, className }: GuideChipProps) {
  const tutorial = useTutorial()
  const guide = tutorial === 'on' ? getGuideEntry(id) : undefined
  const context = useContext(GuideContext)

  useEffect(() => {
    if (!guide || !context) return
    return context.markVisible(guide.id)
  }, [context, guide])

  if (!guide) return null

  return (
    <button
      type="button"
      data-guide-chip={guide.id}
      onClick={() => showGuidePopover(guide)}
      aria-label={`اشرح: ${guide.title}`}
      title={`اشرح: ${guide.title}`}
      className={cn(
        'inline-flex size-6 shrink-0 cursor-pointer items-center justify-center rounded-full',
        'text-muted transition-colors hover:bg-surface-muted hover:text-foreground',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus-ring',
        className,
      )}
    >
      <CircleHelp className="size-4" aria-hidden />
    </button>
  )
}

interface GuideRegionProps {
  /** A `GUIDES` entry id for the block, explained by a chip at its corner. */
  id: string
  className?: string
  children: ReactNode
}

/**
 * Declares a page block as explorable. With the mode off it renders its children
 * untouched, so production markup is byte-identical to an unwrapped page.
 */
export function GuideRegion({ id, className, children }: GuideRegionProps) {
  const tutorial = useTutorial()
  const guide = tutorial === 'on' ? getGuideEntry(id) : undefined

  if (!guide) return <>{children}</>

  // `outline` rather than `border`: it marks the explained area without changing layout.
  // The region is what the popover points at; its chip only opens it.
  return (
    <div
      data-guide-region={guide.id}
      className={cn('relative rounded-lg outline-1 outline-dashed outline-accent/40', className)}
    >
      {children}
      <GuideChip id={id} className="absolute -end-1.5 -top-1.5 bg-surface" />
    </div>
  )
}

/** Header button that starts the page tour; silent outside a matching provider. */
export function PageGuideTrigger({ pageId, className }: { pageId: string; className?: string }) {
  const tutorial = useTutorial()
  const context = useContext(GuideContext)
  const hasGuide = tutorial === 'on' && getPageGuide(pageId) !== undefined
  const isMatch = context?.pageId === pageId

  if (!hasGuide || !isMatch || !context) return null

  return (
    <Button
      type="button"
      variant="ghost"
      size="sm"
      className={cn('gap-1.5', className)}
      onClick={context.openPageGuide}
    >
      <CircleHelp className="size-4" aria-hidden />
      شرح هذه الصفحة
    </Button>
  )
}
