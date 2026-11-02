import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { CircleHelp } from 'lucide-react'
import { getGuideEntry, getPageGuide, type GuideEntry, type PageGuide } from '@/shared/config'
import { cn, formatNumber, useTutorial } from '@/shared/lib'
import { Button } from './button'
import { Dialog } from './dialog'

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
  const [visibleIds, setVisibleIds] = useState<readonly string[]>([])
  const [open, setOpen] = useState(false)
  const [step, setStep] = useState(0)

  const markVisible = useCallback((id: string) => {
    setVisibleIds((previous) => (previous.includes(id) ? previous : [...previous, id]))
    return () => {
      setVisibleIds((previous) => previous.filter((entryId) => entryId !== id))
    }
  }, [])

  // Re-opening starts from the page introduction instead of the last step read.
  const openPageGuide = useCallback(() => {
    setStep(0)
    setOpen(true)
  }, [])

  const value = useMemo(
    () => ({ pageId, markVisible, openPageGuide }),
    [pageId, markVisible, openPageGuide],
  )

  const guide = tutorial === 'on' ? getPageGuide(pageId) : undefined

  return (
    <GuideContext.Provider value={value}>
      {children}
      {guide && (
        <PageGuideDialog
          guide={guide}
          visibleIds={visibleIds}
          open={open}
          onOpenChange={setOpen}
          step={step}
          onStepChange={setStep}
        />
      )}
    </GuideContext.Provider>
  )
}

interface GuideChipProps {
  /** A `GUIDES` entry id; an unknown or missing id renders nothing. */
  id?: string
  className?: string
}

/** Inline «؟» control that opens one entry's explanation. Never placed inside a menu. */
export function GuideChip({ id, className }: GuideChipProps) {
  const tutorial = useTutorial()
  const guide = tutorial === 'on' ? getGuideEntry(id) : undefined
  const context = useContext(GuideContext)
  const [open, setOpen] = useState(false)

  useEffect(() => {
    if (!guide || !context) return
    return context.markVisible(guide.id)
  }, [context, guide])

  if (!guide) return null

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
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
      <GuideEntryDialog guide={guide} open={open} onOpenChange={setOpen} />
    </>
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
  return (
    <div className={cn('relative rounded-lg outline-1 outline-dashed outline-accent/40', className)}>
      {children}
      <GuideChip id={id} className="absolute -end-1.5 -top-1.5 bg-surface" />
    </div>
  )
}

/** Header button that opens the page walkthrough; silent outside a matching provider. */
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

function GuideEntryDialog({
  guide,
  open,
  onOpenChange,
}: {
  guide: GuideEntry
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange} title={guide.title} description={guide.body} size="sm">
      {guide.note && <GuideNote>{guide.note}</GuideNote>}
    </Dialog>
  )
}

function GuideNote({ children }: { children: ReactNode }) {
  return (
    <p className="rounded-lg bg-surface-muted px-3 py-2 text-xs leading-relaxed text-muted">
      {children}
    </p>
  )
}

interface PageGuideDialogProps {
  guide: PageGuide
  visibleIds: readonly string[]
  open: boolean
  onOpenChange: (open: boolean) => void
  step: number
  onStepChange: (step: number) => void
}

/** One entry at a time, so a long page reads as a walkthrough instead of a wall of text. */
function PageGuideDialog({
  guide,
  visibleIds,
  open,
  onOpenChange,
  step,
  onStepChange,
}: PageGuideDialogProps) {
  const steps: readonly GuideEntry[] = [
    { id: guide.pageId, title: 'عن هذه الصفحة', body: guide.intro },
    ...guide.entryIds.flatMap((id) => {
      const entry = getGuideEntry(id)
      // A region the user cannot see right now has nothing to explain about it.
      return entry && visibleIds.includes(entry.id) ? [entry] : []
    }),
  ]

  const index = Math.min(step, steps.length - 1)
  const current = steps[index]
  const isLast = index === steps.length - 1

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={current.title}
      description={current.body}
      size="sm"
      footer={
        <>
          <span className="me-auto text-xs text-muted">
            خطوة <span dir="ltr">{formatNumber(index + 1)}</span> من{' '}
            <span dir="ltr">{formatNumber(steps.length)}</span>
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={index === 0}
            onClick={() => onStepChange(index - 1)}
          >
            السابق
          </Button>
          {isLast ? (
            <Button type="button" size="sm" onClick={() => onOpenChange(false)}>
              إغلاق
            </Button>
          ) : (
            <Button type="button" size="sm" onClick={() => onStepChange(index + 1)}>
              التالي
            </Button>
          )}
        </>
      }
    >
      {current.note && <GuideNote>{current.note}</GuideNote>}
    </Dialog>
  )
}
