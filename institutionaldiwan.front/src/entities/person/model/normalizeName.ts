const IGNORED_MARKS = /[ً-ٰٟـ]/g

/**
 * Folds Arabic spelling variants the way the server's `PersonNameNormalizer` does, so the input
 * catches a name entered twice before the request is sent.
 */
export function normalizePersonName(name: string): string {
  return name
    .replace(IGNORED_MARKS, '')
    .replace(/[أإآٱ]/g, 'ا')
    .replace(/ة/g, 'ه')
    .replace(/ى/g, 'ي')
    .replace(/ؤ/g, 'و')
    .replace(/ئ/g, 'ي')
    .replace(/\s+/g, ' ')
    .trim()
    .toLowerCase()
}
