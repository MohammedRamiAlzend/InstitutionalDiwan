import { ShieldAlert, ShieldCheck } from 'lucide-react'
import { useLabel } from '@/shared/lib'
import { Badge } from '@/shared/ui'

interface IntegrityStatusBadgeProps {
  isValid: boolean
}

/** Pass/fail badge for an integrity report (`isValid`). */
export function IntegrityStatusBadge({ isValid }: IntegrityStatusBadgeProps) {
  const Icon = isValid ? ShieldCheck : ShieldAlert
  const label = useLabel(isValid ? 'audit.integrity.valid' : 'audit.integrity.breached')

  return (
    <Badge variant={isValid ? 'success' : 'danger'}>
      <Icon className="size-3.5" aria-hidden />
      {label}
    </Badge>
  )
}
