import { useEffect, useState, type ReactNode } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { CircleHelp, Pause, Play, RefreshCw, Rows3, Settings2 } from 'lucide-react'
import {
  ContextMenu,
  ErrorBoundary,
  dispatchContextMenu,
  isContextMenuShortcut,
  type MenuEntry,
} from '@/shared/ui'
import { setTutorialChoice, useTutorial } from '@/shared/lib'
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

  const title = findNavItem(location.pathname)?.label ?? 'الديوان المؤسسي'

  // Only an explicit choice is stored, so `prefers-reduced-motion` keeps steering until then.
  const changeMotion = (next: Motion) => {
    setMotion(next)
    persistMotion(next)
  }

  const shellMenu: MenuEntry[] = [
    {
      id: 'refresh-data',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => {
        void queryClient.invalidateQueries()
      },
    },
    {
      id: 'toggle-density',
      label: density === 'comfortable' ? 'العرض المضغوط' : 'العرض المريح',
      icon: <Rows3 className="size-4" aria-hidden />,
      onSelect: () =>
        setDensity((previous) => (previous === 'comfortable' ? 'compact' : 'comfortable')),
    },
    {
      id: 'toggle-motion',
      label: motion === 'on' ? 'إيقاف الحركات' : 'تفعيل الحركات',
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
      label: tutorial === 'on' ? 'إيقاف الوضع التعليمي' : 'تفعيل الوضع التعليمي',
      icon: <CircleHelp className="size-4" aria-hidden />,
      onSelect: () => setTutorialChoice(tutorial === 'on' ? 'off' : 'on'),
    },
    { type: 'separator' },
    {
      id: 'open-settings',
      label: 'إعدادات العرض',
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
            aria-label="إغلاق القائمة"
            onClick={() => setMobileOpen(false)}
            className="absolute inset-0 cursor-pointer bg-overlay animate-overlay-in"
          />
          <div className="absolute inset-y-0 start-0 shadow-xl animate-content-in">
            <AppSidebar onNavigate={() => setMobileOpen(false)} />
          </div>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
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
            aria-label="محتوى الصفحة"
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
