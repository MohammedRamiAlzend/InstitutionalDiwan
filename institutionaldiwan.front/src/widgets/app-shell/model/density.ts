export type Density = 'comfortable' | 'compact'

const DENSITY_KEY = 'diwan.density'

/** Per-browser display preference, applied as `<html data-density>`. */
export function getStoredDensity(): Density {
  return localStorage.getItem(DENSITY_KEY) === 'compact' ? 'compact' : 'comfortable'
}

export function applyDensity(density: Density): void {
  document.documentElement.dataset.density = density
}

export function persistDensity(density: Density): void {
  try {
    localStorage.setItem(DENSITY_KEY, density)
  } catch {
    // Storage can be unavailable; density is a convenience preference.
  }
}
