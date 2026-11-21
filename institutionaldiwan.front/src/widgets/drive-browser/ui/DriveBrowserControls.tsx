import { useEffect, useState } from 'react'
import { FolderPlus, LayoutGrid, List, RefreshCw, Search, Upload } from 'lucide-react'
import { cn, t, useDebouncedValue } from '@/shared/lib'
import { Button, Input, Label, Select, type SelectOption } from '@/shared/ui'

export type DriveLayout = 'grid' | 'list'

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

/**
 * The browse toolbar shared by the grid and list presentations: debounced name search, the
 * grid/list switch, new-folder/upload entry points, kind/owner filters, and refresh. Keeping it in
 * one component means both views drive the same URL state.
 */
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

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Input
        type="search"
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        placeholder={t('drive.browser.search-placeholder')}
        aria-label={t('drive.browser.search-placeholder')}
        startIcon={<Search className="size-4" aria-hidden />}
        className="h-10 w-full ps-10 pe-4 sm:w-64"
      />

      <div className="flex items-center rounded-lg border border-border p-0.5">
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className={cn('size-8', layout === 'list' && 'bg-accent/10 text-accent')}
          onClick={() => onLayoutChange('list')}
          aria-label={t('drive.browser.view-list')}
          aria-pressed={layout === 'list'}
        >
          <List className="size-4" aria-hidden />
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className={cn('size-8', layout === 'grid' && 'bg-accent/10 text-accent')}
          onClick={() => onLayoutChange('grid')}
          aria-label={t('drive.browser.view-grid')}
          aria-pressed={layout === 'grid'}
        >
          <LayoutGrid className="size-4" aria-hidden />
        </Button>
      </div>

      {canCreateFolder && (
        <Button type="button" variant="outline" className="h-10" onClick={onNewFolder}>
          <FolderPlus className="size-4" aria-hidden />
          <Label k="drive.action.new-folder" />
        </Button>
      )}
      {onUpload && (
        <Button type="button" className="h-10" onClick={onUpload}>
          <Upload className="size-4" aria-hidden />
          <Label k="drive.action.upload" />
        </Button>
      )}

      <Select
        aria-label={t('drive.browser.filter-kind')}
        value={kind || 'all'}
        onValueChange={onKindChange}
        options={kindOptions}
        className="h-10 w-36"
      />
      <Select
        aria-label={t('drive.browser.filter-owner')}
        value={owner || 'all'}
        onValueChange={onOwnerChange}
        options={ownerSelectOptions}
        className="h-10 w-44"
      />

      <Button
        type="button"
        variant="outline"
        size="icon"
        className="ms-auto size-10"
        onClick={onRefresh}
        aria-label={t('table.refresh')}
      >
        <RefreshCw className={cn('size-4', isFetching && 'animate-spin')} aria-hidden />
      </Button>
    </div>
  )
}