const DAMASCUS_TIME_ZONE = 'Asia/Damascus'

const dateFormatter = new Intl.DateTimeFormat('ar-SY', {
  timeZone: DAMASCUS_TIME_ZONE,
  numberingSystem: 'latn',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
})

const dateTimeFormatter = new Intl.DateTimeFormat('ar-SY', {
  timeZone: DAMASCUS_TIME_ZONE,
  numberingSystem: 'latn',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
})

const EMPTY_VALUE = '—'

function toDate(value: string | Date | null | undefined): Date | null {
  if (!value) return null
  const date = value instanceof Date ? value : new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

/** Backend UTC timestamps (`datetime2(3)`) rendered as Damascus local dates. */
export function formatDate(value: string | Date | null | undefined): string {
  const date = toDate(value)
  return date ? dateFormatter.format(date) : EMPTY_VALUE
}

export function formatDateTime(value: string | Date | null | undefined): string {
  const date = toDate(value)
  return date ? dateTimeFormatter.format(date) : EMPTY_VALUE
}
