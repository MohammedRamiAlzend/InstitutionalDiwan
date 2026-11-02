import * as DialogPrimitive from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '@/shared/lib'

export type DialogSize = 'sm' | 'md' | 'lg'

interface DialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description?: string
  children?: ReactNode
  footer?: ReactNode
  size?: DialogSize
  closeLabel?: string
  className?: string
}

const sizeClasses: Record<DialogSize, string> = {
  sm: 'max-w-sm',
  md: 'max-w-lg',
  lg: 'max-w-2xl',
}

function keepWhileGuideOpen(event: Event) {
  if (document.body.classList.contains('driver-active')) event.preventDefault()
}

/**
 * Centered modal for create/edit/confirm flows; closes on overlay click and Esc.
 * Height is capped at the viewport and only the body scrolls, so the title and
 * footer stay pinned no matter how long the content is.
 */
export function Dialog({
  open,
  onOpenChange,
  title,
  description,
  children,
  footer,
  size = 'md',
  closeLabel = 'إغلاق',
  className,
}: DialogProps) {
  return (
    <DialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-40 bg-overlay animate-overlay-in data-[state=closed]:animate-overlay-out" />
        <DialogPrimitive.Content
          // A guide popover (Driver.js) is mounted on <body>, outside this modal: without
          // this, clicking its buttons or pressing Esc would close the dialog under it.
          onInteractOutside={keepWhileGuideOpen}
          onEscapeKeyDown={keepWhileGuideOpen}
          className={cn(
            'fixed start-1/2 top-1/2 z-50 flex max-h-[calc(100dvh-2rem)] w-[calc(100%-2rem)] -translate-x-1/2 -translate-y-1/2 flex-col overflow-hidden rounded-xl border border-border bg-surface p-6 text-foreground shadow-xl animate-content-in data-[state=closed]:animate-content-out',
            'rtl:translate-x-1/2',
            sizeClasses[size],
            className,
          )}
        >
          <div className="flex shrink-0 flex-col gap-1 pe-8">
            <DialogPrimitive.Title className="text-lg font-semibold">
              {title}
            </DialogPrimitive.Title>
            {description ? (
              <DialogPrimitive.Description className="text-sm text-muted">
                {description}
              </DialogPrimitive.Description>
            ) : (
              <DialogPrimitive.Description className="sr-only">
                {title}
              </DialogPrimitive.Description>
            )}
          </div>

          {children && (
            <div className="mt-4 flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto overscroll-contain pe-1">
              {children}
            </div>
          )}

          {footer && (
            <div className="mt-6 flex shrink-0 flex-wrap items-center justify-end gap-2">
              {footer}
            </div>
          )}

          <DialogPrimitive.Close
            aria-label={closeLabel}
            className="absolute end-4 top-4 rounded-md p-1 text-muted transition-colors hover:bg-surface-muted hover:text-foreground"
          >
            <X className="size-4" aria-hidden />
          </DialogPrimitive.Close>
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  )
}
