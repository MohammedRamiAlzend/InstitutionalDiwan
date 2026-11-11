import { DEFAULT_BRAND_NAME } from '../lib/brand'
import { useBranding } from './queries'

/** The stored Arabic institution name, falling back to the default while it loads. */
export function useBrandName(): string {
  const branding = useBranding()
  return branding.data?.institutionNameAr?.trim() || DEFAULT_BRAND_NAME
}
