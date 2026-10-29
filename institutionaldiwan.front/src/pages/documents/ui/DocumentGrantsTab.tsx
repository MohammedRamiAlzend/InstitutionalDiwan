import { useMemo, useState } from 'react'
import { ShieldCheck, ShieldOff, UserPlus } from 'lucide-react'
import { useDocumentAccessGrants, type DocumentAccessGrantResponse } from '@/entities/document'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { useSessionStore, useUserById } from '@/entities/user'
import {
  AddAccessGrantDialog,
  RevokeAccessGrantDialog,
} from '@/features/document-access-grant'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { formatDate, formatDateTime, formatShortId } from '@/shared/lib'
import { Badge, Button, Skeleton } from '@/shared/ui'

interface DocumentGrantsTabProps {
  documentId: string
}

interface RevokeTarget {
  grantId: string
  granteeLabel: string
}

type GrantState = 'active' | 'expired' | 'revoked'

function resolveGrantState(grant: DocumentAccessGrantResponse): GrantState {
  if (grant.isRevoked) return 'revoked'
  if (grant.expiresAt && new Date(grant.expiresAt) <= new Date()) return 'expired'
  return 'active'
}

const STATE_BADGES: Record<
  GrantState,
  { label: string; variant: 'success' | 'warning' | 'danger' }
> = {
  active: { label: 'ساري', variant: 'success' },
  expired: { label: 'منتهٍ', variant: 'warning' },
  revoked: { label: 'ملغى', variant: 'danger' },
}

/** Resolves a grant participant id through `/api/users/{id}` when the caller may read users. */
function useUserLabel(userId: string) {
  const canReadUsers = useSessionStore((state) => state.hasPermission(Permissions.UserManage))
  const query = useUserById(userId, { enabled: canReadUsers })

  return {
    canReadUsers,
    isLoading: canReadUsers && query.isLoading,
    label: canReadUsers ? (query.data?.username ?? formatShortId(userId)) : formatShortId(userId),
  }
}

interface GrantRowProps {
  grant: DocumentAccessGrantResponse
  canManage: boolean
  onRevoke: (target: RevokeTarget) => void
}

function GrantRow({ grant, canManage, onRevoke }: GrantRowProps) {
  const state = resolveGrantState(grant)
  const badge = STATE_BADGES[state]
  const grantee = useUserLabel(grant.userId)
  const grantor = useUserLabel(grant.grantedByUserId)
  const revoker = useUserLabel(grant.revokedByUserId ?? '')

  return (
    <li className="flex flex-col gap-2 rounded-lg border border-border bg-background px-4 py-3">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <LookupLabel
              lookup={LOOKUP_SLUGS.AccessLevels}
              id={grant.accessLevelId}
              className="font-medium"
            />
            <Badge variant={badge.variant}>{badge.label}</Badge>
          </div>
          <div className="flex flex-wrap items-center gap-1.5 text-sm">
            <span className="text-muted">المستخدم:</span>
            {grantee.canReadUsers && grantee.isLoading ? (
              <Skeleton className="h-4 w-20" />
            ) : (
              <span dir={grantee.canReadUsers ? undefined : 'ltr'} title={grant.userId}>
                {grantee.label}
              </span>
            )}
          </div>
        </div>

        {canManage && state !== 'revoked' && (
          <Button
            type="button"
            size="sm"
            variant="outline"
            className="text-danger"
            onClick={() =>
              onRevoke({ grantId: grant.grantId, granteeLabel: grantee.label })
            }
          >
            <ShieldOff className="size-4" aria-hidden />
            إلغاء التصريح
          </Button>
        )}
      </div>

      <dl className="grid gap-x-4 gap-y-1 text-xs text-muted sm:grid-cols-2">
        <div className="flex flex-wrap items-center gap-1.5">
          <dt>تاريخ المنح:</dt>
          <dd className="text-foreground">{formatDateTime(grant.grantedAt)}</dd>
        </div>
        <div className="flex flex-wrap items-center gap-1.5">
          <dt>مانح التصريح:</dt>
          <dd className="text-foreground">{grantor.label}</dd>
        </div>
        <div className="flex flex-wrap items-center gap-1.5">
          <dt>تاريخ الانتهاء:</dt>
          <dd className="text-foreground">
            {grant.expiresAt ? formatDate(grant.expiresAt) : 'بلا انتهاء'}
          </dd>
        </div>
        {grant.isRevoked && (
          <div className="flex flex-wrap items-center gap-1.5">
            <dt>أُلغي في:</dt>
            <dd className="text-foreground">
              {grant.revokedAt ? formatDateTime(grant.revokedAt) : '—'}
              {grant.revokedByUserId && ` · ${revoker.label}`}
            </dd>
          </div>
        )}
      </dl>

      {grant.reason && <p className="text-xs leading-6 text-muted">{grant.reason}</p>}
    </li>
  )
}

/** Grants tab: every grant recorded on the document, including revoked rows. */
export function DocumentGrantsTab({ documentId }: DocumentGrantsTabProps) {
  const grantsQuery = useDocumentAccessGrants(documentId)
  const canManage = useSessionStore((state) => state.hasPermission(Permissions.AccessGrantManage))

  const [grantOpen, setGrantOpen] = useState(false)
  const [revokeTarget, setRevokeTarget] = useState<RevokeTarget | null>(null)

  const grants = useMemo(() => grantsQuery.data ?? [], [grantsQuery.data])
  const activeCount = grants.filter((grant) => resolveGrantState(grant) === 'active').length

  return (
    <section className="rounded-xl border border-border bg-surface p-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="text-base font-semibold text-foreground">تصاريح الوصول</h2>
          <Badge variant="outline">{grants.length}</Badge>
          <Badge variant="success">{activeCount} ساري</Badge>
        </div>
        {canManage && (
          <Button type="button" size="sm" onClick={() => setGrantOpen(true)}>
            <UserPlus className="size-4" aria-hidden />
            منح وصول
          </Button>
        )}
      </div>

      {grantsQuery.isLoading ? (
        <div className="mt-4 flex flex-col gap-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      ) : grantsQuery.isError ? (
        <div className="mt-6 flex flex-col items-center gap-2 text-center">
          <p className="text-sm font-medium text-danger">
            {toArabicErrorMessage(grantsQuery.error)}
          </p>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() => void grantsQuery.refetch()}
          >
            إعادة المحاولة
          </Button>
        </div>
      ) : grants.length === 0 ? (
        <div className="mt-6 flex flex-col items-center gap-2 rounded-lg border border-dashed border-border px-4 py-10 text-center">
          <ShieldCheck className="size-5 text-muted" aria-hidden />
          <p className="text-sm font-medium text-foreground">لا توجد تصاريح وصول بعد.</p>
          <p className="max-w-md text-xs text-muted">
            التصاريح تمنح مستخدمًا محددًا مستوى وصول إضافيًا على هذه الوثيقة، وتظهر هنا مع
            المانح وتاريخ الانتهاء وسجل الإلغاء.
          </p>
          {canManage && (
            <Button type="button" size="sm" variant="outline" onClick={() => setGrantOpen(true)}>
              <UserPlus className="size-4" aria-hidden />
              منح أول تصريح
            </Button>
          )}
        </div>
      ) : (
        <ul className="mt-4 flex flex-col gap-3">
          {grants.map((grant) => (
            <GrantRow
              key={grant.grantId}
              grant={grant}
              canManage={canManage}
              onRevoke={setRevokeTarget}
            />
          ))}
        </ul>
      )}

      {grantOpen && (
        <AddAccessGrantDialog
          documentId={documentId}
          open
          onOpenChange={setGrantOpen}
        />
      )}
      {revokeTarget && (
        <RevokeAccessGrantDialog
          documentId={documentId}
          grantId={revokeTarget.grantId}
          granteeLabel={revokeTarget.granteeLabel}
          open
          onOpenChange={(open) => {
            if (!open) setRevokeTarget(null)
          }}
        />
      )}
    </section>
  )
}
