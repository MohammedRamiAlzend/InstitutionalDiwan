import { COMMON_LABELS } from './common'
import { NAV_LABELS } from './nav'
import { PAGE_LABELS } from './pages'
import { ERROR_LABELS } from './errors'

/**
 * The built-in Arabic defaults for every editable UI label. The app renders fully
 * from this catalog with no server and no overrides; the server stores only what an
 * administrator changed. New area files (documents.ts, …) are spread in here as the
 * migration phases land.
 */
export const LABEL_DEFAULTS = Object.freeze({
  ...COMMON_LABELS,
  ...NAV_LABELS,
  ...PAGE_LABELS,
  ...ERROR_LABELS,
})

/** A catalog key; using a key that is not declared here is a build error. */
export type LabelKey = keyof typeof LABEL_DEFAULTS