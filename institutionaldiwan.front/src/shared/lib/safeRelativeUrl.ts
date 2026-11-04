/**
 * Returns `value` only when it is an in-app relative path: it starts with exactly one `/` and has
 * no scheme, host, backslash or control character. The server validates `actionUrl` already; this
 * is the second line of defence before the value reaches `navigate()`.
 */
export function safeRelativeUrl(value: string | null | undefined): string | null {
  if (!value) return null
  if (!value.startsWith('/') || value.startsWith('//')) return null
  if (value.includes('\\')) return null
  for (let index = 0; index < value.length; index += 1) {
    const code = value.charCodeAt(index)
    if (code <= 0x1f || code === 0x7f) return null
  }
  return value
}
