import { useSyncExternalStore } from 'react'

export type Tutorial = 'on' | 'off'

const TUTORIAL_KEY = 'diwan.tutorial'

/**
 * Per-browser tutorial mode, applied as `<html data-tutorial>`. Unlike motion it has
 * no system signal to follow: it stays off until the user turns it on.
 */
export function getTutorialChoice(): Tutorial {
  return localStorage.getItem(TUTORIAL_KEY) === 'on' ? 'on' : 'off'
}

/** Applies the choice to `<html data-tutorial>`; mirrors the boot script in index.html. */
export function applyTutorial(choice: Tutorial): void {
  document.documentElement.dataset.tutorial = choice
}

const listeners = new Set<() => void>()

export function setTutorialChoice(choice: Tutorial): void {
  try {
    // Only «on» is stored: an absent key is the off default, so switching back leaves
    // nothing behind and a future system signal still wins.
    if (choice === 'on') localStorage.setItem(TUTORIAL_KEY, choice)
    else localStorage.removeItem(TUTORIAL_KEY)
  } catch {
    // Storage can be unavailable; tutorial mode is a convenience preference.
  }
  applyTutorial(choice)
  listeners.forEach((listener) => listener())
}

function subscribe(onStoreChange: () => void): () => void {
  listeners.add(onStoreChange)
  return () => {
    listeners.delete(onStoreChange)
  }
}

/**
 * Effective tutorial mode. It lives in `shared/lib`, not in the app shell, because the
 * grid and form primitives below `widgets` read it too.
 */
export function useTutorial(): Tutorial {
  return useSyncExternalStore(subscribe, getTutorialChoice)
}
