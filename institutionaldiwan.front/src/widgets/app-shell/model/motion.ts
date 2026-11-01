export type Motion = 'on' | 'off'

const MOTION_KEY = 'diwan.motion'
const REDUCED_MOTION_QUERY = '(prefers-reduced-motion: reduce)'

/**
 * Per-browser motion preference, applied as `<html data-motion>`.
 * Until the user chooses, the system reduced-motion request wins.
 */
export function getStoredMotion(): Motion {
  const stored = localStorage.getItem(MOTION_KEY)
  if (stored === 'on' || stored === 'off') return stored
  return window.matchMedia(REDUCED_MOTION_QUERY).matches ? 'off' : 'on'
}

export function applyMotion(motion: Motion): void {
  document.documentElement.dataset.motion = motion
}

export function persistMotion(motion: Motion): void {
  try {
    localStorage.setItem(MOTION_KEY, motion)
  } catch {
    // Storage can be unavailable; motion is a convenience preference.
  }
}
