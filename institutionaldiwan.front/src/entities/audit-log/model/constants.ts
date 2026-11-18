import type { LabelKey } from '@/shared/config'
import { t } from '@/shared/lib'

/**
 * Module names written by the API (`AuditLog.ModuleName`, uppercased by the
 * domain). The list is static because modules are code, not a lookup catalog.
 */
export const AUDIT_MODULES = [
  { code: 'IDENTITY', labelKey: 'audit.module.identity' },
  { code: 'ORGANIZATION', labelKey: 'audit.module.organization' },
  { code: 'CORRESPONDENCE', labelKey: 'audit.module.correspondence' },
  { code: 'NUMBERING', labelKey: 'audit.module.numbering' },
  { code: 'MOVEMENTS', labelKey: 'audit.module.movements' },
  { code: 'RELATIONS', labelKey: 'audit.module.relations' },
  { code: 'ATTACHMENTS', labelKey: 'audit.module.attachments' },
  { code: 'ARCHIVING', labelKey: 'audit.module.archiving' },
  { code: 'AUDIT', labelKey: 'audit.module.audit' },
  { code: 'LOOKUPS', labelKey: 'audit.module.lookups' },
] as const satisfies readonly { code: string; labelKey: LabelKey }[]

/** Arabic module label, falling back to the raw API value for unknown modules. */
export function auditModuleLabel(moduleName: string): string {
  const module = AUDIT_MODULES.find((entry) => entry.code === moduleName)
  return module ? t(module.labelKey) : moduleName
}