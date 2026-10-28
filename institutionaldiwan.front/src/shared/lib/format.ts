const numberFormatter = new Intl.NumberFormat('ar-SY', { numberingSystem: 'latn' })

/** Latin-digit grouping for counts, row ranges, and pagination labels. */
export function formatNumber(value: number): string {
  return numberFormatter.format(value)
}

/** First GUID segment for compact LTR display; the full value belongs in `title`. */
export function formatShortId(value: string | null | undefined): string {
  return value ? value.slice(0, 8) : '—'
}
