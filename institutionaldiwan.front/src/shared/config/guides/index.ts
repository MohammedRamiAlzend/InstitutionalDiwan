import { COMMON_GUIDE_ENTRIES } from './common'
import { USERS_GUIDE, USERS_GUIDE_ENTRIES } from './users'
import type { GuideEntry, PageGuide } from './types'

const ALL_ENTRIES: readonly GuideEntry[] = [
  ...COMMON_GUIDE_ENTRIES,
  ...USERS_GUIDE_ENTRIES,
]

const ALL_PAGES: readonly PageGuide[] = [USERS_GUIDE]

const entriesById = new Map(ALL_ENTRIES.map((entry) => [entry.id, entry]))
const pagesById = new Map(ALL_PAGES.map((page) => [page.pageId, page]))

/** Explains one visible part of the interface; undefined ids render no help control. */
export function getGuideEntry(id: string | undefined): GuideEntry | undefined {
  return id === undefined ? undefined : entriesById.get(id)
}

export function getPageGuide(pageId: string): PageGuide | undefined {
  return pagesById.get(pageId)
}

export type { GuideEntry, PageGuide } from './types'
