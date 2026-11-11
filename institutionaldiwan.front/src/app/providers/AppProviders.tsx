import type { ReactNode } from 'react'
import { Toaster } from 'sonner'
import { ThemeProvider } from './ThemeProvider'
import { QueryProvider } from './QueryProvider'
import { AuthSessionLoader } from './AuthSessionLoader'
import { BrandingEffects } from '@/entities/branding'

/** Wraps the application tree in all global providers, in deterministic order. */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider>
      <QueryProvider>
        <BrandingEffects />
        <AuthSessionLoader>{children}</AuthSessionLoader>
        <Toaster dir="rtl" position="top-left" richColors />
      </QueryProvider>
    </ThemeProvider>
  )
}
