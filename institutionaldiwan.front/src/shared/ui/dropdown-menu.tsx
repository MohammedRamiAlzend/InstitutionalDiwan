import * as DropdownMenuPrimitive from '@radix-ui/react-dropdown-menu'
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

interface DropdownMenuProps {
  trigger: ReactNode
  entries: readonly MenuEntry[]
  align?: 'start' | 'center' | 'end'
  disabled?: boolean
  label?: string
  className?: string
}

/**
 * Always-visible overflow menu (touch + keyboard). It renders the same
 * permission-filtered entries as the row context menu.
 */
export function DropdownMenu({
  trigger,
  entries,
  align = 'end',
  disabled,
  label,
  className,
}: DropdownMenuProps) {
  if (entries.length === 0) return <>{trigger}</>

  return (
    <DropdownMenuPrimitive.Root modal={false} dir={documentDirection()}>
      <DropdownMenuPrimitive.Trigger asChild disabled={disabled}>
        {trigger}
      </DropdownMenuPrimitive.Trigger>
      <DropdownMenuPrimitive.Portal>
        <DropdownMenuPrimitive.Content
          align={align}
          sideOffset={4}
          collisionPadding={8}
          className={cn(menuContentClass, className)}
        >
          {label && (
            <DropdownMenuPrimitive.Label className={menuLabelClass}>
              {label}
            </DropdownMenuPrimitive.Label>
          )}
          {entries.map((entry, index) => {
            if (!isMenuAction(entry)) {
              if (entry.type === 'separator') {
                return (
                  <DropdownMenuPrimitive.Separator
                    key={`separator-${index}`}
                    className={menuSeparatorClass}
                  />
                )
              }
              return (
                <DropdownMenuPrimitive.Label
                  key={`label-${index}`}
                  className={menuLabelClass}
                >
                  {entry.label}
                </DropdownMenuPrimitive.Label>
              )
            }

            return (
              <DropdownMenuPrimitive.Item
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
              </DropdownMenuPrimitive.Item>
            )
          })}
        </DropdownMenuPrimitive.Content>
      </DropdownMenuPrimitive.Portal>
    </DropdownMenuPrimitive.Root>
  )
}
