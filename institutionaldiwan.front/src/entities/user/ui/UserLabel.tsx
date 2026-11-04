import { useQuery } from '@tanstack/react-query'
import { tokenStorage } from '@/shared/api'
import { NameWithId } from '@/shared/ui'
import { loadUserLabel } from '../api/userLabelLoader'
import { userKeys } from '../model/queries'

interface UserLabelProps {
  userId: string | null | undefined
  className?: string
}

/**
 * A user's login name with the short id («superadmin  9f55121c»). Every label on a page shares one
 * batched request; where the caller may not read names it quietly shows the short id alone.
 */
export function UserLabel({ userId, className }: UserLabelProps) {
  const query = useQuery({
    queryKey: userKeys.label(userId ?? ''),
    queryFn: () => loadUserLabel(userId as string),
    enabled: Boolean(userId) && tokenStorage.has(),
    staleTime: 10 * 60 * 1000,
    retry: false,
  })

  if (!userId) return <span>—</span>
  return <NameWithId name={query.data?.username} id={userId} className={className} />
}
