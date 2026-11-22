import { useEffect, useState } from 'react'
import { Columns3, LayoutGrid, List, RefreshCw, Search, TableProperties, FolderPlus, Upload } from 'lucide-react'
import { cn, t, useDebouncedValue } from '@/shared/lib'
import { Button, Input, Label, Select, type SelectOption } from '@/shared/ui'

export type DriveLayout = 'details' | 'list' | 'tiles' | 'grid'

interface DriveBrowserControlsProps {
  search: string
  onSearchChange: (value: string) => void
  layout: DriveLayout
  onLayoutChange: (layout: DriveLayout) => void
  canCreateFolder: boolean
  onNewFolder: () => void
  onUpload?: () => void
  kind: string
  onKindChange: (value: string) => void
  owner: string
  onOwnerChange: (value: string) => void
  ownerOptions: readonly SelectOption[]
  onRefresh: () => void
  isFetching: boolean
}

/** Explorer-style toolbar shared by all Drive presentations; view and filter state stays in the URL. */
export function DriveBrowserControls({
  search,
  onSearchChange,
  layout,
  onLayoutChange,
  canCreateFolder,
  onNewFolder,
  onUpload,
  kind,
  onKindChange,
  owner,
  onOwnerChange,
  ownerOptions,
  onRefresh,
  isFetching,
}: DriveBrowserControlsProps) {
  const [draft, setDraft] = useState(search)
  const [prevSearch, setPrevSearch] = useState(search)
  if (search !== prevSearch) {
    setPrevSearch(search)
    setDraft(search)
  }
  const debounced = useDebouncedValue(draft, 300)

  useEffect(() => {
    if (debounced.trim() !== search.trim()) onSearchChange(debounced)
  }, [debounced, onSearchChange, search])

  const kindOptions: SelectOption[] = [
    { value: 'all', label: t('drive.browser.filter-all') },
    { value: 'folder', label: t('drive.browser.filter-folders') },
    { value: 'file', label: t('drive.browser.filter-files') },
  ]
  const ownerSelectOptions: SelectOption[] = [
    { value: 'all', label: t('drive.browser.filter-all') },
    ...ownerOptions,
  ]
  const views: { id: DriveLayout; icon: typeof TableProperties; label: string }[] = [
    { id: 'details', icon: TableProperties, label: t('drive.browser.view-details') },
    { id: 'list', icon: List, label: t('drive.browser.view-list') },
    { id: 'tiles', icon: Columns3, label: t('drive.browser.view-tiles') },
    { id: 'grid', icon: LayoutGrid, label: t('drive.browser.view-grid') },
  ]

  return (
    <div className="flex flex-wrap items-center gap-3 rounded-2xl border border-border bg-surface p-3 shadow-sm">
      <Input
        type="search"
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        placeholder={t('drive.browser.search-placeholder')}
        aria-label={t('drive.browser.search-placeholder')}
        startIcon={<Search className="size-4" aria-hidden />}
        className="h-10 w-full min-w-48 flex-1 rounded-xl sm:w-auto"
      />

      <Select
        aria-label={t('drive.browser.filter-kind')}
        value={kind || 'all'}
        onValueChange={onKindChange}
        options={kindOptions}
        className="h-10 w-32 rounded-xl"
      />
      <Select
        aria-label={t('drive.browser.filter-owner')}
        value={owner || 'all'}
        onValueChange={onOwnerChange}
        options={ownerSelectOptions}
        className="h-10 w-40 rounded-xl"
      />

      {(canCreateFolder || onUpload) && (
        <>
          <span className="mx-0.5 hidden h-7 border-s border-border sm:block" aria-hidden />
          <div className="flex flex-wrap items-center gap-2">
            {canCreateFolder && (
              <Button type="button" variant="outline" className="h-10 rounded-xl" onClick={onNewFolder}>
                <FolderPlus className="size-4" aria-hidden />
                <Label k="drive.action.new-folder" />
              </Button>
            )}
            {onUpload && (
              <Button type="button" className="h-10 rounded-xl" onClick={onUpload}>
                <Upload className="size-4" aria-hidden />
                <Label k="drive.action.upload" />
              </Button>
            )}
          </div>
        </>
      )}

      <div className="ms-auto flex items-center gap-1 rounded-xl border border-border bg-surface-muted/50 p-1" role="group" aria-label={t('drive.browser.view-mode')}>
        {views.map(({ id, icon: Icon, label }) => (
          <Button
            key={id}
            type="button"
            variant="ghost"
            size="icon"
            className={cn('size-8 rounded-lg', layout === id && 'bg-surface text-accent shadow-sm')}
            onClick={() => onLayoutChange(id)}
            aria-label={label}
            title={label}
            aria-pressed={layout === id}
          >
            <Icon className="size-4" aria-hidden />
          </Button>
        ))}
      </div>
      <Button
        type="button"
        variant="outline"
        size="icon"
        className="size-10 rounded-xl"
        onClick={onRefresh}
        aria-label={t('table.refresh')}
      >
        <RefreshCw className={cn('size-4', isFetching && 'animate-spin')} aria-hidden />
      </Button>
    </div>
  )
}
