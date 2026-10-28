export type { LookupEntryResponse, LookupCatalogEntry } from './model/types'
export {
  LOOKUP_SLUGS,
  USER_SCOPE_TYPE,
  ASSIGNMENT_TYPE,
  DEPARTMENT_TYPE,
  LOOKUP_FALLBACKS,
  findLookupFallback,
} from './model/constants'
export type { LookupSlug } from './model/constants'
export { lookupKeys, useLookupEntries } from './model/queries'
export { LookupLabel } from './ui/LookupLabel'
