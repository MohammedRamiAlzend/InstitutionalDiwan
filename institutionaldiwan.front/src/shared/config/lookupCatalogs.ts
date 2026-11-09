import { FEATURES } from './features'

/** Display metadata for the reference catalogs administered on `/lookups`. */
export interface LookupCatalogMeta {
  labelAr: string
  group: string
  /** `false` when the server rejects `POST` (`Lookup.CatalogReadOnly`). */
  canCreate: boolean
}

export const LOOKUP_GROUP_OTHER = 'أخرى'

/** Group display order; unknown groups fall to the end. */
export const LOOKUP_GROUP_ORDER: readonly string[] = [
  'الهيكل التنظيمي',
  'الهوية والصلاحيات',
  'المراسلات',
  'الترقيم',
  'الإحالات',
  'المرفقات',
  'الأرشفة',
  'العلاقات',
  'التدقيق',
  'الإشعارات',
  LOOKUP_GROUP_OTHER,
]

const meta = (labelAr: string, group: string, canCreate = true): LookupCatalogMeta => ({
  labelAr,
  group,
  canCreate,
})

/** Keys mirror `LookupCatalog.Definitions` on the server. */
export const LOOKUP_CATALOG_META: Record<string, LookupCatalogMeta> = {
  'department-types': meta('أنواع الدوائر', 'الهيكل التنظيمي'),
  'numbering-modes': meta('أنماط الترقيم', 'الهيكل التنظيمي'),
  'user-scope-types': meta('نطاقات المستخدمين', 'الهوية والصلاحيات'),
  'assignment-types': meta('أنواع التكليف', 'الهوية والصلاحيات'),
  'access-levels': meta('مستويات الوصول', 'الهوية والصلاحيات'),
  'document-directions': meta('اتجاهات الوثيقة', 'المراسلات'),
  'document-types': meta('أنواع الوثائق', 'المراسلات'),
  'document-status-types': meta('حالات الوثيقة', 'المراسلات'),
  'document-confidentiality-levels': meta('مستويات السرية', 'المراسلات'),
  'document-priority-levels': meta('مستويات الأولوية', 'المراسلات'),
  'participant-roles': meta('أدوار المشاركين', 'المراسلات'),
  'participant-target-types': meta('أنواع أهداف المشاركين', 'المراسلات'),
  'organization-types': meta('أنواع الجهات', 'المراسلات'),
  'document-person-roles': meta('صفات الأشخاص في الوثائق', 'المراسلات'),
  genders: meta('الجنس', 'المراسلات'),
  nationalities: meta('الجنسيات', 'المراسلات'),
  'document-number-statuses': meta('حالات رقم الوثيقة', 'الترقيم'),
  'document-number-event-types': meta('أحداث رقم الوثيقة', 'الترقيم'),
  'numbering-reset-policies': meta('سياسات إعادة الترقيم', 'الترقيم'),
  'document-number-scopes': meta('نطاقات الترقيم', 'الترقيم'),
  'movement-types': meta('أنواع الإحالة', 'الإحالات'),
  'movement-statuses': meta('حالات الإحالة', 'الإحالات'),
  'movement-step-statuses': meta('حالات خطوة الإحالة', 'الإحالات'),
  'movement-event-types': meta('أحداث الإحالة', 'الإحالات'),
  'storage-provider-types': meta('أنواع مزوّدي التخزين', 'المرفقات'),
  'document-file-types': meta('أنواع الملفات', 'المرفقات'),
  'file-storage-providers': meta('مزوّدو تخزين الملفات', 'المرفقات', false),
  'archive-event-types': meta('أحداث الأرشفة', 'الأرشفة'),
  'archive-locations': meta('مواقع الأرشفة', 'الأرشفة'),
  'document-relation-types': meta('أنواع علاقات الوثائق', 'العلاقات'),
  'relation-action-types': meta('إجراءات العلاقات', 'العلاقات'),
  'audit-action-types': meta('أنواع إجراءات التدقيق', 'التدقيق'),
  'audit-entity-types': meta('أنواع كيانات التدقيق', 'التدقيق'),
  'notification-types': meta('أنواع الإشعارات', 'الإشعارات', false),
  'notification-priorities': meta('أولويات الإشعارات', 'الإشعارات'),
  'notification-channels': meta('قنوات الإشعارات', 'الإشعارات'),
  'notification-outbox-statuses': meta('حالات صندوق الإرسال', 'الإشعارات'),
}

/** Unknown slugs (a catalog added server-side) still show, under «أخرى». */
export function lookupCatalogMeta(slug: string): LookupCatalogMeta {
  return LOOKUP_CATALOG_META[slug] ?? meta(slug, LOOKUP_GROUP_OTHER)
}

/** False for a catalog that belongs to a suspended part of the product. */
export function isLookupCatalogEnabled(slug: string): boolean {
  return slug !== 'organization-types' || FEATURES.externalOrganizations
}
