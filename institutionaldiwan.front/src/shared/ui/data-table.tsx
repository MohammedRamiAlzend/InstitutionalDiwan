import {
  Fragment,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import {
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  Check,
  ChevronLeft,
  ChevronRight,
  Columns3,
  Inbox,
  MoreHorizontal,
  RefreshCw,
  RotateCcw,
  Search,
} from 'lucide-react'
import {
  cn,
  formatNumber,
  loadColumnVisibility,
  resetColumnVisibility,
  saveColumnVisibility,
  useDebouncedValue,
  type SortDirection,
} from '@/shared/lib'
import { toArabicErrorMessage } from '@/shared/api'
import type { MenuEntry } from './menu'
import {
  dispatchContextMenu,
  isContextMenuShortcut,
} from './menu'
import { Button } from './button'
import { ContextMenu } from './context-menu'
import { DropdownMenu } from './dropdown-menu'
import { Input } from './input'
import { Select } from './select'
import { Skeleton } from './skeleton'

export interface DataTableColumn<T> {
  id: string
  header: string
  cell: (row: T) => ReactNode
  sortable?: boolean
  /** Server sort key; defaults to the column id. */
  sortKey?: string
  /** Client-mode comparison value. */
  sortValue?: (row: T) => string | number | null | undefined
  defaultHidden?: boolean
  /** Identifying columns cannot be hidden from the column chooser. */
  hideable?: boolean
  className?: string
  headerClassName?: string
  width?: string
}

export type DataTableMode = 'server' | 'client'

export interface DataTableProps<T> {
  /** Stable id used for the persisted column preference. */
  tableId: string
  columns: readonly DataTableColumn<T>[]
  rows: readonly T[] | undefined
  rowKey: (row: T) => string
  mode?: DataTableMode
  caption?: string
  isLoading?: boolean
  error?: unknown
  onRetry?: () => void
  onRefresh?: () => void
  isFetching?: boolean
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
  pageSizeOptions?: readonly number[]
  search?: string
  onSearchChange?: (search: string) => void
  searchPlaceholder?: string
  sortKey?: string | null
  sortDirection?: SortDirection
  onSortChange?: (sortKey: string | null, direction: SortDirection) => void
  rowMenu?: (row: T) => readonly MenuEntry[]
  headerMenu?: readonly MenuEntry[]
  emptyMenu?: readonly MenuEntry[]
  rowLabel?: (row: T) => string
  rowSearchText?: (row: T) => string
  toolbar?: ReactNode
  filters?: ReactNode
  emptyTitle?: string
  emptyDescription?: string
  emptyAction?: ReactNode
  className?: string
}

const DEFAULT_PAGE_SIZES = [10, 20, 50, 100]
const SEARCH_DEBOUNCE_MS = 300

function compareValues(
  left: string | number | null | undefined,
  right: string | number | null | undefined,
): number {
  if (left === right) return 0
  if (left === null || left === undefined) return 1
  if (right === null || right === undefined) return -1
  if (typeof left === 'number' && typeof right === 'number') return left - right
  return String(left).localeCompare(String(right), 'ar')
}

/**
 * The only real table. Filters/search/sort/paging come from `useTableState`
 * (URL state); column visibility persists per browser under
 * `diwan.table.<tableId>.columns.v1`. Rows, headers, and the empty area expose
 * permission-filtered context menus plus an always-visible overflow trigger.
 */
export function DataTable<T>({
  tableId,
  columns,
  rows,
  rowKey,
  mode = 'server',
  caption,
  isLoading = false,
  error,
  onRetry,
  onRefresh,
  isFetching,
  page,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = DEFAULT_PAGE_SIZES,
  search = '',
  onSearchChange,
  searchPlaceholder = 'بحث…',
  sortKey = null,
  sortDirection = 'asc',
  onSortChange,
  rowMenu,
  headerMenu,
  emptyMenu,
  rowLabel,
  rowSearchText,
  toolbar,
  filters,
  emptyTitle = 'لا توجد بيانات للعرض.',
  emptyDescription,
  emptyAction,
  className,
}: DataTableProps<T>) {
  const [visibility, setVisibility] = useState<Record<string, boolean>>(() =>
    loadColumnVisibility(tableId, columns),
  )

  const effectiveVisibility = useMemo(() => {
    const map: Record<string, boolean> = {}
    for (const column of columns) {
      map[column.id] =
        visibility[column.id] ?? (column.hideable === false ? true : !column.defaultHidden)
    }
    return map
  }, [columns, visibility])

  const visibleColumns = useMemo(
    () => columns.filter((column) => effectiveVisibility[column.id]),
    [columns, effectiveVisibility],
  )

  const [searchDraft, setSearchDraft] = useState(search)
  const debouncedSearch = useDebouncedValue(searchDraft, SEARCH_DEBOUNCE_MS)
  const lastEmittedSearch = useRef<string | undefined>(undefined)

  useEffect(() => {
    if (!onSearchChange) return
    const next = debouncedSearch.trim()
    if (next === search.trim()) return
    lastEmittedSearch.current = next
    onSearchChange(debouncedSearch)
  }, [debouncedSearch, onSearchChange, search])

  useEffect(() => {
    if (lastEmittedSearch.current !== undefined && search === lastEmittedSearch.current) {
      lastEmittedSearch.current = undefined
      return
    }
    setSearchDraft(search)
  }, [search])

  const processedRows = useMemo(() => {
    if (mode !== 'client') return rows ?? []
    let data = [...(rows ?? [])]

    const query = search.trim().toLocaleLowerCase('ar')
    if (query && rowSearchText) {
      data = data.filter((row) => rowSearchText(row).toLocaleLowerCase('ar').includes(query))
    }

    if (sortKey) {
      const sortColumn = columns.find((column) => (column.sortKey ?? column.id) === sortKey)
      if (sortColumn?.sortValue) {
        const direction = sortDirection === 'asc' ? 1 : -1
        data.sort(
          (left, right) => compareValues(sortColumn.sortValue?.(left), sortColumn.sortValue?.(right)) * direction,
        )
      }
    }

    return data
  }, [mode, rows, search, rowSearchText, sortKey, sortDirection, columns])

  const effectiveTotal = mode === 'client' ? processedRows.length : totalCount
  const pagedRows =
    mode === 'client'
      ? processedRows.slice((page - 1) * pageSize, page * pageSize)
      : processedRows

  const totalPages = Math.max(1, Math.ceil(effectiveTotal / pageSize))
  const rangeStart = effectiveTotal === 0 ? 0 : (page - 1) * pageSize + 1
  const rangeEnd = Math.min(page * pageSize, effectiveTotal)

  useEffect(() => {
    if (!isLoading && page > totalPages) onPageChange(totalPages)
  }, [isLoading, onPageChange, page, totalPages])

  const toggleColumn = (columnId: string) => {
    const column = columns.find((candidate) => candidate.id === columnId)
    if (column?.hideable === false) return

    setVisibility(() => {
      const next = { ...effectiveVisibility, [columnId]: !effectiveVisibility[columnId] }
      if (!columns.some((candidate) => next[candidate.id])) return effectiveVisibility
      saveColumnVisibility(tableId, next)
      return next
    })
  }

  const resetColumns = () => {
    resetColumnVisibility(tableId)
    setVisibility(loadColumnVisibility(tableId, columns))
  }

  const columnMenuEntries: MenuEntry[] = [
    { type: 'label', label: 'أعمدة الجدول' },
    ...columns.map<MenuEntry>((column) => ({
      id: `column-${column.id}`,
      label: column.header,
      icon: effectiveVisibility[column.id] ? (
        <Check className="size-4 text-accent" aria-hidden />
      ) : (
        <span className="size-4" aria-hidden />
      ),
      disabled: column.hideable === false,
      keepOpen: true,
      onSelect: () => toggleColumn(column.id),
    })),
    { type: 'separator' },
    {
      id: 'reset-columns',
      label: 'إعادة الأعمدة الافتراضية',
      icon: <RotateCcw className="size-4" aria-hidden />,
      onSelect: resetColumns,
    },
  ]

  const handleSort = (column: DataTableColumn<T>) => {
    if (!column.sortable || !onSortChange) return
    const key = column.sortKey ?? column.id
    if (sortKey !== key) {
      onSortChange(key, 'asc')
    } else if (sortDirection === 'asc') {
      onSortChange(key, 'desc')
    } else {
      onSortChange(null, 'asc')
    }
  }

  const spinner = isFetching ? 'animate-spin' : undefined

  const header = (
    <thead className="bg-surface-muted">
      <tr>
        {visibleColumns.map((column) => {
          const key = column.sortKey ?? column.id
          const active = sortKey === key
          const ariaSort = active
            ? sortDirection === 'asc'
              ? 'ascending'
              : 'descending'
            : 'none'

          return (
            <th
              key={column.id}
              scope="col"
              aria-sort={column.sortable ? ariaSort : undefined}
              style={column.width ? { width: column.width } : undefined}
              className={cn(
                'px-3 py-2.5 text-start text-xs font-semibold text-muted whitespace-nowrap',
                column.headerClassName,
              )}
            >
              {column.sortable && onSortChange ? (
                <button
                  type="button"
                  onClick={() => handleSort(column)}
                  aria-label={`فرز حسب ${column.header}`}
                  className="inline-flex cursor-pointer items-center gap-1 text-start transition-colors hover:text-foreground"
                >
                  {column.header}
                  {active ? (
                    sortDirection === 'asc' ? (
                      <ArrowUp className="size-3.5" aria-hidden />
                    ) : (
                      <ArrowDown className="size-3.5" aria-hidden />
                    )
                  ) : (
                    <ArrowUpDown className="size-3.5 opacity-60" aria-hidden />
                  )}
                </button>
              ) : (
                column.header
              )}
            </th>
          )
        })}
        {rowMenu && (
          <th scope="col" aria-label="إجراءات الصف" className="w-12 px-2 py-2.5">
            <span className="sr-only">إجراءات</span>
          </th>
        )}
      </tr>
    </thead>
  )

  const columnCount = visibleColumns.length + (rowMenu ? 1 : 0)

  return (
    <div className={cn('flex flex-col gap-3', className)}>
      <div className="flex flex-col gap-3">
        <div className="flex flex-wrap items-center gap-2">
          {onSearchChange && (
            <Input
              type="search"
              value={searchDraft}
              onChange={(event) => setSearchDraft(event.target.value)}
              placeholder={searchPlaceholder}
              aria-label={searchPlaceholder}
              startIcon={<Search className="size-4" aria-hidden />}
              className="h-10 w-full ps-10 pe-4 sm:w-72"
            />
          )}
          {toolbar}
          <div className="ms-auto flex items-center gap-2">
            {onRefresh && (
              <Button
                type="button"
                variant="outline"
                size="icon"
                onClick={onRefresh}
                aria-label="تحديث البيانات"
                className="size-10"
              >
                <RefreshCw className={cn('size-4', spinner)} aria-hidden />
              </Button>
            )}
            <DropdownMenu
              trigger={
                <Button type="button" variant="outline" size="sm" className="h-10">
                  <Columns3 className="size-4" aria-hidden />
                  الأعمدة
                </Button>
              }
              entries={columnMenuEntries}
              label="تخصيص الجدول"
            />
          </div>
        </div>
        {filters}
      </div>

      {error !== undefined && error !== null && (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
        >
          <span>{toArabicErrorMessage(error)}</span>
          {onRetry && (
            <Button type="button" variant="outline" size="sm" onClick={onRetry}>
              إعادة المحاولة
            </Button>
          )}
        </div>
      )}

      <div className="overflow-hidden rounded-xl border border-border bg-surface">
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-sm">
            <caption className="sr-only">{caption ?? 'جدول البيانات'}</caption>
            {headerMenu && headerMenu.length > 0 ? (
              <ContextMenu entries={headerMenu}>{header}</ContextMenu>
            ) : (
              header
            )}

            <tbody>
              {isLoading ? (
                Array.from({ length: 5 }, (_, index) => (
                  <tr key={index} className="border-t border-border">
                    {visibleColumns.map((column) => (
                      <td key={column.id} className="px-3 py-3">
                        <Skeleton className="h-4 w-full" />
                      </td>
                    ))}
                    {rowMenu && (
                      <td className="px-3 py-3">
                        <Skeleton className="size-4" />
                      </td>
                    )}
                  </tr>
                ))
              ) : pagedRows.length === 0 ? (
                (() => {
                  const emptyRow = (
                    <tr className="border-t border-border">
                      <td colSpan={columnCount} className="px-4 py-12">
                        <div className="flex flex-col items-center gap-3 text-center">
                          <span className="flex size-12 items-center justify-center rounded-full bg-surface-muted text-muted">
                            <Inbox className="size-6" aria-hidden />
                          </span>
                          <div className="flex flex-col gap-1">
                            <p className="text-sm font-medium text-foreground">{emptyTitle}</p>
                            {emptyDescription && (
                              <p className="text-xs text-muted">{emptyDescription}</p>
                            )}
                          </div>
                          {emptyAction}
                        </div>
                      </td>
                    </tr>
                  )
                  return emptyMenu && emptyMenu.length > 0 ? (
                    <ContextMenu entries={emptyMenu}>{emptyRow}</ContextMenu>
                  ) : (
                    emptyRow
                  )
                })()
              ) : (
                pagedRows.map((row) => {
                  const key = rowKey(row)
                  const entries = rowMenu?.(row) ?? []
                  const label = rowLabel?.(row)

                  const rowElement = (
                    <tr
                      tabIndex={0}
                      onKeyDown={(event) => {
                        if (entries.length === 0 || !isContextMenuShortcut(event)) return
                        event.preventDefault()
                        dispatchContextMenu(event.currentTarget)
                      }}
                      className="border-t border-border transition-colors hover:bg-surface-muted/60 focus-visible:bg-surface-muted/60"
                    >
                      {visibleColumns.map((column) => (
                        <td
                          key={column.id}
                          className={cn('px-3 py-2.5 align-middle text-foreground', column.className)}
                        >
                          {column.cell(row)}
                        </td>
                      ))}
                      {rowMenu && (
                        <td className="px-2 py-1.5 text-end">
                          {entries.length > 0 && (
                            <DropdownMenu
                              trigger={
                                <Button
                                  type="button"
                                  variant="ghost"
                                  size="icon"
                                  className="size-8"
                                  aria-label={label ? `إجراءات ${label}` : 'إجراءات الصف'}
                                >
                                  <MoreHorizontal className="size-4" aria-hidden />
                                </Button>
                              }
                              entries={entries}
                            />
                          )}
                        </td>
                      )}
                    </tr>
                  )

                  return entries.length > 0 ? (
                    <ContextMenu key={key} entries={entries}>
                      {rowElement}
                    </ContextMenu>
                  ) : (
                    <Fragment key={key}>{rowElement}</Fragment>
                  )
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2 text-xs text-muted">
          <span>
            عرض <span dir="ltr">{formatNumber(rangeStart)}–{formatNumber(rangeEnd)}</span> من{' '}
            <span dir="ltr">{formatNumber(effectiveTotal)}</span>
          </span>
          <span className="hidden sm:inline">·</span>
          <label className="flex items-center gap-1.5">
            <span className="hidden sm:inline">صفوف في الصفحة</span>
            <Select
              value={String(pageSize)}
              onValueChange={(value) => onPageSizeChange(Number.parseInt(value, 10))}
              options={pageSizeOptions.map((size) => ({
                value: String(size),
                label: formatNumber(size),
              }))}
              aria-label="عدد الصفوف في الصفحة"
              className="h-8 w-20 text-xs"
            />
          </label>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-xs text-muted">
            الصفحة <span dir="ltr">{formatNumber(page)}</span> من{' '}
            <span dir="ltr">{formatNumber(totalPages)}</span>
          </span>
          <Button
            type="button"
            variant="outline"
            size="icon"
            className="size-8"
            onClick={() => onPageChange(page - 1)}
            disabled={page <= 1 || isLoading}
            aria-label="الصفحة السابقة"
          >
            <ChevronLeft className="size-4 rtl:rotate-180" aria-hidden />
          </Button>
          <Button
            type="button"
            variant="outline"
            size="icon"
            className="size-8"
            onClick={() => onPageChange(page + 1)}
            disabled={page >= totalPages || isLoading}
            aria-label="الصفحة التالية"
          >
            <ChevronRight className="size-4 rtl:rotate-180" aria-hidden />
          </Button>
        </div>
      </div>
    </div>
  )
}
