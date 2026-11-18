import type { LabelKey } from '@/shared/config'
import { t } from '@/shared/lib'

/** Catalog keys for the permission catalog's `moduleName` values. */
export const PERMISSION_MODULE_LABELS: Record<string, LabelKey> = {
  IDENTITY: 'role.module.identity',
  ORGANIZATION: 'role.module.organization',
  CORRESPONDENCE: 'role.module.correspondence',
  NUMBERING: 'role.module.numbering',
  MOVEMENTS: 'role.module.movements',
  ATTACHMENTS: 'role.module.attachments',
  AUDIT: 'role.module.audit',
  LOOKUPS: 'role.module.lookups',
  REPORTING: 'role.module.reporting',
}

/** Falls back to the raw module name when the API adds a module without a label. */
export function permissionModuleLabel(moduleName: string): string {
  const key = PERMISSION_MODULE_LABELS[moduleName]
  return key ? t(key) : moduleName
}