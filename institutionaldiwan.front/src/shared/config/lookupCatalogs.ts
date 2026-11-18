import { FEATURES } from './features'
import type { LabelKey } from './labels'

/** Display metadata for the reference catalogs administered on `/lookups`.
 * `labelKey` is null for a slug added server-side that has no built-in name. */
export interface LookupCatalogMeta {
  labelKey: LabelKey | null
  groupKey: LabelKey
  /** `false` when the server rejects `POST` (`Lookup.CatalogReadOnly`). */
  canCreate: boolean
}

export const LOOKUP_GROUP_OTHER: LabelKey = 'lookup.group.other'

/** Group display order; unknown groups fall to the end. */
export const LOOKUP_GROUP_ORDER: readonly LabelKey[] = [
  'lookup.group.organization',
  'lookup.group.identity',
  'lookup.group.correspondence',
  'lookup.group.numbering',
  'lookup.group.movements',
  'lookup.group.attachments',
  'lookup.group.archiving',
  'lookup.group.relations',
  'lookup.group.audit',
  'lookup.group.notifications',
  LOOKUP_GROUP_OTHER,
]

const meta = (labelKey: LabelKey, groupKey: LabelKey, canCreate = true): LookupCatalogMeta => ({
  labelKey,
  groupKey,
  canCreate,
})

/** Keys mirror `LookupCatalog.Definitions` on the server. */
export const LOOKUP_CATALOG_META: Record<string, LookupCatalogMeta> = {
  'department-types': meta('lookup.catalog.department-types', 'lookup.group.organization'),
  'numbering-modes': meta('lookup.catalog.numbering-modes', 'lookup.group.organization'),
  'user-scope-types': meta('lookup.catalog.user-scope-types', 'lookup.group.identity'),
  'assignment-types': meta('lookup.catalog.assignment-types', 'lookup.group.identity'),
  'access-levels': meta('lookup.catalog.access-levels', 'lookup.group.identity'),
  'document-directions': meta('lookup.catalog.document-directions', 'lookup.group.correspondence'),
  'document-types': meta('lookup.catalog.document-types', 'lookup.group.correspondence'),
  'document-status-types': meta('lookup.catalog.document-status-types', 'lookup.group.correspondence'),
  'document-confidentiality-levels': meta('lookup.catalog.document-confidentiality-levels', 'lookup.group.correspondence'),
  'document-priority-levels': meta('lookup.catalog.document-priority-levels', 'lookup.group.correspondence'),
  'participant-roles': meta('lookup.catalog.participant-roles', 'lookup.group.correspondence'),
  'participant-target-types': meta('lookup.catalog.participant-target-types', 'lookup.group.correspondence'),
  'organization-types': meta('lookup.catalog.organization-types', 'lookup.group.correspondence'),
  'document-person-roles': meta('lookup.catalog.document-person-roles', 'lookup.group.correspondence'),
  genders: meta('lookup.catalog.genders', 'lookup.group.correspondence'),
  nationalities: meta('lookup.catalog.nationalities', 'lookup.group.correspondence'),
  'document-number-statuses': meta('lookup.catalog.document-number-statuses', 'lookup.group.numbering'),
  'document-number-event-types': meta('lookup.catalog.document-number-event-types', 'lookup.group.numbering'),
  'numbering-reset-policies': meta('lookup.catalog.numbering-reset-policies', 'lookup.group.numbering'),
  'document-number-scopes': meta('lookup.catalog.document-number-scopes', 'lookup.group.numbering'),
  'movement-types': meta('lookup.catalog.movement-types', 'lookup.group.movements'),
  'movement-statuses': meta('lookup.catalog.movement-statuses', 'lookup.group.movements'),
  'movement-step-statuses': meta('lookup.catalog.movement-step-statuses', 'lookup.group.movements'),
  'movement-event-types': meta('lookup.catalog.movement-event-types', 'lookup.group.movements'),
  'storage-provider-types': meta('lookup.catalog.storage-provider-types', 'lookup.group.attachments'),
  'document-file-types': meta('lookup.catalog.document-file-types', 'lookup.group.attachments'),
  'file-storage-providers': meta('lookup.catalog.file-storage-providers', 'lookup.group.attachments', false),
  'archive-event-types': meta('lookup.catalog.archive-event-types', 'lookup.group.archiving'),
  'archive-locations': meta('lookup.catalog.archive-locations', 'lookup.group.archiving'),
  'document-relation-types': meta('lookup.catalog.document-relation-types', 'lookup.group.relations'),
  'relation-action-types': meta('lookup.catalog.relation-action-types', 'lookup.group.relations'),
  'audit-action-types': meta('lookup.catalog.audit-action-types', 'lookup.group.audit'),
  'audit-entity-types': meta('lookup.catalog.audit-entity-types', 'lookup.group.audit'),
  'notification-types': meta('lookup.catalog.notification-types', 'lookup.group.notifications', false),
  'notification-priorities': meta('lookup.catalog.notification-priorities', 'lookup.group.notifications'),
  'notification-channels': meta('lookup.catalog.notification-channels', 'lookup.group.notifications'),
  'notification-outbox-statuses': meta('lookup.catalog.notification-outbox-statuses', 'lookup.group.notifications'),
}

/** Unknown slugs (a catalog added server-side) still show, under «أخرى». */
export function lookupCatalogMeta(slug: string): LookupCatalogMeta {
  return LOOKUP_CATALOG_META[slug] ?? { labelKey: null, groupKey: LOOKUP_GROUP_OTHER, canCreate: true }
}

/** False for a catalog that belongs to a suspended part of the product. */
export function isLookupCatalogEnabled(slug: string): boolean {
  return slug !== 'organization-types' || FEATURES.externalOrganizations
}