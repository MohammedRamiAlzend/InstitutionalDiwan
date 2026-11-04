const DAMASCUS_TIME_ZONE = 'Asia/Damascus'

// `en-US` is used only to read the numeric parts in Latin digits; the text is assembled below.
// The `ar-*` locales insert right-to-left marks between the parts, which splits the digit runs in
// an RTL page and reorders them (the time lands before the date, the year loses its slashes).
const partsFormatter = new Intl.DateTimeFormat('en-US', {
  timeZone: DAMASCUS_TIME_ZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

const EMPTY_VALUE = '—'

/**
 * U+2066 LEFT-TO-RIGHT ISOLATE ... U+2069 POP DIRECTIONAL ISOLATE. A date is a left-to-right
 * sequence of digits and separators; isolating it keeps it intact next to Arabic text and inside
 * RTL table cells and flex rows, even where the caller forgot `dir="ltr"`.
 */
const LRI = '⁦'
const PDI = '⁩'

function toDate(value: string | Date | null | undefined): Date | null {
  if (!value) return null
  const date = value instanceof Date ? value : new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

function readParts(date: Date): Record<string, string> {
  const parts: Record<string, string> = {}
  for (const part of partsFormatter.formatToParts(date)) {
    if (part.type !== 'literal') parts[part.type] = part.value
  }
  return parts
}

/** Backend UTC timestamps (`datetime2(3)`) rendered as Damascus local dates: `2026/11/04`. */
export function formatDate(value: string | Date | null | undefined): string {
  const date = toDate(value)
  if (!date) return EMPTY_VALUE

  const { year, month, day } = readParts(date)
  return `${LRI}${year}/${month}/${day}${PDI}`
}

/** Damascus local date and 24-hour time: `2026/11/04 09:12`. */
export function formatDateTime(value: string | Date | null | undefined): string {
  const date = toDate(value)
  if (!date) return EMPTY_VALUE

  const { year, month, day, hour, minute } = readParts(date)
  return `${LRI}${year}/${month}/${day} ${hour}:${minute}${PDI}`
}
