import { COMMON_LABELS } from './common'
import { NAV_LABELS } from './nav'
import { PAGE_LABELS } from './pages'
import { ERROR_LABELS } from './errors'
import { DOCUMENT_LABELS } from './documents'
import { MOVEMENT_LABELS } from './movements'
import { DECISION_LABELS } from './decisions'
import { MY_REGISTERS_LABELS } from './my-registers'
import { PERSONS_LABELS } from './persons'
import { ARCHIVE_LABELS } from './archive'
import { DEPARTMENTS_LABELS } from './departments'
import { INSTITUTION_SETTINGS_LABELS } from './institution-settings'
import { EXTERNAL_ORGANIZATIONS_LABELS } from './external-organizations'
import { ACCESS_LABELS } from './access'
import { SYSTEM_ERRORS_LABELS } from './system-errors'
import { AUDIT_LABELS } from './audit'
import { LOOKUPS_LABELS } from './lookups'
import { NUMBERING_LABELS } from './numbering'
import { DOCUMENT_SCHEMAS_LABELS } from './document-schemas'
import { DOCUMENT_DETAILS_LABELS } from './document-details'
import { ATTACHMENTS_LABELS } from './attachments'
import { RELATIONS_LABELS } from './relations'
import { AUTH_LABELS } from './auth'
import { DASHBOARD_LABELS } from './dashboard'
import { NOTIFICATIONS_LABELS } from './notifications'
import { PRINT_LABELS } from './print'
import { GUIDE_LABELS } from './guides'
import { LABEL_EDITOR_LABELS } from './label-editor'

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
  ...DOCUMENT_LABELS,
  ...MOVEMENT_LABELS,
  ...DECISION_LABELS,
  ...MY_REGISTERS_LABELS,
  ...PERSONS_LABELS,
  ...ARCHIVE_LABELS,
  ...DEPARTMENTS_LABELS,
  ...INSTITUTION_SETTINGS_LABELS,
  ...EXTERNAL_ORGANIZATIONS_LABELS,
  ...ACCESS_LABELS,
  ...SYSTEM_ERRORS_LABELS,
  ...AUDIT_LABELS,
  ...LOOKUPS_LABELS,
  ...NUMBERING_LABELS,
  ...DOCUMENT_SCHEMAS_LABELS,
  ...DOCUMENT_DETAILS_LABELS,
  ...ATTACHMENTS_LABELS,
  ...RELATIONS_LABELS,
  ...AUTH_LABELS,
  ...DASHBOARD_LABELS,
  ...NOTIFICATIONS_LABELS,
  ...PRINT_LABELS,
  ...GUIDE_LABELS,
  ...LABEL_EDITOR_LABELS,
})

/** A catalog key; using a key that is not declared here is a build error. */
export type LabelKey = keyof typeof LABEL_DEFAULTS