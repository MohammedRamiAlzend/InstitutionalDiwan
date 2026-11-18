import { useMemo, useState } from 'react'
import { Plus, ShieldCheck, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { RoleBadge, useRoleOptions } from '@/entities/role'
import { useSessionStore, useUserById, type UserResponse } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { t } from '@/shared/lib'
import { Button, Dialog, Label, Select } from '@/shared/ui'
import {
  useAssignUserRoleMutation,
  useRevokeUserRoleMutation,
} from '../api/userMutations'

interface UserRolesDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user: UserResponse
}

/** Grants and revokes roles through `/api/users/{id}/roles`. */
export function UserRolesDialog({ open, onOpenChange, user }: UserRolesDialogProps) {
  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManageRoles = hasPermission(Permissions.UserManage)

  const userQuery = useUserById(user.userId, { enabled: open })
  const rolesQuery = useRoleOptions({ enabled: open && canManageRoles })
  const assignMutation = useAssignUserRoleMutation()
  const revokeMutation = useRevokeUserRoleMutation()

  const [selectedRoleId, setSelectedRoleId] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)

  const roleByCode = useMemo(
    () => new Map((rolesQuery.data ?? []).map((role) => [role.code, role])),
    [rolesQuery.data],
  )

  const assignedRoles = useMemo(
    () =>
      (userQuery.data?.roles ?? [])
        .map((code) => ({ code, role: roleByCode.get(code) }))
        .sort((left, right) =>
          (left.role?.nameAr ?? left.code).localeCompare(
            right.role?.nameAr ?? right.code,
            'ar',
          ),
        ),
    [roleByCode, userQuery.data?.roles],
  )

  const availableRoles = useMemo(
    () =>
      (rolesQuery.data ?? [])
        .filter((role) => role.isActive && userQuery.data && !userQuery.data.roles.includes(role.code))
        .map((role) => ({
          value: role.roleId,
          label: `${role.nameAr} (${role.code})`,
        })),
    [rolesQuery.data, userQuery.data],
  )

  const effectiveSelection = availableRoles.some(
    (option) => option.value === selectedRoleId,
  )
    ? selectedRoleId
    : ''

  const handleAssign = async () => {
    if (!effectiveSelection) return
    setActionError(null)
    try {
      await assignMutation.mutateAsync({ userId: user.userId, roleId: effectiveSelection })
      setSelectedRoleId('')
      toast.success(t('user.roles.toast-assigned'))
    } catch (error) {
      setActionError(toArabicErrorMessage(error))
    }
  }

  const handleRevoke = async (roleId: string) => {
    setActionError(null)
    try {
      await revokeMutation.mutateAsync({ userId: user.userId, roleId })
      toast.success(t('user.roles.toast-revoked'))
    } catch (error) {
      setActionError(toArabicErrorMessage(error))
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('user.roles.title')}
      description={t('user.roles.description', { name: user.username })}
      size="md"
      footer={
        <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
          <Label k="common.action.close" />
        </Button>
      }
    >
      <section className="flex flex-col gap-2">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-foreground">
          <ShieldCheck className="size-4 text-accent" aria-hidden />
          <Label k="user.roles.current" />
        </h3>

        {userQuery.isError ? (
          <div role="alert" className="text-sm text-danger">
            {toArabicErrorMessage(userQuery.error)}
            <Button type="button" variant="outline" onClick={() => void userQuery.refetch()}><Label k="common.action.retry" /></Button>
          </div>
        ) : userQuery.isLoading ? (
          <p role="status" className="text-sm text-muted">{t('user.roles.loading')}</p>
        ) : assignedRoles.length === 0 ? (
          <p className="rounded-lg bg-surface-muted px-3 py-2 text-sm text-muted">
            {t('user.roles.empty')}
          </p>
        ) : (
          <ul className="flex flex-col gap-2">
            {assignedRoles.map(({ code, role }) => (
              <li
                key={code}
                className="flex items-center justify-between gap-3 rounded-lg border border-border px-3 py-2"
              >
                {role ? (
                  <RoleBadge
                    nameAr={role.nameAr}
                    code={role.code}
                  />
                ) : (
                  <span
                    dir="ltr"
                    className="rounded border border-border px-1.5 py-0.5 font-mono text-xs text-muted"
                  >
                    {code}
                  </span>
                )}

                {role && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="text-danger"
                    disabled={!canManageRoles || userQuery.isFetching || assignMutation.isPending || revokeMutation.isPending}
                    onClick={() => handleRevoke(role.roleId)}
                  >
                    <Trash2 className="size-4" aria-hidden />
                    <Label k="user.roles.revoke" />
                  </Button>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="flex flex-col gap-2 border-t border-border pt-4">
        <h3 className="text-sm font-semibold text-foreground"><Label k="user.roles.assign-title" /></h3>

        {!canManageRoles ? (
          <p role="status" className="rounded-lg bg-warning/10 px-3 py-2 text-xs text-warning">
            {t('user.roles.denied')}
          </p>
        ) : rolesQuery.isError ? (
          <div className="flex flex-col gap-2 rounded-lg bg-danger/10 px-3 py-2 text-xs text-danger">
            <span>{toArabicErrorMessage(rolesQuery.error)}</span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => void rolesQuery.refetch()}
            >
              <Label k="common.action.retry" />
            </Button>
          </div>
        ) : availableRoles.length === 0 ? (
          <p className="rounded-lg bg-surface-muted px-3 py-2 text-sm text-muted">
            {rolesQuery.isLoading
              ? t('user.roles.available-loading')
              : t('user.roles.available-empty')}
          </p>
        ) : (
          <div className="flex flex-wrap items-center gap-2">
            <Select
              value={effectiveSelection || undefined}
              onValueChange={setSelectedRoleId}
              options={availableRoles}
              placeholder={t('user.roles.placeholder')}
              aria-label={t('user.roles.select-aria')}
              className="min-w-48 flex-1"
            />
            <Button
              type="button"
              loading={assignMutation.isPending}
              disabled={!effectiveSelection || userQuery.isFetching || userQuery.isError || revokeMutation.isPending}
              onClick={handleAssign}
            >
              <Plus className="size-4" aria-hidden />
              <Label k="user.roles.assign" />
            </Button>
          </div>
        )}
      </section>

      {actionError && (
        <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          {actionError}
        </p>
      )}
    </Dialog>
  )
}
