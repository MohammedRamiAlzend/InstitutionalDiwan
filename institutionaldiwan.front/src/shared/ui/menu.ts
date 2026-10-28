import type { ReactNode } from 'react'

export interface MenuAction {
  type?: 'item'
  id: string
  label: string
  icon?: ReactNode
  shortcut?: string
  danger?: boolean
  disabled?: boolean
  /** Column chooser toggles stay open so several columns can be changed at once. */
  keepOpen?: boolean
  onSelect: () => void
}

export interface MenuSeparator {
  type: 'separator'
}

export interface MenuLabel {
  type: 'label'
  label: string
}

/** A permission-filtered menu command, separator, or section label. */
export type MenuEntry = MenuAction | MenuSeparator | MenuLabel

export function isMenuAction(entry: MenuEntry): entry is MenuAction {
  return entry.type === undefined || entry.type === 'item'
}

export const menuContentClass =
  'z-50 min-w-52 overflow-hidden rounded-lg border border-border bg-surface p-1 text-foreground shadow-lg animate-content-in'

export const menuItemClass =
  'flex cursor-pointer select-none items-center gap-2 rounded-md px-2 py-1.5 text-sm outline-none data-[disabled]:pointer-events-none data-[disabled]:opacity-50 data-[highlighted]:bg-surface-muted'

export const menuSeparatorClass = 'my-1 h-px bg-border'

export const menuLabelClass = 'px-2 py-1.5 text-xs font-medium text-muted'

/** Radix primitives need the direction explicitly; the document is the source. */
export function documentDirection(): 'rtl' | 'ltr' {
  if (typeof document === 'undefined') return 'rtl'
  return document.documentElement.dir === 'ltr' ? 'ltr' : 'rtl'
}

/** True for `Shift+F10` and the dedicated ContextMenu key. */
export function isContextMenuShortcut(event: { key: string; shiftKey: boolean }): boolean {
  return (event.shiftKey && event.key === 'F10') || event.key === 'ContextMenu'
}

/** Opens the context menu positioned at the element's center (keyboard path). */
export function dispatchContextMenu(element: HTMLElement): void {
  const rect = element.getBoundingClientRect()
  element.dispatchEvent(
    new MouseEvent('contextmenu', {
      bubbles: true,
      cancelable: true,
      clientX: rect.left + rect.width / 2,
      clientY: rect.top + rect.height / 2,
    }),
  )
}
