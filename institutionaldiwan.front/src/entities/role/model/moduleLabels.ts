/** Arabic labels for the permission catalog's `moduleName` values. */
export const PERMISSION_MODULE_LABELS: Record<string, string> = {
  IDENTITY: 'الهوية والوصول',
  ORGANIZATION: 'التنظيم',
  CORRESPONDENCE: 'المراسلات',
  NUMBERING: 'الترقيم',
  MOVEMENTS: 'الحركات',
  ATTACHMENTS: 'المرفقات',
  AUDIT: 'التدقيق',
  LOOKUPS: 'القوائم المرجعية',
  REPORTING: 'التقارير',
}

/** Falls back to the raw module name when the API adds a module without a label. */
export function permissionModuleLabel(moduleName: string): string {
  return PERMISSION_MODULE_LABELS[moduleName] ?? moduleName
}
