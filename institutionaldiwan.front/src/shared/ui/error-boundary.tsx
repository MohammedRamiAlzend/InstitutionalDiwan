import { Component, type ErrorInfo, type ReactNode } from 'react'
import { AlertTriangle, Home, RefreshCw } from 'lucide-react'
import { CLIENT_ERROR_KIND, reportClientError } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { cn } from '@/shared/lib'
import { Button } from './button'

interface ErrorBoundaryProps {
  children: ReactNode
  /** A new value clears the error, so moving to another page or filter recovers on its own. */
  resetKey?: string
  /** Fills the window; for the outermost boundary, where no application frame is left around it. */
  fullPage?: boolean
}

interface ErrorBoundaryState {
  failed: boolean
  error?: unknown
  componentStack?: string
}

function describeError(error: unknown): string {
  return error instanceof Error ? (error.stack ?? `${error.name}: ${error.message}`) : String(error)
}

/**
 * Shows an Arabic error screen instead of a blank page when rendering throws. React has no hook for
 * this, so it is the one class component here. Developers also see the technical error on
 * `npm run dev`; a production build never renders it, so users are not shown code details.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { failed: false }

  static getDerivedStateFromError(error: unknown): ErrorBoundaryState {
    return { failed: true, error }
  }

  componentDidCatch(error: unknown, info: ErrorInfo) {
    reportClientError({
      kind: CLIENT_ERROR_KIND.Render,
      message: error instanceof Error ? error.message : String(error),
      details: `${describeError(error)}${info.componentStack ?? ''}`,
    })
    if (import.meta.env.DEV) this.setState({ componentStack: info.componentStack ?? undefined })
  }

  componentDidUpdate(previous: ErrorBoundaryProps) {
    if (this.state.failed && previous.resetKey !== this.props.resetKey) {
      this.setState({ failed: false, error: undefined, componentStack: undefined })
    }
  }

  render() {
    if (!this.state.failed) return this.props.children

    return (
      <div
        role="alert"
        className={cn(
          'flex flex-col items-center justify-center gap-4 px-4 py-16 text-center',
          this.props.fullPage && 'min-h-screen bg-background text-foreground',
        )}
      >
        <AlertTriangle className="size-10 text-danger" aria-hidden />
        <div className="flex flex-col gap-1">
          <h1 className="text-lg font-semibold text-foreground">تعذّر عرض هذه الصفحة</h1>
          <p className="max-w-md text-sm text-muted">
            حدث خطأ غير متوقع أثناء عرضها. البيانات المحفوظة لم تتأثر، أما ما لم يُحفظ في هذه الصفحة
            فقد يلزم إدخاله مجددًا.
          </p>
        </div>
        <div className="flex flex-wrap items-center justify-center gap-2">
          <Button type="button" onClick={() => window.location.reload()}>
            <RefreshCw className="size-4" aria-hidden />
            إعادة تحميل الصفحة
          </Button>
          <Button type="button" variant="outline" onClick={() => window.location.assign(ROUTES.DASHBOARD)}>
            <Home className="size-4" aria-hidden />
            الذهاب إلى لوحة التحكم
          </Button>
        </div>
        {import.meta.env.DEV && (
          <pre
            dir="ltr"
            className="max-h-96 w-full max-w-4xl overflow-auto rounded-lg border border-border bg-surface-muted p-3 text-start text-xs text-danger"
          >
            {describeError(this.state.error)}
            {this.state.componentStack}
          </pre>
        )}
      </div>
    )
  }
}
