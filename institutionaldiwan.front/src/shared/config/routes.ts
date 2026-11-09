/** Typed route builders — no magic strings in guards, links, or redirects. */
export const ROUTES = {
  LOGIN: '/login',
  CHANGE_PASSWORD: '/change-password',
  DASHBOARD: '/',
  DOCUMENTS: '/documents',
  DOCUMENT_DETAIL: (id: string) => `/documents/${id}`,
  /** Route pattern consumed by `AppRouter`; keep in sync with `DOCUMENT_DETAIL`. */
  DOCUMENT_DETAIL_PATTERN: '/documents/:documentId',
  MOVEMENTS: '/movements',
  ARCHIVE: '/archive',
  NOTIFICATIONS: '/notifications',
  LOOKUPS: '/lookups',
  DEPARTMENTS: '/departments',
  INSTITUTION_SETTINGS: '/institution-settings',
  USERS: '/users',
  ROLES: '/roles',
  EMPLOYEES: '/employees',
  NUMBERING: '/numbering',
  DOCUMENT_SCHEMAS: '/document-schemas',
  AUDIT: '/audit',
  SYSTEM_ERRORS: '/system-errors',
  PERSONS: '/persons',
  PERSON_DETAIL: (id: string) => `/persons/${id}`,
  /** Route pattern consumed by `AppRouter`; keep in sync with `PERSON_DETAIL`. */
  PERSON_DETAIL_PATTERN: '/persons/:personId',
} as const

export type RoutePath = (typeof ROUTES)[keyof typeof ROUTES] | string
