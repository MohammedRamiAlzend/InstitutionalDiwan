import * as ContextMenuPrimitive from '@radix-ui/react-context-menu'
import type { ReactNode } from 'react'
import { cn } from '@/shared/lib'
import {
  documentDirection,
  isMenuAction,
  menuContentClass,
  menuItemClass,
  menuLabelClass,
  menuSeparatorClass,
  type MenuEntry,
} from './menu'

interface ContextMenuProps {
  /** Permission-filtered commands supplied by the owning widget or feature. */
  entries: readonly MenuEntry[]
  children: ReactNode
  disabled?: boolean
  className?: string
}

/**
 * The single right-click primitive. The caller owns the items; this wrapper
 * handles positioning, RTL, focus return, and keyboard access (Shift+F10 /
 * ContextMenu key dispatch a `contextmenu` event on the focused trigger).
 */
export function ContextMenu({ entries, children, disabled, className }: ContextMenuProps) {
  if (entries.length === 0) return <>{children}</>

  return (
    <ContextMenuPrimitive.Root modal={false} dir={documentDirection()}>
      <ContextMenuPrimitive.Trigger asChild disabled={disabled}>
        {children}
      </ContextMenuPrimitive.Trigger>
      <ContextMenuPrimitive.Portal>
        <ContextMenuPrimitive.Content
          collisionPadding={8}
          className={cn(menuContentClass, className)}
        >
          {entries.map((entry, index) => {
            if (!isMenuAction(entry)) {
              if (entry.type === 'separator') {
                return (
                  <ContextMenuPrimitive.Separator
                    key={`separator-${index}`}
                    className={menuSeparatorClass}
                  />
                )
              }
              return (
                <ContextMenuPrimitive.Label
                  key={`label-${index}`}
                  className={menuLabelClass}
                >
                  {entry.label}
                </ContextMenuPrimitive.Label>
              )
            }

            return (
              <ContextMenuPrimitive.Item
                key={entry.id}
                disabled={entry.disabled}
                className={cn(menuItemClass, entry.danger && 'text-danger')}
                onSelect={(event) => {
                  if (entry.keepOpen) event.preventDefault()
                  entry.onSelect()
                }}
              >
                {entry.icon && (
                  <span className="flex size-4 shrink-0 items-center justify-center">
                    {entry.icon}
                  </span>
                )}
                <span className="flex-1 text-start">{entry.label}</span>
                {entry.shortcut && (
                  <span dir="ltr" className="text-xs text-muted">
                    {entry.shortcut}
                  </span>
                )}
              </ContextMenuPrimitive.Item>
            )
          })}
        </ContextMenuPrimitive.Content>
      </ContextMenuPrimitive.Portal>
    </ContextMenuPrimitive.Root>
  )
}
