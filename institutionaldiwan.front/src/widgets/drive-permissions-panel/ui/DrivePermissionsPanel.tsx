import { useMemo, useState } from 'react'
import { Plus, Shield, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import {
  useDrivePermissions,
  type DriveItemResponse,
  type DrivePermissionResponse,
} from '@/entities/drive-item'
import { useUserOptions } from '@/entities/user'
import { useDepartmentOptions } from '@/entities/department'
import {
  useGrantDrivePermissionMutation,
  useRevokeDrivePermissionMutation,
  useSetDriveInheritanceMutation,
} from '@/features/drive-sharing'
import { toArabicErrorMessage } from '@/shared/api'
import { DRIVE_PRINCIPAL_TYPE, DRIVE_ROLE } from '@/shared/config'
import { t } from '@/shared/lib'
import { Badge, Button, ConfirmDialog, Dialog, Label, Select } from '@/shared/ui'
import { drivePermissionSchema } from '@/features/drive-sharing'

interface DrivePermissionsPanelProps {
  item: DriveItemResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function DrivePermissionsPanel({ item, open, onOpenChange }: DrivePermissionsPanelProps) {
  const query = useDrivePermissions(item.itemId, { enabled: open })
  const users = useUserOptions({ page: 1, pageSize: 100 }, { enabled: open })
  const departments = useDepartmentOptions({ enabled: open })
  const grant = useGrantDrivePermissionMutation(item.itemId)
  const revoke = useRevokeDrivePermissionMutation(item.itemId)
  const inheritance = useSetDriveInheritanceMutation(item.itemId)
  const [principalTypeId, setPrincipalTypeId] = useState(String(DRIVE_PRINCIPAL_TYPE.User))
  const [principalId, setPrincipalId] = useState('')
  const [roleId, setRoleId] = useState(String(DRIVE_ROLE.Viewer))
  const [revokeTarget, setRevokeTarget] = useState<DrivePermissionResponse | null>(null)

  const principalOptions = useMemo(
    () =>
      principalTypeId === String(DRIVE_PRINCIPAL_TYPE.User)
        ? (users.data?.items ?? [])
            .filter((user) => user.isActive)
            .map((user) => ({ value: user.userId, label: user.username }))
        : (departments.data ?? [])
            .filter((department) => department.isActive)
            .map((department) => ({ value: department.departmentId, label: department.nameAr })),
    [principalTypeId, users.data, departments.data],
  )

  const submitGrant = async () => {
    const parsed = drivePermissionSchema.safeParse({ principalTypeId, principalId, roleId })
    if (!parsed.success) {
      toast.error(parsed.error.issues[0]?.message ?? t('drive.dialog.grant-principal'))
      return
    }
    try {
      await grant.mutateAsync({
        principalTypeId: Number(parsed.data.principalTypeId),
        principalId: parsed.data.principalId,
        roleId: Number(parsed.data.roleId),
      })
      toast.success(t('drive.toast.permission-granted'))
      setPrincipalId('')
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const confirmRevoke = async () => {
    if (!revokeTarget) return
    try {
      await revoke.mutateAsync(revokeTarget.permissionId)
      toast.success(t('drive.toast.permission-revoked'))
      setRevokeTarget(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const toggleInheritance = async () => {
    try {
      await inheritance.mutateAsync(!item.breakInheritance)
      toast.success(
        t(item.breakInheritance ? 'drive.toast.inheritance-restored' : 'drive.toast.inheritance-broken'),
      )
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  return (
    <>
      <Dialog
        open={open}
        onOpenChange={onOpenChange}
        title={t('drive.dialog.permissions-title')}
        description={item.name}
        size="lg"
        footer={
          <Button type="button" onClick={() => onOpenChange(false)}>
            <Label k="common.action.close" />
          </Button>
        }
      >
        <div className="flex flex-col gap-5">
          <div className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-border bg-surface-muted/20 p-3">
            <p className="flex items-center gap-2 text-sm">
              <Shield className="size-4 text-accent" aria-hidden />
              {item.breakInheritance ? (
                <Label k="drive.inheritance.explicit" />
              ) : (
                <Label k="drive.inheritance.inherited-from" />
              )}
            </p>
            <Button variant="outline" size="sm" onClick={toggleInheritance} loading={inheritance.isPending}>
              <Label
                k={
                  item.breakInheritance
                    ? 'drive.permission.restore-inheritance'
                    : 'drive.permission.break-inheritance'
                }
              />
            </Button>
          </div>

          <div className="grid gap-2 rounded-lg border border-border p-3 sm:grid-cols-[10rem_1fr_10rem_auto]">
            <Select
              value={principalTypeId}
              onValueChange={(value) => {
                setPrincipalTypeId(value)
                setPrincipalId('')
              }}
              options={[
                { value: String(DRIVE_PRINCIPAL_TYPE.User), label: t('drive.principal.user') },
                { value: String(DRIVE_PRINCIPAL_TYPE.Department), label: t('drive.principal.department') },
              ]}
              aria-label={t('drive.dialog.grant-principal')}
            />
            <Select
              value={principalId || undefined}
              onValueChange={setPrincipalId}
              options={principalOptions}
              placeholder={t('drive.dialog.grant-principal')}
              aria-label={t('drive.dialog.grant-principal')}
            />
            <Select
              value={roleId}
              onValueChange={setRoleId}
              options={[
                { value: String(DRIVE_ROLE.Viewer), label: t('drive.role.viewer') },
                { value: String(DRIVE_ROLE.Editor), label: t('drive.role.editor') },
                { value: String(DRIVE_ROLE.Manager), label: t('drive.role.manager') },
              ]}
              aria-label={t('drive.dialog.grant-role')}
            />
            <Button onClick={() => void submitGrant()} disabled={!principalId} loading={grant.isPending}>
              <Plus className="size-4" aria-hidden />
              <Label k="drive.permission.grant" />
            </Button>
          </div>

          {query.isLoading ? (
            <p className="py-6 text-center text-sm text-muted">
              <Label k="drive.state.loading" />
            </p>
          ) : query.error ? (
            <div className="flex flex-col items-center gap-3 py-6">
              <p className="text-sm text-danger">{toArabicErrorMessage(query.error)}</p>
              <Button variant="outline" onClick={() => void query.refetch()}>
                <Label k="drive.retry" />
              </Button>
            </div>
          ) : (query.data ?? []).length === 0 ? (
            <p className="py-6 text-center text-sm text-muted">
              <Label k="drive.browser.empty-title" />
            </p>
          ) : (
            <ul className="flex flex-col gap-2">
              {(query.data ?? []).map((permission) => (
                <PermissionRow
                  key={permission.permissionId}
                  permission={permission}
                  onRevoke={() => setRevokeTarget(permission)}
                />
              ))}
            </ul>
          )}
        </div>
      </Dialog>
      <ConfirmDialog
        open={Boolean(revokeTarget)}
        onOpenChange={(nextOpen) => {
          if (!nextOpen) setRevokeTarget(null)
        }}
        title={t('drive.permission.revoke')}
        description={t('drive.dialog.permissions-title')}
        confirmLabel={t('drive.permission.revoke')}
        onConfirm={() => void confirmRevoke()}
        loading={revoke.isPending}
        destructive
      />
    </>
  )
}

function PermissionRow({
  permission,
  onRevoke,
}: {
  permission: DrivePermissionResponse
  onRevoke: () => void
}) {
  return (
    <li className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-surface-muted/20 p-3">
      <div className="flex min-w-0 items-center gap-2 text-sm">
        <Badge variant="outline">
          <Label
            k={
              permission.principalTypeId === DRIVE_PRINCIPAL_TYPE.User
                ? 'drive.principal.user'
                : 'drive.principal.department'
            }
          />
        </Badge>
        <span dir="ltr" className="truncate">
          {permission.principalId}
        </span>
        <Badge variant="success">
          <Label
            k={
              permission.roleId === DRIVE_ROLE.Manager
                ? 'drive.role.manager'
                : permission.roleId === DRIVE_ROLE.Editor
                  ? 'drive.role.editor'
                  : 'drive.role.viewer'
            }
          />
        </Badge>
      </div>
      <Button variant="outline" size="sm" onClick={onRevoke}>
        <Trash2 className="size-4 text-danger" aria-hidden />
        <Label k="drive.permission.revoke" />
      </Button>
    </li>
  )
}
