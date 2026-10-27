import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react'
import { cn } from '@/shared/lib'

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  /** Icon rendered at the inline-start edge (mirrors automatically in RTL). */
  startIcon?: ReactNode
  /** Icon rendered at the inline-end edge (e.g. the password visibility toggle). */
  endIcon?: ReactNode
  invalid?: boolean
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, startIcon, endIcon, invalid, ...props }, ref) => (
    <div className="relative flex items-center">
      {startIcon && (
        <span className="pointer-events-none absolute start-3 text-muted">
          {startIcon}
        </span>
      )}
      <input
        ref={ref}
        className={cn(
          'h-12 w-full rounded-lg border bg-surface ps-10 pe-10 text-sm text-foreground placeholder:text-muted transition-colors',
          'focus:border-focus-ring focus:outline-none',
          invalid ? 'border-danger' : 'border-border',
          !startIcon && 'ps-4',
          !endIcon && 'pe-4',
          className,
        )}
        {...props}
      />
      {endIcon && (
        <span className="absolute end-3 flex items-center text-muted">
          {endIcon}
        </span>
      )}
    </div>
  ),
)
Input.displayName = 'Input'
