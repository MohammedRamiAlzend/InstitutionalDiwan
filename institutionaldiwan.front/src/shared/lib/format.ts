import { t } from './labels'

const numberFormatter = new Intl.NumberFormat('ar-SY', { numberingSystem: 'latn' })
const sizeFormatter = new Intl.NumberFormat('ar-SY', {
  numberingSystem: 'latn',
  maximumFractionDigits: 1,
})

/** Latin-digit grouping for counts, row ranges, and pagination labels. */
export function formatNumber(value: number): string {
  return numberFormatter.format(value)
}

/** First GUID segment for compact LTR display; the full value belongs in `title`. */
export function formatShortId(value: string | null | undefined): string {
  return value ? value.slice(0, 8) : '—'
}

const SIZE_UNIT_KEYS = ['common.size.kb', 'common.size.mb', 'common.size.gb'] as const

/** Byte counts as Latin-digit sizes with Arabic units, for attachment and version rows. */
export function formatFileSize(bytes: number | null | undefined): string {
  if (bytes === null || bytes === undefined || Number.isNaN(bytes) || bytes < 0) return '—'
  if (bytes < 1024) return `${numberFormatter.format(bytes)} ${t('common.size.bytes')}`

  let value = bytes / 1024
  let unit = 0
  while (value >= 1024 && unit < SIZE_UNIT_KEYS.length - 1) {
    value /= 1024
    unit += 1
  }
  return `${sizeFormatter.format(value)} ${t(SIZE_UNIT_KEYS[unit])}`
}
