import { useEffect, type ReactNode } from 'react'
import { applyTheme, getThemeChoice } from '@/shared/lib'

/** Applies `data-theme` on `<html>` and keeps it in sync with the stored choice. */
export function ThemeProvider({ children }: { children: ReactNode }) {
  useEffect(() => {
    applyTheme(getThemeChoice())
  }, [])

  return <>{children}</>
}
