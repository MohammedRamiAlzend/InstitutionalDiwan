import {
  ASSIGNMENT_TYPE,
  DEPARTMENT_TYPE,
  DOCUMENT_CONFIDENTIALITY,
  DOCUMENT_DIRECTION,
  DOCUMENT_PRIORITY,
  DOCUMENT_STATUS,
  DOCUMENT_TYPE,
  NUMBERING_MODE,
  NUMBERING_RESET_POLICY,
  USER_SCOPE_TYPE,
} from '@/shared/config'
import type { LookupCatalogEntry } from './types'

export {
  ASSIGNMENT_TYPE,
  DEPARTMENT_TYPE,
  DOCUMENT_CONFIDENTIALITY,
  DOCUMENT_DIRECTION,
  DOCUMENT_PRIORITY,
  DOCUMENT_STATUS,
  DOCUMENT_TYPE,
  DOCUMENT_TYPE_DIRECTION,
  NUMBERING_MODE,
  NUMBERING_RESET_POLICY,
  USER_SCOPE_TYPE,
} from '@/shared/config'

/** Catalogs Module 01/02/03 (pickers, settings, and document forms) consume directly. */
export const LOOKUP_SLUGS = {
  DepartmentTypes: 'department-types',
  UserScopeTypes: 'user-scope-types',
  AssignmentTypes: 'assignment-types',
  AccessLevels: 'access-levels',
  NumberingModes: 'numbering-modes',
  NumberingResetPolicies: 'numbering-reset-policies',
  DocumentDirections: 'document-directions',
  DocumentTypes: 'document-types',
  DocumentStatusTypes: 'document-status-types',
  DocumentConfidentialityLevels: 'document-confidentiality-levels',
  DocumentPriorityLevels: 'document-priority-levels',
} as const

export type LookupSlug = (typeof LOOKUP_SLUGS)[keyof typeof LOOKUP_SLUGS]

/**
 * Seeded catalogs mirroring `InstitutionalDiwan.Infrastructure/Persistence/Seed`.
 * They keep Arabic labels usable before the first response, or when the caller
 * lacks the lookup permission.
 */
export const LOOKUP_FALLBACKS: Record<string, readonly LookupCatalogEntry[]> = {
  'user-scope-types': [
    { id: USER_SCOPE_TYPE.Department, desc: 'Own department', descAr: 'الدائرة' },
    { id: USER_SCOPE_TYPE.DepartmentTree, desc: 'Department and sub-departments', descAr: 'الدائرة والفروع' },
    { id: USER_SCOPE_TYPE.Institution, desc: 'Whole institution', descAr: 'المؤسسة' },
    { id: USER_SCOPE_TYPE.System, desc: 'System', descAr: 'النظام' },
  ],
  'assignment-types': [
    { id: ASSIGNMENT_TYPE.Primary, desc: 'Primary', descAr: 'أساسي' },
    { id: ASSIGNMENT_TYPE.Additional, desc: 'Additional', descAr: 'إضافي' },
  ],
  'department-types': [
    { id: DEPARTMENT_TYPE.Directorate, desc: 'Directorate', descAr: 'مديرية' },
    { id: DEPARTMENT_TYPE.Department, desc: 'Department', descAr: 'قسم' },
    { id: DEPARTMENT_TYPE.Section, desc: 'Section', descAr: 'شعبة' },
    { id: DEPARTMENT_TYPE.Unit, desc: 'Unit', descAr: 'وحدة' },
    { id: DEPARTMENT_TYPE.CentralDiwan, desc: 'Central Diwan', descAr: 'الديوان المركزي' },
  ],
  'access-levels': [
    { id: 1, desc: 'View', descAr: 'عرض' },
    { id: 2, desc: 'Download', descAr: 'تنزيل' },
    { id: 3, desc: 'Forward', descAr: 'إحالة' },
    { id: 4, desc: 'Manage', descAr: 'إدارة' },
  ],
  'numbering-modes': [
    { id: NUMBERING_MODE.Automatic, desc: 'Automatic', descAr: 'تلقائي' },
    { id: NUMBERING_MODE.Manual, desc: 'Manual', descAr: 'يدوي' },
  ],
  'numbering-reset-policies': [
    { id: NUMBERING_RESET_POLICY.Yearly, desc: 'Yearly', descAr: 'سنوي' },
    { id: NUMBERING_RESET_POLICY.Monthly, desc: 'Monthly', descAr: 'شهري' },
    { id: NUMBERING_RESET_POLICY.Never, desc: 'Never', descAr: 'لا يتصفّر' },
  ],
  'document-directions': [
    { id: DOCUMENT_DIRECTION.Internal, desc: 'Internal', descAr: 'داخلي' },
    { id: DOCUMENT_DIRECTION.Incoming, desc: 'Incoming', descAr: 'وارد' },
    { id: DOCUMENT_DIRECTION.Outgoing, desc: 'Outgoing', descAr: 'صادر' },
    { id: DOCUMENT_DIRECTION.All, desc: 'All directions', descAr: 'كل الاتجاهات' },
  ],
  'document-types': [
    { id: DOCUMENT_TYPE.IncomingLetter, desc: 'Incoming letter', descAr: 'كتاب وارد' },
    { id: DOCUMENT_TYPE.OutgoingLetter, desc: 'Outgoing letter', descAr: 'كتاب صادر' },
    { id: DOCUMENT_TYPE.InternalMemo, desc: 'Internal memo', descAr: 'مذكرة داخلية' },
    { id: DOCUMENT_TYPE.Circular, desc: 'Circular', descAr: 'تعميم' },
    { id: DOCUMENT_TYPE.Decision, desc: 'Decision', descAr: 'قرار' },
  ],
  'document-status-types': [
    { id: DOCUMENT_STATUS.Draft, desc: 'Draft', descAr: 'مسودة' },
    { id: DOCUMENT_STATUS.Registered, desc: 'Registered', descAr: 'مسجل' },
    { id: DOCUMENT_STATUS.InMovement, desc: 'In movement', descAr: 'قيد التداول' },
    { id: DOCUMENT_STATUS.Archived, desc: 'Archived', descAr: 'مؤرشف' },
    { id: DOCUMENT_STATUS.Canceled, desc: 'Canceled', descAr: 'ملغي' },
  ],
  'document-confidentiality-levels': [
    { id: DOCUMENT_CONFIDENTIALITY.Normal, desc: 'Normal', descAr: 'عادي' },
    { id: DOCUMENT_CONFIDENTIALITY.Confidential, desc: 'Confidential', descAr: 'سري' },
    { id: DOCUMENT_CONFIDENTIALITY.Secret, desc: 'Secret', descAr: 'سري للغاية' },
    { id: DOCUMENT_CONFIDENTIALITY.TopSecret, desc: 'Top secret', descAr: 'بالغ السرية' },
  ],
  'document-priority-levels': [
    { id: DOCUMENT_PRIORITY.Normal, desc: 'Normal', descAr: 'عادي' },
    { id: DOCUMENT_PRIORITY.Urgent, desc: 'Urgent', descAr: 'عاجل' },
    { id: DOCUMENT_PRIORITY.Immediate, desc: 'Immediate', descAr: 'فوري' },
  ],
}

/** Looks up a seeded label when the catalog response has not arrived. */
export function findLookupFallback(
  lookup: string,
  id: number,
): LookupCatalogEntry | undefined {
  return LOOKUP_FALLBACKS[lookup]?.find((entry) => entry.id === id)
}
