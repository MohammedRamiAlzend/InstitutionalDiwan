import { z } from 'zod'
import { t } from '@/shared/lib'

/** Mirrors `DriveItem.MaxNameLength`. */
const ITEM_NAME_MAX = 255

/** Mirrors `DriveItem.ForbiddenNameChars` plus the reserved `.` / `..` names. */
const FORBIDDEN_NAME_CHARS = /[\\/:*?"<>|]/

/**
 * Create-folder and rename share one shape: a trimmed, non-empty name within the server limit that
 * carries no forbidden character and is not the reserved `.` or `..`. The server re-checks all of
 * this; the client mirrors it so the form fails before a round-trip.
 */
export const itemNameSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, t('drive.validation.name-required'))
    .max(ITEM_NAME_MAX, t('drive.validation.name-max'))
    .refine(
      (value) => value !== '.' && value !== '..' && !FORBIDDEN_NAME_CHARS.test(value),
      t('drive.validation.name-invalid'),
    ),
})

export type ItemNameFormValues = z.infer<typeof itemNameSchema>

/** The move dialog only picks a destination folder (or the root); the item id rides in the hook. */
export const moveItemSchema = z.object({
  destinationId: z.string(),
})

export type MoveItemFormValues = z.infer<typeof moveItemSchema>