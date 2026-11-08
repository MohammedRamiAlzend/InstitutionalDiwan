import { ENV } from '@/shared/config'
import { ApiError } from './problemDetails'
import { tokenStorage } from './tokenStorage'

/** `SystemErrorKind` values a browser may report. */
export const CLIENT_ERROR_KIND = { Render: 3, Script: 4, Network: 5 } as const

export type ClientErrorKind = (typeof CLIENT_ERROR_KIND)[keyof typeof CLIENT_ERROR_KIND]

export interface ClientErrorReport {
  kind: ClientErrorKind
  message: string
  details?: string
  path?: string
  httpMethod?: string
  statusCode?: number
}

const STORAGE_KEY = 'diwan.error-reports.v1'
const MAX_PENDING = 20
const REPEAT_WINDOW_MS = 60_000
const RETRY_INTERVAL_MS = 60_000
const MAX_MESSAGE_LENGTH = 2000
const MAX_DETAILS_LENGTH = 16_000

const lastSent = new Map<string, number>()
let flushing = false

function readPending(): ClientErrorReport[] {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]')
    return Array.isArray(parsed) ? (parsed as ClientErrorReport[]) : []
  } catch {
    return []
  }
}

function writePending(reports: readonly ClientErrorReport[]): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(reports))
  } catch {
    // Storage full or blocked: the report is lost, which must not disturb the page.
  }
}

/** Query strings can hold search terms and other personal input, so they are never reported. */
function withoutQuery(path: string | undefined): string {
  return (path ?? window.location.pathname).split(/[?#]/)[0]
}

async function send(report: ClientErrorReport, token: string): Promise<boolean> {
  try {
    const response = await fetch(`${ENV.API_BASE_URL}/system-errors/client-reports`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify(report),
      keepalive: true,
    })
    // Only a server that could not answer is worth retrying; a refused report never will succeed.
    return response.status < 500
  } catch {
    return false
  }
}

/** Sends what is waiting, in order. Stops at the first failure: the server is still out of reach. */
async function flush(): Promise<void> {
  const token = tokenStorage.get()
  if (flushing || !token) return

  flushing = true
  try {
    let pending = readPending()
    while (pending.length > 0 && (await send(pending[0], token))) {
      pending = readPending().slice(1)
      writePending(pending)
    }
  } finally {
    flushing = false
  }
}

/**
 * Hands a browser failure to the developer's error log. Never throws and never blocks. Reports wait
 * in `localStorage`, so a failure caused by the server being down arrives once it is back. It uses
 * `fetch` directly: going through `httpClient` would report its own failures forever.
 */
export function reportClientError(report: ClientErrorReport): void {
  try {
    const key = `${report.kind}|${report.httpMethod ?? ''}|${report.path ?? ''}|${report.message}`
    const now = Date.now()
    if (now - (lastSent.get(key) ?? 0) < REPEAT_WINDOW_MS) return
    lastSent.set(key, now)

    writePending(
      [
        ...readPending(),
        {
          ...report,
          message: report.message.slice(0, MAX_MESSAGE_LENGTH) || 'Unknown error',
          details: report.details?.slice(0, MAX_DETAILS_LENGTH),
          path: withoutQuery(report.path),
        },
      ].slice(-MAX_PENDING),
    )
    void flush()
  } catch {
    // Reporting must never become the failure.
  }
}

/** Catches what nothing else handled. Called once, when the application starts. */
export function installGlobalErrorReporting(): void {
  window.addEventListener('error', (event) => {
    // The browser's own layout notice, not a defect.
    if (event.message.includes('ResizeObserver')) return
    reportClientError({
      kind: CLIENT_ERROR_KIND.Script,
      message: event.message,
      details: event.error instanceof Error ? event.error.stack : undefined,
    })
  })

  window.addEventListener('unhandledrejection', (event) => {
    const reason: unknown = event.reason
    // API failures are reported by the HTTP client, with the endpoint they belong to.
    if (reason instanceof ApiError) return
    reportClientError({
      kind: CLIENT_ERROR_KIND.Script,
      message: reason instanceof Error ? reason.message : String(reason),
      details: reason instanceof Error ? reason.stack : undefined,
    })
  })

  window.addEventListener('online', () => void flush())
  window.setInterval(() => void flush(), RETRY_INTERVAL_MS)
  void flush()
}
