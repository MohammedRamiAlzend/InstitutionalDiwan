import { driver, type Config, type DriveStep, type Driver } from 'driver.js'
import type { GuideEntry } from '@/shared/config'
import { formatNumber } from '@/shared/lib'

/**
 * Driver.js adapter for the tutorial mode. A guide entry is anchored to whichever element
 * declares it: the `GuideRegion` wrapper when there is one, otherwise the entry's own chip.
 */
export function guideSelector(id: string): string {
  return `[data-guide-region="${id}"], [data-guide-chip="${id}"]`
}

let active: Driver | undefined

/** Closes the running tour or popover; safe to call when none is open. */
export function destroyGuideTour(): void {
  active?.destroy()
  active = undefined
}

function escapeHtml(text: string): string {
  return text
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
}

function popoverOf(entry: GuideEntry): NonNullable<DriveStep['popover']> {
  const note = entry.note ? `<p class="diwan-tour-note">${escapeHtml(entry.note)}</p>` : ''
  return {
    title: escapeHtml(entry.title),
    description: `<p>${escapeHtml(entry.body)}</p>${note}`,
  }
}

function overlayColor(): string {
  // SVG `fill` takes a resolved color, not var(); read the brand charcoal from the tokens.
  return getComputedStyle(document.documentElement).getPropertyValue('--charcoal-900').trim() || 'black'
}

// Animation is part of the tour's feel: `animate` and `smoothScroll` stay on whatever the
// display-motion preference says (the stylesheet exempts `.driver-popover` from it too).
function baseConfig(): Config {
  return {
    animate: true,
    smoothScroll: true,
    allowClose: true,
    stagePadding: 6,
    stageRadius: 10,
    overlayColor: overlayColor(),
    overlayOpacity: 0.55,
    popoverClass: 'diwan-tour',
    nextBtnText: 'التالي',
    prevBtnText: 'السابق',
    doneBtnText: 'إغلاق',
    onDestroyed: () => {
      active = undefined
    },
  }
}

/** One entry as a popover attached to its region or chip. */
export function showGuidePopover(entry: GuideEntry): void {
  destroyGuideTour()
  const element = document.querySelector(guideSelector(entry.id)) ?? undefined
  active = driver(baseConfig())
  active.highlight({ element, popover: { ...popoverOf(entry), showButtons: ['close'] } })
}

/**
 * The page walkthrough: the introduction centered, then each visible entry in order.
 * An entry whose element is not in the DOM right now is left out instead of floating.
 */
export function showGuideTour(intro: GuideEntry, entries: readonly GuideEntry[]): void {
  destroyGuideTour()
  const steps: DriveStep[] = [{ popover: popoverOf(intro) }]
  for (const entry of entries) {
    const element = document.querySelector(guideSelector(entry.id))
    if (element) steps.push({ element, popover: popoverOf(entry) })
  }
  active = driver({
    ...baseConfig(),
    showProgress: true,
    progressText: '{{current}} من {{total}}',
    steps,
    onPopoverRender: (popover) => {
      // Arabic-Indic digits, like the rest of the interface.
      const progress = popover.progress
      progress.textContent = (progress.textContent ?? '').replace(/\d+/g, (digits) =>
        formatNumber(Number(digits)),
      )
    },
  })
  active.drive()
}
