import { forwardRef, type TextareaHTMLAttributes } from 'react'
import { cn } from '@/shared/lib'

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  invalid?: boolean
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, invalid, rows = 4, ...props }, ref) => (
    <textarea
      ref={ref}
      rows={rows}
      className={cn(
        'min-h-24 w-full resize-y rounded-lg border bg-surface px-4 py-3 text-sm text-foreground placeholder:text-muted transition-colors',
        'focus:border-focus-ring focus:outline-none',
        invalid ? 'border-danger' : 'border-border',
        className,
      )}
      {...props}
    />
  ),
)
Textarea.displayName = 'Textarea'
