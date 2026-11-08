import { ErrorBoundary } from '@/shared/ui'
import { AppProviders } from './providers'
import { AppRouter } from './router'

export function App() {
  return (
    <ErrorBoundary fullPage>
      <AppProviders>
        <AppRouter />
      </AppProviders>
    </ErrorBoundary>
  )
}
