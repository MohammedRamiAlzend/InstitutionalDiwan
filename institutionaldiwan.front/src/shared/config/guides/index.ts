import { ARCHIVE_GUIDE, ARCHIVE_GUIDE_ENTRIES } from './archive'
import { AUDIT_GUIDE, AUDIT_GUIDE_ENTRIES } from './audit'
import { COMMON_GUIDE_ENTRIES } from './common'
import { DASHBOARD_GUIDE, DASHBOARD_GUIDE_ENTRIES } from './dashboard'
import { DEPARTMENTS_GUIDE, DEPARTMENTS_GUIDE_ENTRIES } from './departments'
import {
  DOCUMENT_DETAILS_GUIDE,
  DOCUMENT_DETAILS_GUIDE_ENTRIES,
  DOCUMENTS_GUIDE,
  DOCUMENTS_GUIDE_ENTRIES,
} from './documents'
import {
  DOCUMENT_SCHEMAS_GUIDE,
  DOCUMENT_SCHEMAS_GUIDE_ENTRIES,
} from './document-schemas'
import { EMPLOYEES_GUIDE, EMPLOYEES_GUIDE_ENTRIES } from './employees'
import {
  INSTITUTION_SETTINGS_GUIDE,
  INSTITUTION_SETTINGS_GUIDE_ENTRIES,
} from './institution-settings'
import { MOVEMENTS_GUIDE, MOVEMENTS_GUIDE_ENTRIES } from './movements'
import { LOOKUPS_GUIDE, LOOKUPS_GUIDE_ENTRIES } from './lookups'
import { NOTIFICATIONS_GUIDE, NOTIFICATIONS_GUIDE_ENTRIES } from './notifications'
import { NUMBERING_GUIDE, NUMBERING_GUIDE_ENTRIES } from './numbering'
import { ROLES_GUIDE, ROLES_GUIDE_ENTRIES } from './roles'
import { USERS_GUIDE, USERS_GUIDE_ENTRIES } from './users'
import type { GuideEntry, PageGuide } from './types'

const ALL_ENTRIES: readonly GuideEntry[] = [
  ...COMMON_GUIDE_ENTRIES,
  ...USERS_GUIDE_ENTRIES,
  ...ROLES_GUIDE_ENTRIES,
  ...EMPLOYEES_GUIDE_ENTRIES,
  ...MOVEMENTS_GUIDE_ENTRIES,
  ...DOCUMENTS_GUIDE_ENTRIES,
  ...DOCUMENT_DETAILS_GUIDE_ENTRIES,
  ...DOCUMENT_SCHEMAS_GUIDE_ENTRIES,
  ...DEPARTMENTS_GUIDE_ENTRIES,
  ...NUMBERING_GUIDE_ENTRIES,
  ...INSTITUTION_SETTINGS_GUIDE_ENTRIES,
  ...ARCHIVE_GUIDE_ENTRIES,
  ...LOOKUPS_GUIDE_ENTRIES,
  ...NOTIFICATIONS_GUIDE_ENTRIES,
  ...AUDIT_GUIDE_ENTRIES,
  ...DASHBOARD_GUIDE_ENTRIES,
]

const ALL_PAGES: readonly PageGuide[] = [
  USERS_GUIDE,
  ROLES_GUIDE,
  EMPLOYEES_GUIDE,
  MOVEMENTS_GUIDE,
  DOCUMENTS_GUIDE,
  DOCUMENT_DETAILS_GUIDE,
  DOCUMENT_SCHEMAS_GUIDE,
  DEPARTMENTS_GUIDE,
  NUMBERING_GUIDE,
  INSTITUTION_SETTINGS_GUIDE,
  ARCHIVE_GUIDE,
  LOOKUPS_GUIDE,
  NOTIFICATIONS_GUIDE,
  AUDIT_GUIDE,
  DASHBOARD_GUIDE,
]

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
