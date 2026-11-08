import { installGlobalErrorReporting } from '@/shared/api'
import { ErrorBoundary } from '@/shared/ui'
import { AppProviders } from './providers'
import { AppRouter } from './router'

installGlobalErrorReporting()

export function App() {
  return (
    <ErrorBoundary fullPage>
      <AppProviders>
        <AppRouter />
      </AppProviders>
    </ErrorBoundary>
  )
}
