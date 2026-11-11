import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

export type SortDirection = 'asc' | 'desc'

export interface TableStateOptions {
  defaultPageSize?: number
  defaultSortKey?: string | null
  defaultSortDirection?: SortDirection
  /** Extra URL parameters exposed through `filters` / `setFilter`. */
  filterKeys?: readonly string[]
  /**
   * URL parameters that may repeat (`?f=a&f=b`), exposed through `repeated` / `setRepeated`. They
   * count as active filters and are cleared with them.
   */
  repeatedKeys?: readonly string[]
  /** Changing a filter key also removes these parameters (for example field filters of a type). */
  resetOnChange?: Readonly<Record<string, readonly string[]>>
}

export interface TableState {
  page: number
  pageSize: number
  search: string
  sortKey: string | null
  sortDirection: SortDirection
  filters: Record<string, string>
  repeated: Record<string, string[]>
  hasActiveFilters: boolean
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSearch: (search: string) => void
  setSort: (sortKey: string | null, sortDirection?: SortDirection) => void
  setFilter: (key: string, value: string | null) => void
  setRepeated: (key: string, values: readonly string[]) => void
  clearFilters: () => void
  /** Removes just these URL parameters (and resets the page) in one write. */
  removeParams: (keys: readonly string[]) => void
  clearQuery: () => void
}

/** Bounds a grid may page by; `MAX_PAGE_SIZE` mirrors the server's `PageRequest.MaxPageSize`. */
export const MIN_PAGE_SIZE = 1
export const MAX_PAGE_SIZE = 100
const DEFAULT_PAGE_SIZE = 10

function parsePositiveInt(value: string | null, fallback: number): number {
  if (!value) return fallback
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback
}

/**
 * Table view state (page, size, search, sort, filters) kept in the URL so every
 * grid is bookmarkable and shareable. Column visibility is deliberately not
 * here — it is a per-browser preference stored by `columnPreferences`.
 */
export function useTableState(options: TableStateOptions = {}): TableState {
  const {
    defaultPageSize = DEFAULT_PAGE_SIZE,
    defaultSortKey = null,
    defaultSortDirection = 'desc',
    filterKeys = [],
    repeatedKeys = [],
    resetOnChange = {},
  } = options

  const [searchParams, setSearchParams] = useSearchParams()

  const page = parsePositiveInt(searchParams.get('page'), 1)
  const pageSize = Math.min(
    Math.max(parsePositiveInt(searchParams.get('pageSize'), defaultPageSize), MIN_PAGE_SIZE),
    MAX_PAGE_SIZE,
  )
  const search = searchParams.get('search') ?? ''
  const sortKey = searchParams.get('sort') ?? defaultSortKey
  const rawDirection = searchParams.get('dir')
  const sortDirection: SortDirection =
    rawDirection === 'asc' || rawDirection === 'desc' ? rawDirection : defaultSortDirection

  const filterSignature = filterKeys.join('|')
  const filters = useMemo(() => {
    const values: Record<string, string> = {}
    if (!filterSignature) return values
    for (const key of filterSignature.split('|')) {
      const value = searchParams.get(key)
      if (value) values[key] = value
    }
    return values
  }, [filterSignature, searchParams])

  const resetSignature = JSON.stringify(resetOnChange)
  const repeatedSignature = repeatedKeys.join("|")
  const repeated = useMemo(() => {
    const values: Record<string, string[]> = {}
    if (!repeatedSignature) return values
    for (const key of repeatedSignature.split("|")) {
      const all = searchParams.getAll(key).filter(Boolean)
      if (all.length > 0) values[key] = all
    }
    return values
  }, [repeatedSignature, searchParams])

  const update = useCallback(
    (mutate: (next: URLSearchParams) => void, resetPage = false) => {
      const next = new URLSearchParams(searchParams)
      if (resetPage) next.delete('page')
      mutate(next)
      setSearchParams(next, { replace: true })
    },
    [searchParams, setSearchParams],
  )

  const setPage = useCallback(
    (nextPage: number) => {
      const safePage = Math.max(1, Math.trunc(nextPage))
      update((next) => {
        if (safePage <= 1) next.delete('page')
        else next.set('page', String(safePage))
      })
    },
    [update],
  )

  const setPageSize = useCallback(
    (nextSize: number) => {
      const safeSize = Math.min(Math.max(Math.trunc(nextSize), MIN_PAGE_SIZE), MAX_PAGE_SIZE)
      update((next) => {
        if (safeSize === defaultPageSize) next.delete('pageSize')
        else next.set('pageSize', String(safeSize))
      }, true)
    },
    [defaultPageSize, update],
  )

  const setSearch = useCallback(
    (value: string) => {
      const trimmed = value.trim()
      update((next) => {
        if (!trimmed) next.delete('search')
        else next.set('search', trimmed)
      }, true)
    },
    [update],
  )

  const setSort = useCallback(
    (nextKey: string | null, nextDirection?: SortDirection) => {
      update((next) => {
        if (!nextKey) {
          next.delete('sort')
          next.delete('dir')
          return
        }
        next.set('sort', nextKey)
        if (nextDirection) next.set('dir', nextDirection)
      }, true)
    },
    [update],
  )

  const setFilter = useCallback(
    (key: string, value: string | null) => {
      const trimmed = value?.trim() ?? ''
      update((next) => {
        if (!trimmed) next.delete(key)
        else next.set(key, trimmed)
        const dependents = (JSON.parse(resetSignature) as Record<string, string[]>)[key] ?? []
        for (const dependent of dependents) next.delete(dependent)
      }, true)
    },
    [resetSignature, update],
  )

  const setRepeated = useCallback(
    (key: string, values: readonly string[]) => {
      update((next) => {
        next.delete(key)
        for (const value of values) if (value) next.append(key, value)
      }, true)
    },
    [update],
  )

  const clearFilters = useCallback(() => {
    update((next) => {
      for (const key of filterKeys) next.delete(key)
      for (const key of repeatedKeys) next.delete(key)
    }, true)
  }, [filterKeys, repeatedKeys, update])

  // One URL write: two sequential setters would each start from the same stale
  // search params, so the second would restore what the first removed.
  const clearQuery = useCallback(() => {
    update((next) => {
      next.delete('search')
      for (const key of filterKeys) next.delete(key)
      for (const key of repeatedKeys) next.delete(key)
    }, true)
  }, [filterKeys, repeatedKeys, update])

  const removeParams = useCallback(
    (keys: readonly string[]) => {
      update((next) => {
        for (const key of keys) next.delete(key)
      }, true)
    },
    [update],
  )

  return {
    page,
    pageSize,
    search,
    sortKey,
    sortDirection,
    filters,
    repeated,
    hasActiveFilters: Object.keys(filters).length > 0 || Object.keys(repeated).length > 0,
    setPage,
    setPageSize,
    setSearch,
    setSort,
    setFilter,
    setRepeated,
    clearFilters,
    removeParams,
    clearQuery,
  }
}
