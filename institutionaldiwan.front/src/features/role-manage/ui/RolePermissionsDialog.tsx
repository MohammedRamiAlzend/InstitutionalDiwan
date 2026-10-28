import { useMemo, useState } from 'react'
import { KeyRound } from 'lucide-react'
import { toast } from 'sonner'
import {
  permissionModuleLabel,
  usePermissionsCatalog,
  type RoleResponse,
} from '@/entities/role'
import { toArabicErrorMessage } from '@/shared/api'
import type { PermissionCode } from '@/shared/config'
import { Badge, Button, Checkbox, Dialog, Skeleton } from '@/shared/ui'
import { useUpdateRolePermissionsMutation } from '../api/roleMutations'

interface RolePermissionsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  role: RoleResponse
}

/** Grouped permission matrix; saves only the grants/revokes that changed. */
export function RolePermissionsDialog({
  open,
  onOpenChange,
  role,
}: RolePermissionsDialogProps) {
  const catalogQuery = usePermissionsCatalog()
  const mutation = useUpdateRolePermissionsMutation()

  const [draft, setDraft] = useState(() => new Set(role.permissions))
  const [actionError, setActionError] = useState<string | null>(null)

  const groups = useMemo(() => {
    const byModule = new Map<string, { code: PermissionCode; nameAr: string }[]>()

    for (const permission of catalogQuery.data ?? []) {
      const items = byModule.get(permission.moduleName) ?? []
      items.push({ code: permission.code, nameAr: permission.nameAr })
      byModule.set(permission.moduleName, items)
    }

    return Array.from(byModule.entries())
      .map(([module, items]) => ({
        module,
        label: permissionModuleLabel(module),
        items: [...items].sort((left, right) =>
          left.nameAr.localeCompare(right.nameAr, 'ar'),
        ),
      }))
      .sort((left, right) => left.label.localeCompare(right.label, 'ar'))
  }, [catalogQuery.data])

  const current = useMemo(() => new Set(role.permissions), [role.permissions])
  const grant = useMemo(
    () => [...draft].filter((code) => !current.has(code)),
    [current, draft],
  )
  const revoke = useMemo(
    () => role.permissions.filter((code) => !draft.has(code)),
    [draft, role.permissions],
  )
  const dirty = grant.length > 0 || revoke.length > 0

  const toggle = (code: PermissionCode, checked: boolean) => {
    setDraft((previous) => {
      const next = new Set(previous)
      if (checked) next.add(code)
      else next.delete(code)
      return next
    })
  }

  const handleSave = async () => {
    if (!dirty) return
    setActionError(null)
    try {
      await mutation.mutateAsync({ roleId: role.roleId, grant, revoke })
      toast.success('تم تحديث صلاحيات الدور.')
    } catch (error) {
      setActionError(toArabicErrorMessage(error))
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={`صلاحيات «${role.nameAr}»`}
      description="حدد الصلاحيات الممنوحة لهذا الدور. تُحفظ التغييرات دفعة واحدة."
      size="lg"
      footer={
        <>
          <Button
            type="button"
            variant="ghost"
            disabled={!dirty || mutation.isPending}
            onClick={() => {
              setDraft(new Set(role.permissions))
              setActionError(null)
            }}
          >
            إعادة التعيين
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            إغلاق
          </Button>
          <Button
            type="button"
            loading={mutation.isPending}
            disabled={!dirty}
            onClick={handleSave}
          >
            حفظ التغييرات
          </Button>
        </>
      }
    >
      <div className="flex flex-wrap items-center gap-2 text-xs text-muted">
        <Badge variant="accent" className="gap-1" dir="ltr">
          {role.code}
        </Badge>
        <span>{draft.size} صلاحية محددة</span>
        {role.isSystemRole && <Badge variant="outline">دور معرّف من النظام</Badge>}
      </div>

      {catalogQuery.isLoading ? (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 4 }, (_, index) => (
            <Skeleton key={index} className="h-12 w-full" />
          ))}
        </div>
      ) : catalogQuery.isError ? (
        <div className="flex flex-col gap-2 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          <span>{toArabicErrorMessage(catalogQuery.error)}</span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => void catalogQuery.refetch()}
          >
            إعادة المحاولة
          </Button>
        </div>
      ) : (
        <div className="max-h-[52vh] overflow-y-auto pe-1">
          <div className="flex flex-col gap-4">
            {groups.map((group) => (
              <section key={group.module} className="flex flex-col gap-2">
                <h3 className="flex items-center gap-2 text-sm font-semibold text-foreground">
                  <KeyRound className="size-4 text-accent" aria-hidden />
                  {group.label}
                </h3>
                <ul className="grid gap-2 sm:grid-cols-2">
                  {group.items.map((permission) => (
                    <li
                      key={permission.code}
                      className="flex items-start justify-between gap-3 rounded-lg border border-border px-3 py-2"
                    >
                      <Checkbox
                        checked={draft.has(permission.code)}
                        onCheckedChange={(checked) =>
                          toggle(permission.code, checked)
                        }
                        disabled={mutation.isPending}
                        label={permission.nameAr}
                      />
                      <span
                        dir="ltr"
                        className="mt-0.5 shrink-0 rounded border border-border px-1.5 py-0.5 font-mono text-[10px] leading-none text-muted"
                      >
                        {permission.code}
                      </span>
                    </li>
                  ))}
                </ul>
              </section>
            ))}
          </div>
        </div>
      )}

      {actionError && (
        <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          {actionError}
        </p>
      )}
    </Dialog>
  )
}
