export const RESTRICTED_DOCUMENT_LABEL = 'وثيقة مقيّدة'

/** Queues and registers keep rows the caller cannot view, with the document's title blanked. */
export function isDocumentRestricted(documentTitle: string): boolean {
  return documentTitle === ''
}

/** The text a row is searched and announced by: its title, or the restricted label. */
export function documentRowLabel(documentTitle: string): string {
  return isDocumentRestricted(documentTitle) ? RESTRICTED_DOCUMENT_LABEL : documentTitle
}
