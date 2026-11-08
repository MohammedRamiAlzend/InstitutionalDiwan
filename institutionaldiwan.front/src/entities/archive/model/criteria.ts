/**
 * The URL filter keys a saved search remembers. They are the archive grid's own search params, so
 * restoring a search is just writing them back into the URL — the server stores the JSON but never
 * runs it. `f` holds the repeated field filters joined by {@link FIELD_FILTER_SEPARATOR}.
 */
export const ARCHIVE_CRITERIA_KEYS = [
  'search',
  'sort',
  'dir',
  'owningDepartmentId',
  'documentTypeId',
  'archiveLocationId',
  'archivedFrom',
  'archivedTo',
  'f',
] as const

export const FIELD_FILTER_SEPARATOR = '\n'

export type ArchiveCriteriaKey = (typeof ARCHIVE_CRITERIA_KEYS)[number]
export type ArchiveCriteria = Partial<Record<ArchiveCriteriaKey, string>>

/** Serialises only the non-empty known keys, so the stored JSON stays small and stable. */
export function serializeArchiveCriteria(
  source: Partial<Record<ArchiveCriteriaKey, string | null | undefined>>,
): string {
  const criteria: ArchiveCriteria = {}
  for (const key of ARCHIVE_CRITERIA_KEYS) {
    const value = source[key]
    if (value) criteria[key] = value
  }
  return JSON.stringify(criteria)
}

/** Parses stored criteria defensively: anything unreadable becomes an empty search. */
export function parseArchiveCriteria(criteriaJson: string): ArchiveCriteria {
  try {
    const parsed: unknown = JSON.parse(criteriaJson)
    if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) return {}

    const record = parsed as Record<string, unknown>
    const criteria: ArchiveCriteria = {}
    for (const key of ARCHIVE_CRITERIA_KEYS) {
      const value = record[key]
      if (typeof value === 'string' && value) criteria[key] = value
    }
    return criteria
  } catch {
    return {}
  }
}

/** The URL search params that restore a saved search. */
export function archiveCriteriaToParams(criteria: ArchiveCriteria): URLSearchParams {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(criteria)) {
    if (key === 'f') for (const filter of value.split(FIELD_FILTER_SEPARATOR)) params.append('f', filter)
    else params.set(key, value)
  }
  return params
}

/** The saved-search source of the current URL; repeated field filters collapse into one value. */
export function archiveCriteriaFromParams(
  params: URLSearchParams,
): Partial<Record<ArchiveCriteriaKey, string | null>> {
  return Object.fromEntries(
    ARCHIVE_CRITERIA_KEYS.map((key) => [
      key,
      key === 'f' ? params.getAll('f').join(FIELD_FILTER_SEPARATOR) : params.get(key),
    ]),
  )
}
