/**
 * Module names written by the API (`AuditLog.ModuleName`, uppercased by the
 * domain). The list is static because modules are code, not a lookup catalog.
 */
export const AUDIT_MODULES = [
  { code: 'IDENTITY', label: 'الهوية والوصول' },
  { code: 'ORGANIZATION', label: 'التنظيم' },
  { code: 'CORRESPONDENCE', label: 'المراسلات' },
  { code: 'NUMBERING', label: 'الترقيم' },
  { code: 'MOVEMENTS', label: 'الحركات' },
  { code: 'RELATIONS', label: 'ربط الوثائق' },
  { code: 'ATTACHMENTS', label: 'المرفقات' },
  { code: 'ARCHIVING', label: 'الأرشفة' },
  { code: 'AUDIT', label: 'التدقيق' },
  { code: 'LOOKUPS', label: 'القوائم المرجعية' },
] as const

/** Arabic module label, falling back to the raw API value for unknown modules. */
export function auditModuleLabel(moduleName: string): string {
  return AUDIT_MODULES.find((module) => module.code === moduleName)?.label ?? moduleName
}
