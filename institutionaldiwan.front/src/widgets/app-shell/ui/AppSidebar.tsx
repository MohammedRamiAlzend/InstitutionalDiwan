import { useMemo, useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import {
  ArrowUpRight,
  ChevronDown,
  Link2,
  PanelLeftClose,
  PanelLeftOpen,
} from 'lucide-react'
import { toast } from 'sonner'
import { BrandMark, BrandName } from '@/entities/branding'
import { useSessionStore } from '@/entities/user'
import type { LabelKey } from '@/shared/config'
import { cn, resolveLabel, t, useLabelsStore } from '@/shared/lib'
import {
  Button,
  ContextMenu,
  Label,
  dispatchContextMenu,
  isContextMenuShortcut,
  type MenuEntry,
} from '@/shared/ui'
import { filterNavGroups, type ShellNavItem } from '../model/shell-nav'
import { DocumentsRegisterNav } from './DocumentsRegisterNav'

interface AppSidebarProps {
  collapsed?: boolean
  onToggleCollapsed?: () => void
  /** Closes the mobile drawer after navigation. */
  onNavigate?: () => void
}

async function copyLink(path: string): Promise<void> {
  try {
    await navigator.clipboard.writeText(new URL(path, window.location.origin).href)
    toast.success(t('common.link-copied'))
  } catch {
    toast.error(t('common.link-copy-failed'))
  }
}

/** Permission-filtered navigation with collapsible groups and item context menus. */
export function AppSidebar({
  collapsed = false,
  onToggleCollapsed,
  onNavigate,
}: AppSidebarProps) {
  const navigate = useNavigate()
  const permissions = useSessionStore((state) => state.user?.permissions)
  const groups = useMemo(() => filterNavGroups(permissions ?? []), [permissions])
  const [closedGroups, setClosedGroups] = useState<string[]>([])

  // Subscribe to the labels store so renames repaint both the visible text and the
  // string props (aria-label/title/menu entries) in expanded and collapsed modes alike.
  const overrides = useLabelsStore((state) => state.overrides)
  const label = (key: LabelKey) => resolveLabel(key, overrides)

  const isGroupOpen = (groupId: string) =>
    collapsed || !closedGroups.includes(groupId)

  const toggleGroup = (groupId: string) => {
    setClosedGroups((previous) =>
      previous.includes(groupId)
        ? previous.filter((id) => id !== groupId)
        : [...previous, groupId],
    )
  }

  const itemEntries = (
    item: ShellNavItem,
    groupId: string,
    groupOpen: boolean,
  ): MenuEntry[] => {
    const entries: MenuEntry[] = [
      {
        id: `${item.id}-open`,
        label: label('common.open'),
        icon: <ArrowUpRight className="size-4 rtl:-scale-x-100" aria-hidden />,
        disabled: !item.available,
        onSelect: () => {
          onNavigate?.()
          navigate(item.to)
        },
      },
      {
        id: `${item.id}-copy`,
        label: label('common.copy-link'),
        icon: <Link2 className="size-4" aria-hidden />,
        disabled: !item.available,
        onSelect: () => {
          void copyLink(item.to)
        },
      },
    ]

    if (!collapsed) {
      entries.push(
        { type: 'separator' },
        {
          id: `${item.id}-toggle-group`,
          label: groupOpen ? label('common.collapse-group') : label('common.expand-group'),
          icon: <ChevronDown className="size-4" aria-hidden />,
          onSelect: () => toggleGroup(groupId),
        },
      )
    }

    return entries
  }

  return (
    <aside
      className={cn(
        'flex h-full flex-col border-e border-border bg-surface transition-[width] duration-200',
        collapsed ? 'w-[4.75rem]' : 'w-72',
      )}
    >
      <div
        className={cn(
          'flex items-center gap-3 border-b border-border px-3',
          collapsed ? 'flex-col justify-center py-3' : 'h-16',
        )}
      >
        <BrandMark />
        {!collapsed && (
          <div className="min-w-0 flex-1 leading-tight">
            <p className="truncate font-heading text-sm font-bold text-foreground">
              <BrandName />
            </p>
            <p className="truncate text-[11px] text-muted">
              <Label k="nav.tagline" />
            </p>
          </div>
        )}
        {onToggleCollapsed && (
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className={cn('size-8 shrink-0', !collapsed && 'ms-auto')}
            onClick={onToggleCollapsed}
            aria-label={collapsed ? label('nav.expand-menu') : label('nav.collapse-menu')}
            title={collapsed ? label('nav.expand-menu') : label('nav.collapse-menu')}
          >
            {collapsed ? (
              <PanelLeftOpen className="size-4 rtl:-scale-x-100" aria-hidden />
            ) : (
              <PanelLeftClose className="size-4 rtl:-scale-x-100" aria-hidden />
            )}
          </Button>
        )}
      </div>

      <nav aria-label={label('nav.main-menu')} className="flex-1 overflow-y-auto px-2 py-3">
        {groups.map((group) => {
          const open = isGroupOpen(group.id)
          return (
            <div key={group.id} className="mb-3">
              {!collapsed && (
                <button
                  type="button"
                  onClick={() => toggleGroup(group.id)}
                  aria-expanded={open}
                  className="flex w-full cursor-pointer items-center justify-between rounded-md px-2 py-1.5 text-[11px] font-semibold text-muted transition-colors hover:text-foreground"
                >
                  <span>
                    <Label k={group.labelKey} />
                  </span>
                  <ChevronDown
                    className={cn(
                      'size-3.5 transition-transform',
                      !open && '-rotate-90 rtl:rotate-90',
                    )}
                    aria-hidden
                  />
                </button>
              )}

              {open && (
                <ul className="mt-1 flex flex-col gap-0.5">
                  {group.items.map((item) => {
                    const Icon = item.icon
                    const entries = itemEntries(item, group.id, open)

                    const content = item.available ? (
                      // NavLink keeps a static className because the ContextMenu trigger clones it
                      // through Radix Slot, which stringifies a function className and drops styling.
                      <NavLink
                        to={item.to}
                        end={item.to === '/'}
                        onClick={onNavigate}
                        onKeyDown={(event) => {
                          if (!isContextMenuShortcut(event)) return
                          event.preventDefault()
                          dispatchContextMenu(event.currentTarget)
                        }}
                        aria-label={collapsed ? label(item.labelKey) : undefined}
                        title={collapsed ? label(item.labelKey) : undefined}
                        className="block rounded-lg"
                      >
                        {({ isActive }) => (
                          <span
                            className={cn(
                              'flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                              isActive
                                ? 'bg-accent text-accent-contrast'
                                : 'text-foreground hover:bg-surface-muted',
                              collapsed && 'justify-center px-2',
                            )}
                          >
                            <Icon className="size-4.5 shrink-0" aria-hidden />
                            {!collapsed && <Label k={item.labelKey} className="truncate" />}
                          </span>
                        )}
                      </NavLink>
                    ) : (
                      <span
                        aria-disabled="true"
                        aria-label={collapsed ? label(item.labelKey) : undefined}
                        title={collapsed ? label(item.labelKey) : undefined}
                        className={cn(
                          'flex w-full cursor-not-allowed items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-muted opacity-70',
                          collapsed && 'justify-center px-2',
                        )}
                      >
                        <Icon className="size-4.5 shrink-0" aria-hidden />
                        {!collapsed && (
                          <>
                            <Label k={item.labelKey} className="flex-1 truncate" />
                            <span className="rounded border border-border px-1.5 py-0.5 text-[10px] leading-none">
                              <Label k="common.coming-soon" />
                            </span>
                          </>
                        )}
                      </span>
                    )

                    return (
                      <li key={item.id}>
                        <ContextMenu entries={entries}>{content}</ContextMenu>
                        {item.id === 'documents' && !collapsed && <DocumentsRegisterNav />}
                      </li>
                    )
                  })}
                </ul>
              )}
            </div>
          )
        })}
      </nav>
    </aside>
  )
}
