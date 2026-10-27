import { useCallback, useSyncExternalStore } from 'react'

export type ThemeChoice = 'light' | 'dark' | 'system'

const THEME_KEY = 'diwan.theme'
const MEDIA_QUERY = '(prefers-color-scheme: dark)'

export function getThemeChoice(): ThemeChoice {
  const stored = localStorage.getItem(THEME_KEY)
  return stored === 'light' || stored === 'dark' ? stored : 'system'
}

function resolveTheme(choice: ThemeChoice): 'light' | 'dark' {
  if (choice === 'system') {
    return window.matchMedia(MEDIA_QUERY).matches ? 'dark' : 'light'
  }
  return choice
}

/** Applies the resolved theme to `<html data-theme>`; mirrors the boot script in index.html. */
export function applyTheme(choice: ThemeChoice): void {
  document.documentElement.dataset.theme = resolveTheme(choice)
}

const listeners = new Set<() => void>()

function emit(): void {
  listeners.forEach((listener) => listener())
}

export function setThemeChoice(choice: ThemeChoice): void {
  if (choice === 'system') {
    localStorage.removeItem(THEME_KEY)
  } else {
    localStorage.setItem(THEME_KEY, choice)
  }
  applyTheme(choice)
  emit()
}

function subscribe(onStoreChange: () => void): () => void {
  const media = window.matchMedia(MEDIA_QUERY)
  listeners.add(onStoreChange)
  media.addEventListener('change', onStoreChange)
  return () => {
    listeners.delete(onStoreChange)
    media.removeEventListener('change', onStoreChange)
  }
}

function getSnapshot(): 'light' | 'dark' {
  return resolveTheme(getThemeChoice())
}

/** Effective theme (`light` | `dark`); follows `prefers-color-scheme` while the choice is `system`. */
export function useTheme(): 'light' | 'dark' {
  return useSyncExternalStore(subscribe, getSnapshot)
}

export function useThemeChoice(): {
  theme: 'light' | 'dark'
  choice: ThemeChoice
  setChoice: (choice: ThemeChoice) => void
  toggle: () => void
} {
  const theme = useTheme()

  const handleSetChoice = useCallback((choice: ThemeChoice) => {
    setThemeChoice(choice)
  }, [])

  const toggle = useCallback(() => {
    setThemeChoice(theme === 'dark' ? 'light' : 'dark')
  }, [theme])

  return { theme, choice: getThemeChoice(), setChoice: handleSetChoice, toggle }
}
