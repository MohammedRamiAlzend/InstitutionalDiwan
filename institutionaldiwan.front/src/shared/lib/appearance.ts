import { useCallback, useSyncExternalStore } from 'react'

export type AppearanceChoice = 'legacy' | 'modern'

const APPEARANCE_KEY = 'diwan.appearance'
const listeners = new Set<() => void>()

export function getAppearanceChoice(): AppearanceChoice {
  return localStorage.getItem(APPEARANCE_KEY) === 'modern' ? 'modern' : 'legacy'
}

/** Applies the site-wide visual style to `<html data-appearance>`. */
export function applyAppearance(choice: AppearanceChoice): void {
  document.documentElement.dataset.appearance = choice
}

export function setAppearanceChoice(choice: AppearanceChoice): void {
  if (choice === 'legacy') {
    localStorage.removeItem(APPEARANCE_KEY)
  } else {
    localStorage.setItem(APPEARANCE_KEY, choice)
  }
  applyAppearance(choice)
  listeners.forEach((listener) => listener())
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

function getSnapshot(): AppearanceChoice {
  return getAppearanceChoice()
}

export function useAppearanceChoice(): {
  appearance: AppearanceChoice
  setAppearance: (choice: AppearanceChoice) => void
} {
  const appearance = useSyncExternalStore(subscribe, getSnapshot)
  const handleSetAppearance = useCallback((choice: AppearanceChoice) => {
    setAppearanceChoice(choice)
  }, [])

  return { appearance, setAppearance: handleSetAppearance }
}
