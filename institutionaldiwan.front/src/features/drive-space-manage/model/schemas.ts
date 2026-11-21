import { z } from 'zod'
import { t } from '@/shared/lib'

/** Mirrors `DriveSpace.MaxNameLength`. */
const SPACE_NAME_MAX = 150

/** Create and rename share one shape: a non-empty trimmed name within the server limit. */
export const spaceNameSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, t('drive.validation.space-name-required'))
    .max(SPACE_NAME_MAX, t('drive.validation.space-name-max')),
})

export type SpaceNameFormValues = z.infer<typeof spaceNameSchema>

export const QUOTA_UNITS = ['bytes', 'kb', 'mb', 'gb'] as const
export type QuotaUnit = (typeof QUOTA_UNITS)[number]

export const QUOTA_UNIT_BYTES: Record<QuotaUnit, number> = {
  bytes: 1,
  kb: 1024,
  mb: 1024 ** 2,
  gb: 1024 ** 3,
}

/**
 * A quota override is a positive whole number in the chosen unit, or empty to fall back to the
 * institution default. Kept as a string so an empty field is distinguishable from zero.
 */
export const spaceQuotaSchema = z.object({
  amount: z
    .string()
    .trim()
    .refine(
      (value) => value === '' || (/^\d+$/.test(value) && Number(value) > 0),
      t('drive.validation.quota-positive'),
    ),
  unit: z.enum(QUOTA_UNITS),
})

export type SpaceQuotaFormValues = z.infer<typeof spaceQuotaSchema>

/** Converts the dialog's amount+unit into the byte count the API expects (`null` = default). */
export function quotaFormToBytes(values: SpaceQuotaFormValues): number | null {
  if (values.amount === '') return null
  return Number(values.amount) * QUOTA_UNIT_BYTES[values.unit]
}