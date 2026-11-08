import { Component, type ReactNode } from 'react'
import { AlertTriangle, Home, RefreshCw } from 'lucide-react'
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
}

/**
 * Shows an Arabic error screen instead of a blank page when rendering throws. React has no hook for
 * this, so it is the one class component here. The error itself stays in the browser console; its
 * text is English and technical, so it is not shown.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { failed: false }

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { failed: true }
  }

  componentDidUpdate(previous: ErrorBoundaryProps) {
    if (this.state.failed && previous.resetKey !== this.props.resetKey) {
      this.setState({ failed: false })
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
      </div>
    )
  }
}
