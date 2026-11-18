import { t } from '@/shared/lib'

/** Queues and registers keep rows the caller cannot view, with the document's title blanked. */
export function isDocumentRestricted(documentTitle: string): boolean {
  return documentTitle === ''
}

/** The text a row is searched and announced by: its title, or the restricted label. */
export function documentRowLabel(documentTitle: string): string {
  return isDocumentRestricted(documentTitle) ? t('documents.restricted.label') : documentTitle
}