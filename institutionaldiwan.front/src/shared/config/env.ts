/**
 * Typed `import.meta.env` access. `API_BASE_URL` stays relative so the dev
 * proxy (or production reverse proxy) supplies the origin — never bypass it
 * with an absolute URL (that would require CORS).
 */
export const ENV = {
  API_BASE_URL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  /** SignalR hubs; relative for the same reason as `API_BASE_URL` (the dev proxy forwards `/hubs`). */
  HUB_BASE_URL: import.meta.env.VITE_HUB_BASE_URL ?? '/hubs',
  DEFAULT_TIMEZONE: 'Asia/Damascus',
} as const
