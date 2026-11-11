import { useEffect } from 'react'
import { brandingLogoUrl } from '../lib/brand'
import { useBranding } from '../model/queries'
import { useBrandName } from '../model/useBrandName'

const DEFAULT_ICON = '/favicon.svg'

/** Keeps the browser tab's title and icon in step with the stored name and logo. Renders nothing. */
export function BrandingEffects() {
  const name = useBrandName()
  const logoUpdatedAt = useBranding().data?.logoUpdatedAt

  useEffect(() => {
    document.title = `${name} — بوابة الدخول المؤسسية`
  }, [name])

  useEffect(() => {
    const link = document.querySelector<HTMLLinkElement>('link[rel="icon"]')
    if (!link) return
    const logoUrl = brandingLogoUrl(logoUpdatedAt)
    if (logoUrl) {
      link.removeAttribute('type')
      link.href = logoUrl
    } else {
      link.type = 'image/svg+xml'
      link.href = DEFAULT_ICON
    }
  }, [logoUpdatedAt])

  return null
}
