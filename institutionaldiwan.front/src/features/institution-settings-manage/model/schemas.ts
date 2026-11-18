import { z } from 'zod'
import { t } from '@/shared/lib'

/** Matches the EF limits and the server's FluentValidation rules. */
export const institutionSettingsSchema = z.object({
  institutionNameAr: z
    .string()
    .trim()
    .min(1, t('institution.validation.name-ar-required'))
    .max(200, t('institution.validation.name-ar-max')),
  institutionNameEn: z
    .string()
    .trim()
    .max(200, t('institution.validation.name-en-max')),
  defaultTimezone: z
    .string()
    .trim()
    .min(1, t('institution.validation.timezone-required'))
    .max(100, t('institution.validation.timezone-max')),
  numberingModeId: z.string().min(1, t('institution.validation.mode-required')),
  numberingResetPolicyId: z.string().min(1, t('institution.validation.reset-required')),
  centralDiwanDepartmentId: z.string().min(1, t('institution.validation.central-required')),
  responsibleEmployeeId: z.string(),
  /** A department id, or the form's "none" sentinel. */
  archiveDiwanDepartmentId: z.string(),
  /** False reserves document creation to the Central Diwan. */
  otherDiwansMayCreateDocuments: z.boolean(),
  centralDiwanTransferReason: z
    .string()
    .trim()
    .max(1000, t('institution.validation.transfer-max')),
})

export type InstitutionSettingsFormValues = z.infer<typeof institutionSettingsSchema>
