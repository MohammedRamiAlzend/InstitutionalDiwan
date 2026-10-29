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
  DEPARTMENTS: '/departments',
  INSTITUTION_SETTINGS: '/institution-settings',
  USERS: '/users',
  ROLES: '/roles',
  EMPLOYEES: '/employees',
  NUMBERING: '/numbering',
  AUDIT: '/audit',
} as const

export type RoutePath = (typeof ROUTES)[keyof typeof ROUTES] | string
