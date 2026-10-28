const STORAGE_PREFIX = 'diwan.table.'
const STORAGE_SUFFIX = '.columns.v1'
const STORAGE_VERSION = 1

export interface ColumnPreferenceDefinition {
  id: string
  defaultHidden?: boolean
  /** Identifying columns (for example the row title) are never hidden. */
  hideable?: boolean
}

interface StoredPreference {
  version: number
  visible: string[]
}

export function columnPreferenceKey(tableId: string): string {
  return `${STORAGE_PREFIX}${tableId}${STORAGE_SUFFIX}`
}

function isStoredPreference(value: unknown): value is StoredPreference {
  if (typeof value !== 'object' || value === null) return false
  const candidate = value as { version?: unknown; visible?: unknown }
  return (
    candidate.version === STORAGE_VERSION &&
    Array.isArray(candidate.visible) &&
    candidate.visible.every((id) => typeof id === 'string')
  )
}

function defaultVisibility(
  columns: readonly ColumnPreferenceDefinition[],
): Record<string, boolean> {
  const visibility: Record<string, boolean> = {}
  for (const column of columns) {
    visibility[column.id] = column.hideable === false ? true : !column.defaultHidden
  }
  return visibility
}

/**
 * Reads the saved column visibility for a table, merged with its defaults.
 * Unknown saved ids are dropped and malformed values fall back to defaults.
 */
export function loadColumnVisibility(
  tableId: string,
  columns: readonly ColumnPreferenceDefinition[],
): Record<string, boolean> {
  const defaults = defaultVisibility(columns)
  if (typeof localStorage === 'undefined') return defaults

  try {
    const raw = localStorage.getItem(columnPreferenceKey(tableId))
    if (!raw) return defaults

    const parsed: unknown = JSON.parse(raw)
    if (!isStoredPreference(parsed)) return defaults

    const known = new Set(columns.map((column) => column.id))
    const visible = new Set(parsed.visible.filter((id) => known.has(id)))
    if (visible.size === 0) return defaults

    const merged: Record<string, boolean> = {}
    for (const column of columns) {
      merged[column.id] = column.hideable === false ? true : visible.has(column.id)
    }
    return merged
  } catch {
    // Storage can be unavailable or hold stale JSON; defaults are always safe.
    return defaults
  }
}

export function saveColumnVisibility(
  tableId: string,
  visibility: Record<string, boolean>,
): void {
  if (typeof localStorage === 'undefined') return
  try {
    const preference: StoredPreference = {
      version: STORAGE_VERSION,
      visible: Object.keys(visibility).filter((id) => visibility[id]),
    }
    localStorage.setItem(columnPreferenceKey(tableId), JSON.stringify(preference))
  } catch {
    // Quota or privacy mode: column choice is a convenience, not required state.
  }
}

export function resetColumnVisibility(tableId: string): void {
  if (typeof localStorage === 'undefined') return
  try {
    localStorage.removeItem(columnPreferenceKey(tableId))
  } catch {
    // Ignore unavailable storage.
  }
}
