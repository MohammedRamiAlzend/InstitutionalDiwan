import { ENV } from '@/shared/config'

/** Shown until the stored name arrives, or when the settings cannot be read. */
export const DEFAULT_BRAND_NAME = 'الديوان المؤسسي'

/**
 * Address of the uploaded logo. The version changes with every upload, so a new logo bypasses the
 * browser's copy at once; `null` when no logo was uploaded.
 */
export function brandingLogoUrl(logoUpdatedAt: string | null | undefined): string | null {
  if (!logoUpdatedAt) return null
  return `${ENV.API_BASE_URL}/branding/logo?v=${encodeURIComponent(logoUpdatedAt)}`
}
