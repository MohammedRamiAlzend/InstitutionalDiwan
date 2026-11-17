import { useEffect, useState, type ReactNode } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { CircleHelp, Pause, PenLine, Play, RefreshCw, Rows3, Settings2 } from 'lucide-react'
import { useBrandName } from '@/entities/branding'
import { useSessionStore } from '@/entities/user'
import {
  ContextMenu,
  ErrorBoundary,
  dispatchContextMenu,
  isContextMenuShortcut,
  type MenuEntry,
} from '@/shared/ui'
import {
  resolveLabel,
  setLabelEditMode,
  setTutorialChoice,
  t,
  useLabelEdit,
  useLabelsStore,
  useTutorial,
} from '@/shared/lib'
import { AppHeader } from './AppHeader'
import { AppSidebar } from './AppSidebar'
import { SettingsDialog } from './SettingsDialog'
import {
  applyDensity,
  getStoredDensity,
  persistDensity,
  type Density,
} from '../model/density'
import {
  applyMotion,
  getStoredMotion,
  persistMotion,
  type Motion,
} from '../model/motion'
import { findNavItem } from '../model/shell-nav'

interface AppShellProps {
  /**
   * Extra header controls rendered before the theme toggle (the notification bell). Composed by
   * the app layer because a widget may not import a sibling widget.
   */
  headerSlot?: ReactNode
}

/** Authenticated workspace layout: sidebar, header, settings, and page outlet. */
export function AppShell({ headerSlot }: AppShellProps = {}) {
  const location = useLocation()
  const queryClient = useQueryClient()
  const [collapsed, setCollapsed] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [density, setDensity] = useState<Density>(getStoredDensity)
  const [motion, setMotion] = useState<Motion>(getStoredMotion)
  const tutorial = useTutorial()
  const labelEdit = useLabelEdit()
  // The label-edit capability is bound to the superadmin account itself (not a grantable
  // permission), so the toggle and banner are absent from the DOM for everyone else.
  const isSuperAdmin = useSessionStore((state) => state.user?.username) === 'superadmin'

  useEffect(() => {
    applyDensity(density)
    persistDensity(density)
    return () => {
      delete document.documentElement.dataset.density
    }
  }, [density])

  useEffect(() => {
    applyMotion(motion)
  }, [motion])

  useEffect(() => {
    if (!mobileOpen) return

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setMobileOpen(false)
    }
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    window.addEventListener('keydown', handleKeyDown)

    return () => {
      document.body.style.overflow = previousOverflow
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [mobileOpen])

  const brandName = useBrandName()
  const overrides = useLabelsStore((state) => state.overrides)
  const navItem = findNavItem(location.pathname)
  const title = navItem ? resolveLabel(navItem.labelKey, overrides) : brandName

  // Only an explicit choice is stored, so `prefers-reduced-motion` keeps steering until then.
  const changeMotion = (next: Motion) => {
    setMotion(next)
    persistMotion(next)
  }

  const shellMenu: MenuEntry[] = [
    {
      id: 'refresh-data',
      label: t('shell.refresh'),
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => {
        void queryClient.invalidateQueries()
      },
    },
    {
      id: 'toggle-density',
      label: density === 'comfortable' ? t('shell.density-compact') : t('shell.density-comfortable'),
      icon: <Rows3 className="size-4" aria-hidden />,
      onSelect: () =>
        setDensity((previous) => (previous === 'comfortable' ? 'compact' : 'comfortable')),
    },
    {
      id: 'toggle-motion',
      label: motion === 'on' ? t('shell.motion-off') : t('shell.motion-on'),
      icon:
        motion === 'on' ? (
          <Pause className="size-4" aria-hidden />
        ) : (
          <Play className="size-4" aria-hidden />
        ),
      onSelect: () => changeMotion(motion === 'on' ? 'off' : 'on'),
    },
    {
      id: 'toggle-tutorial',
      label: tutorial === 'on' ? t('shell.tutorial-off') : t('shell.tutorial-on'),
      icon: <CircleHelp className="size-4" aria-hidden />,
      onSelect: () => setTutorialChoice(tutorial === 'on' ? 'off' : 'on'),
    },
    ...(isSuperAdmin
      ? [
          {
            id: 'toggle-label-edit',
            label: labelEdit === 'on' ? t('shell.label-edit-off') : t('shell.label-edit-on'),
            icon: <PenLine className="size-4" aria-hidden />,
            onSelect: () => setLabelEditMode(labelEdit === 'on' ? 'off' : 'on'),
          } satisfies MenuEntry,
        ]
      : []),
    { type: 'separator' },
    {
      id: 'open-settings',
      label: t('shell.display-settings'),
      icon: <Settings2 className="size-4" aria-hidden />,
      onSelect: () => setSettingsOpen(true),
    },
  ]

  return (
    <div className="flex min-h-screen bg-background">
      <div className="sticky top-0 hidden h-screen shrink-0 lg:block">
        <AppSidebar
          collapsed={collapsed}
          onToggleCollapsed={() => setCollapsed((previous) => !previous)}
        />
      </div>

      {mobileOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <button
            type="button"
            aria-label={t('shell.sidebar-close')}
            onClick={() => setMobileOpen(false)}
            className="absolute inset-0 cursor-pointer bg-overlay animate-overlay-in"
          />
          <div className="absolute inset-y-0 start-0 shadow-xl animate-content-in">
            <AppSidebar onNavigate={() => setMobileOpen(false)} />
          </div>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        {isSuperAdmin && labelEdit === 'on' && (
          <div
            role="status"
            className="sticky top-0 z-30 flex flex-wrap items-center gap-3 border-b border-border bg-accent px-4 py-2 text-sm text-accent-contrast sm:px-6 lg:px-8"
          >
            <PenLine className="size-4 shrink-0" aria-hidden />
            <span className="min-w-0 flex-1">{t('shell.banner-text')}</span>
            <button
              type="button"
              onClick={() => setLabelEditMode('off')}
              className="shrink-0 cursor-pointer rounded-md bg-accent-contrast px-3 py-1 text-xs font-semibold text-accent transition-opacity hover:opacity-90"
            >
              {t('shell.banner-end')}
            </button>
          </div>
        )}
        <AppHeader
          title={title}
          onOpenSidebar={() => setMobileOpen(true)}
          onOpenSettings={() => setSettingsOpen(true)}
          menuEntries={shellMenu}
          headerSlot={headerSlot}
        />

        <ContextMenu entries={shellMenu}>
          <main
            tabIndex={0}
            aria-label={t('shell.page-content')}
            onKeyDown={(event) => {
              if (event.target !== event.currentTarget) return
              if (!isContextMenuShortcut(event)) return
              event.preventDefault()
              dispatchContextMenu(event.currentTarget)
            }}
            className="flex-1 px-4 py-6 sm:px-6 lg:px-8"
          >
            {/* Keyed by path so the entrance animation replays for every page, including
                a same-route change such as one document to the next. */}
            <div
              key={location.pathname}
              className="mx-auto flex w-full max-w-7xl flex-col gap-6 animate-page-in"
            >
              {/* One page failing keeps the navigation, so the user can simply go elsewhere. */}
              <ErrorBoundary resetKey={location.pathname + location.search}>
                <Outlet />
              </ErrorBoundary>
            </div>
          </main>
        </ContextMenu>
      </div>

      <SettingsDialog
        open={settingsOpen}
        onOpenChange={setSettingsOpen}
        density={density}
        onDensityChange={setDensity}
        motion={motion}
        onMotionChange={changeMotion}
      />
    </div>
  )
}
