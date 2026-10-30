/** Template applied when a registry leaves `formatPattern` blank (mirrors the server). */
export const DEFAULT_NUMBER_PATTERN = '{YEAR}/{SEQ:D6}'

export interface NumberPatternValues {
  numberingYear: number
  sequenceValue: number
}

const yearToken = /\{YEAR\}/gi
const sequenceToken = /\{SEQ(?::D(?<digits>\d{1,3}))?\}/gi
const anyToken = /\{[^{}]*\}/g
const supportedToken = /^\{(?:YEAR|SEQ(?::D\d{1,3})?)\}$/i

/**
 * Renders a registry template exactly like `OfficialNumberFormatter`: `{YEAR}` and `{SEQ}` are
 * replaced case-insensitively, `{SEQ:Dn}` zero-pads to `n` digits, unknown text stays verbatim,
 * and a blank pattern falls back to `{YEAR}/{SEQ:D6}`.
 */
export function renderNumberPattern(
  pattern: string | null | undefined,
  { numberingYear, sequenceValue }: NumberPatternValues,
): string {
  const template =
    pattern === null || pattern === undefined || pattern.trim() === ''
      ? DEFAULT_NUMBER_PATTERN
      : pattern.trim()

  return template.replace(yearToken, String(numberingYear)).replace(
    sequenceToken,
    (_match: string, digits: string | undefined) => {
      const width = digits ? Number(digits) : 0
      return width > 0 ? String(sequenceValue).padStart(width, '0') : String(sequenceValue)
    },
  )
}

/** `{…}` tokens the formatter cannot expand; empty when every token is supported. */
export function unknownPatternTokens(pattern: string): string[] {
  const unknown = [...pattern.matchAll(anyToken)]
    .map((match) => match[0])
    .filter((token) => !supportedToken.test(token))

  return [...new Set(unknown)]
}
