export interface JwtClaims {
  sub?: string
  unique_name?: string
  name?: string
  dept_id?: string
  scope_type?: string
  must_change_password?: string | boolean
  exp?: number
  [claim: string]: unknown
}

function decodeBase64Url(segment: string): string {
  const normalized = segment.replace(/-/g, '+').replace(/_/g, '/')
  const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=')
  const binary = window.atob(padded)
  const escaped = Array.from(binary)
    .map((char) => `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`)
    .join('')
  return decodeURIComponent(escaped)
}

/**
 * Decodes the JWT payload without verifying the signature. Used only for
 * client-side UX hints (for example the forced password change banner) — the
 * server remains the authority for every claim.
 */
export function decodeJwtPayload(token: string | null | undefined): JwtClaims | null {
  if (!token) return null
  const segment = token.split('.')[1]
  if (!segment) return null

  try {
    const payload: unknown = JSON.parse(decodeBase64Url(segment))
    if (typeof payload !== 'object' || payload === null || Array.isArray(payload)) return null
    return payload as JwtClaims
  } catch {
    return null
  }
}

/** `GET /api/auth/me` omits the flag, so the login token is the only hint. */
export function readMustChangePassword(token: string | null | undefined): boolean {
  const value = decodeJwtPayload(token)?.must_change_password
  return value === true || value === 'true'
}
