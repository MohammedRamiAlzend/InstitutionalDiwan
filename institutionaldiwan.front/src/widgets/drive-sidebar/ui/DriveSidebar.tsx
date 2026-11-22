import { useState } from 'react'
import {
  ClipboardList,
  FolderOpen,
  Gauge,
  MoreHorizontal,
  Pencil,
  Plus,
  Settings,
  Star,
  Trash2,
  type LucideIcon,
} from 'lucide-react'
import { Permissions, type LabelKey } from '@/shared/config'
import { cn, t } from '@/shared/lib'
import {
  Button,
  DropdownMenu,
  GuideRegion,
  Label,
  Skeleton,
  type MenuEntry,
} from '@/shared/ui'
import { useSessionStore } from '@/entities/user'
import { QuotaMeter, type DriveSpaceResponse } from '@/entities/drive-space'
import type { DriveView } from '@/entities/drive-item'
import {
  CreateSpaceDialog,
  DriveSettingsDialog,
  RenameSpaceDialog,
  SpaceQuotaDialog,
} from '@/features/drive-space-manage'

interface DriveSidebarProps {
  personal: DriveSpaceResponse | null
  shared: DriveSpaceResponse[]
  isLoading: boolean
  view: DriveView
  spaceId: string | null
  onSelectView: (view: DriveView) => void
  onSelectSpace: (spaceId: string) => void
}

const VIEWS: readonly { id: DriveView; labelKey: LabelKey; icon: LucideIcon }[] = [
  { id: 'starred', labelKey: 'drive.starred', icon: Star },
  { id: 'trash', labelKey: 'drive.trash', icon: Trash2 },
  { id: 'deletion', labelKey: 'drive.view.deletion-requests', icon: ClipboardList },
]

const rowClass =
  'flex h-9 w-full min-w-0 items-center gap-2.5 rounded-lg px-2.5 text-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring'

interface SpaceRowProps {
  space: DriveSpaceResponse
  active: boolean
  canRename: boolean
  canQuota: boolean
  onClick: () => void
  onRename: () => void
  onQuota: () => void
}

function SpaceRow({ space, active, canRename, canQuota, onClick, onRename, onQuota }: SpaceRowProps) {
  const entries: MenuEntry[] = []
  if (canRename) {
    entries.push({
      id: 'rename',
      label: t('drive.action.rename'),
      icon: <Pencil className="size-4" aria-hidden />,
      onSelect: onRename,
    })
  }
  if (canQuota) {
    entries.push({
      id: 'quota',
      label: t('drive.dialog.space-quota-title'),
      icon: <Gauge className="size-4" aria-hidden />,
      onSelect: onQuota,
    })
  }

  return (
    <div className={cn('group flex items-center gap-0.5 rounded-lg', active && 'bg-accent/10')}>
      <button
        type="button"
        onClick={onClick}
        aria-current={active ? 'page' : undefined}
        className={cn(
          rowClass,
          active ? 'font-semibold text-accent' : 'text-foreground hover:bg-surface-muted',
        )}
      >
        <FolderOpen className="size-4 shrink-0" aria-hidden />
        <span className="truncate">{space.name}</span>
      </button>
      {entries.length > 0 && (
        <DropdownMenu
          entries={entries}
          trigger={
            <Button
              variant="ghost"
              size="icon"
              className="size-7 shrink-0 text-muted opacity-0 transition-opacity group-hover:opacity-100 focus-visible:opacity-100 aria-expanded:opacity-100"
              aria-label={t('table.row-actions')}
            >
              <MoreHorizontal className="size-4" aria-hidden />
            </Button>
          }
        />
      )}
    </div>
  )
}

/**
 * The Drive workspace rail: a short quick-view list (starred / trash / deletion requests), the
 * personal + reachable shared spaces with the active space's quota, and the workspace settings
 * entry. Space creation is open to every `DRIVE_USE` holder; rename needs ownership (or ADMIN) and
 * the quota override / Drive settings need `DRIVE_MANAGE`.
 */
export function DriveSidebar({
  personal,
  shared,
  isLoading,
  view,
  spaceId,
  onSelectView,
  onSelectSpace,
}: DriveSidebarProps) {
  const userId = useSessionStore((state) => state.user?.userId ?? null)
  const canManage = useSessionStore(
    (state) => state.user?.permissions.includes(Permissions.DriveManage) ?? false,
  )
  const [createOpen, setCreateOpen] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [renameTarget, setRenameTarget] = useState<DriveSpaceResponse | null>(null)
  const [quotaTarget, setQuotaTarget] = useState<DriveSpaceResponse | null>(null)

  const activeSpace =
    view === 'shared' ? (shared.find((space) => space.spaceId === spaceId) ?? null) : personal

  const canRenameSpace = (space: DriveSpaceResponse) =>
    canManage || space.ownerUserId === userId

  return (
    <aside className="flex flex-col gap-5 rounded-xl border border-border bg-surface p-3">
      <nav className="flex flex-col gap-0.5" aria-label={t('drive.sidebar.views')}>
        <p className="px-2.5 pb-1 text-[11px] font-semibold uppercase tracking-wide text-muted">
          <Label k="drive.sidebar.views" />
        </p>
        {VIEWS.map(({ id, labelKey, icon: Icon }) => (
          <button
            key={id}
            type="button"
            onClick={() => onSelectView(id)}
            aria-current={view === id ? 'page' : undefined}
            className={cn(
              rowClass,
              view === id
                ? 'bg-accent/10 font-semibold text-accent'
                : 'text-foreground hover:bg-surface-muted',
            )}
          >
            <Icon className="size-4 shrink-0" aria-hidden />
            <Label k={labelKey} />
          </button>
        ))}
      </nav>

      <GuideRegion id="drive.spaces" className="flex flex-col gap-0.5 border-t border-border pt-4">
        <div className="flex items-center justify-between ps-2.5 pe-1 pb-1">
          <p className="text-[11px] font-semibold uppercase tracking-wide text-muted">
            <Label k="drive.sidebar.spaces" />
          </p>
          <Button
            variant="ghost"
            size="icon"
            className="size-7 text-muted hover:text-foreground"
            onClick={() => setCreateOpen(true)}
            aria-label={t('drive.action.new-space')}
          >
            <Plus className="size-4" aria-hidden />
          </Button>
        </div>

        {isLoading ? (
          <div className="flex flex-col gap-1.5 px-1">
            <Skeleton className="h-9 w-full" />
            <Skeleton className="h-9 w-full" />
          </div>
        ) : (
          <>
            {personal && (
              <SpaceRow
                space={personal}
                active={view === 'my-files'}
                canRename={canRenameSpace(personal)}
                canQuota={canManage}
                onClick={() => onSelectView('my-files')}
                onRename={() => setRenameTarget(personal)}
                onQuota={() => setQuotaTarget(personal)}
              />
            )}

            {shared.length > 0 ? (
              <>
                <p className="mt-1 px-2.5 pb-0.5 text-[11px] font-medium text-muted">
                  <Label k="drive.shared-spaces" />
                </p>
                {shared.map((space) => (
                  <SpaceRow
                    key={space.spaceId}
                    space={space}
                    active={view === 'shared' && spaceId === space.spaceId}
                    canRename={canRenameSpace(space)}
                    canQuota={canManage}
                    onClick={() => {
                      onSelectSpace(space.spaceId)
                      onSelectView('shared')
                    }}
                    onRename={() => setRenameTarget(space)}
                    onQuota={() => setQuotaTarget(space)}
                  />
                ))}
              </>
            ) : (
              <p className="px-2.5 py-1 text-xs text-muted">
                <Label k="drive.sidebar.no-shared" />
              </p>
            )}
          </>
        )}

        {activeSpace && (
          <GuideRegion
            id="drive.quota"
            className="mt-2 rounded-lg border border-border bg-surface-muted/40 px-2.5 py-2"
          >
            <QuotaMeter
              usedBytes={activeSpace.usedBytes}
              quotaBytes={activeSpace.effectiveQuotaBytes}
            />
          </GuideRegion>
        )}
      </GuideRegion>

      {canManage && (
        <div className="mt-auto border-t border-border pt-2">
          <button
            type="button"
            onClick={() => setSettingsOpen(true)}
            className={cn(rowClass, 'text-muted hover:bg-surface-muted hover:text-foreground')}
          >
            <Settings className="size-4 shrink-0" aria-hidden />
            <Label k="drive.dialog.settings-title" />
          </button>
        </div>
      )}

      <CreateSpaceDialog open={createOpen} onOpenChange={setCreateOpen} />
      <DriveSettingsDialog open={settingsOpen} onOpenChange={setSettingsOpen} />
      {renameTarget && (
        <RenameSpaceDialog
          space={renameTarget}
          open
          onOpenChange={(nextOpen) => {
            if (!nextOpen) setRenameTarget(null)
          }}
        />
      )}
      {quotaTarget && (
        <SpaceQuotaDialog
          space={quotaTarget}
          open
          onOpenChange={(nextOpen) => {
            if (!nextOpen) setQuotaTarget(null)
          }}
        />
      )}
    </aside>
  )
}
