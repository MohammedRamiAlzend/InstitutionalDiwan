import {
  ASSIGNMENT_TYPE,
  DEPARTMENT_TYPE,
  USER_SCOPE_TYPE,
} from '@/shared/config'
import type { LookupCatalogEntry } from './types'

export { ASSIGNMENT_TYPE, DEPARTMENT_TYPE, USER_SCOPE_TYPE }

/** Catalogs Module 02 (and its department pickers) consumes directly. */
export const LOOKUP_SLUGS = {
  DepartmentTypes: 'department-types',
  UserScopeTypes: 'user-scope-types',
  AssignmentTypes: 'assignment-types',
  AccessLevels: 'access-levels',
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
}

/** Looks up a seeded label when the catalog response has not arrived. */
export function findLookupFallback(
  lookup: string,
  id: number,
): LookupCatalogEntry | undefined {
  return LOOKUP_FALLBACKS[lookup]?.find((entry) => entry.id === id)
}
