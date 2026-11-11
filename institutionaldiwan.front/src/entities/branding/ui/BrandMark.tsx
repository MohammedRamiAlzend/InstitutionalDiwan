import { Landmark } from 'lucide-react'
import { cn } from '@/shared/lib'
import { brandingLogoUrl } from '../lib/brand'
import { useBranding } from '../model/queries'

/**
 * The institution's mark: its uploaded logo, or the default landmark badge while there is none.
 * Size it with `className` (for example `size-10`).
 */
export function BrandMark({ className }: { className?: string }) {
  const branding = useBranding()
  const logoUrl = brandingLogoUrl(branding.data?.logoUpdatedAt)

  if (logoUrl) {
    return (
      <span
        className={cn(
          'flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-surface',
          className,
        )}
      >
        <img
          src={logoUrl}
          alt=""
          className="size-full object-contain"
          draggable={false}
        />
      </span>
    )
  }

  return (
    <span
      className={cn(
        'flex size-10 shrink-0 items-center justify-center rounded-full bg-accent text-accent-contrast',
        className,
      )}
    >
      <Landmark className="size-1/2" aria-hidden />
    </span>
  )
}
