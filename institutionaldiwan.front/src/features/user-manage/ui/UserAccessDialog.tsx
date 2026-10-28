import { useMemo, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { CalendarDays, KeyRound, Network } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartments } from '@/entities/department'
import {
  ASSIGNMENT_TYPE,
  LOOKUP_SLUGS,
  useLookupEntries,
} from '@/entities/lookup'
import { permissionModuleLabel, usePermissionsCatalog } from '@/entities/role'
import {
  useSessionStore,
  useUserEffectivePermissions,
  UserScopeBadge,
  type UserResponse,
} from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { formatDate, formatShortId } from '@/shared/lib'
import {
  Badge,
  Button,
  Checkbox,
  Dialog,
  FormField,
  Input,
  Select,
  Skeleton,
} from '@/shared/ui'
import {
  departmentAssignmentSchema,
  type DepartmentAssignmentFormValues,
} from '../model/schemas'
import {
  useAssignDepartmentScopeMutation,
  useEndDepartmentScopeMutation,
} from '../api/userMutations'

function todayInputValue(): string {
  const now = new Date()
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 10)
}

function defaultAssignmentValues(): DepartmentAssignmentFormValues {
  return {
    departmentId: '',
    assignmentTypeId: String(ASSIGNMENT_TYPE.Additional),
    includeSubtree: false,
    startDate: todayInputValue(),
    endDate: '',
  }
}

interface UserAccessDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user: UserResponse
}

/**
 * Read-only effective authorization (scope, roles, permissions) plus the
 * temporal department assignments managed through `/department-assignments`.
 */
export function UserAccessDialog({ open, onOpenChange, user }: UserAccessDialogProps) {
  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManageDepartments = hasPermission(Permissions.DepartmentManage)
  const canManageRoles = hasPermission(Permissions.RoleManage)

  const effectiveQuery = useUserEffectivePermissions(user.userId)
  const departmentsQuery = useDepartments({ enabled: canManageDepartments })
  const assignmentTypesQuery = useLookupEntries(LOOKUP_SLUGS.AssignmentTypes)
  const permissionsQuery = usePermissionsCatalog({ enabled: canManageRoles })

  const assignMutation = useAssignDepartmentScopeMutation()
  const endMutation = useEndDepartmentScopeMutation()

  const [endingId, setEndingId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const effective = effectiveQuery.data

  const departmentById = useMemo(
    () =>
      new Map(
        (departmentsQuery.data ?? []).map((department) => [
          department.departmentId,
          department,
        ]),
      ),
    [departmentsQuery.data],
  )

  const assignmentTypeOptions = useMemo(
    () =>
      (assignmentTypesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [assignmentTypesQuery.data],
  )

  const departmentOptions = useMemo(
    () =>
      (departmentsQuery.data ?? []).map((department) => ({
        value: department.departmentId,
        label: `${department.nameAr} (${department.code})`,
      })),
    [departmentsQuery.data],
  )

  const groupedPermissions = useMemo(() => {
    const codes = effective?.permissions ?? []
    if (codes.length === 0) return []

    const catalog = permissionsQuery.data
    if (!catalog) {
      return [
        {
          module: 'ALL',
          label: 'الصلاحيات',
          items: codes.map((code) => ({ code, name: code })),
        },
      ]
    }

    const granted = new Set<string>(codes)
    const byModule = new Map<string, { code: string; name: string }[]>()

    for (const permission of catalog) {
      if (!granted.has(permission.code)) continue
      const items = byModule.get(permission.moduleName) ?? []
      items.push({ code: permission.code, name: permission.nameAr })
      byModule.set(permission.moduleName, items)
    }

    return Array.from(byModule.entries())
      .map(([module, items]) => ({
        module,
        label: permissionModuleLabel(module),
        items,
      }))
      .sort((left, right) => left.label.localeCompare(right.label, 'ar'))
  }, [effective, permissionsQuery.data])

  const assignments = useMemo(() => {
    const rows = [...(effective?.departmentAssignments ?? [])]
    rows.sort((left, right) => {
      if (left.isActive !== right.isActive) return left.isActive ? -1 : 1
      return right.startDate.localeCompare(left.startDate)
    })
    return rows
  }, [effective])

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<DepartmentAssignmentFormValues>({
    resolver: zodResolver(departmentAssignmentSchema),
    defaultValues: defaultAssignmentValues(),
  })

  const onSubmit = handleSubmit(async (values) => {
    setActionError(null)
    try {
      await assignMutation.mutateAsync({
        userId: user.userId,
        request: {
          departmentId: values.departmentId,
          assignmentTypeId: Number(values.assignmentTypeId),
          includeSubtree: values.includeSubtree,
          startDate: values.startDate,
          endDate: values.endDate || null,
        },
      })
      reset(defaultAssignmentValues())
      toast.success('تمت إضافة إسناد القسم.')
    } catch (error) {
      setActionError(toArabicErrorMessage(error))
    }
  })

  const handleEnd = async (assignmentId: string) => {
    setEndingId(assignmentId)
    setActionError(null)
    try {
      await endMutation.mutateAsync({ userId: user.userId, assignmentId })
      toast.success('تم إنهاء إسناد القسم.')
    } catch (error) {
      setActionError(toArabicErrorMessage(error))
    } finally {
      setEndingId(null)
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="الصلاحيات والإسنادات"
      description={`نطاق الوصول والصلاحيات الفعلية وإسنادات الأقسام للمستخدم «${user.username}».`}
      size="lg"
      footer={
        <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
          إغلاق
        </Button>
      }
    >
      {effectiveQuery.isLoading ? (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-6 w-40" />
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
        </div>
      ) : effectiveQuery.isError ? (
        <div className="flex flex-col gap-2 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          <span>{toArabicErrorMessage(effectiveQuery.error)}</span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => void effectiveQuery.refetch()}
          >
            إعادة المحاولة
          </Button>
        </div>
      ) : effective ? (
        <>
          <section className="flex flex-col gap-3">
            <h3 className="flex items-center gap-2 text-sm font-semibold text-foreground">
              <KeyRound className="size-4 text-accent" aria-hidden />
              الصلاحيات الفعلية
            </h3>

            <div className="flex flex-wrap items-center gap-2 text-sm">
              <UserScopeBadge scopeTypeId={effective.scopeTypeId} />
              {effective.roleCodes.length === 0 ? (
                <span className="text-muted">لا أدوار مسندة</span>
              ) : (
                effective.roleCodes.map((code) => (
                  <Badge key={code} variant="accent">
                    <span dir="ltr">{code}</span>
                  </Badge>
                ))
              )}
              <Badge variant="outline">
                {effective.permissions.length} صلاحية
              </Badge>
            </div>

            {groupedPermissions.length === 0 ? (
              <p className="rounded-lg bg-surface-muted px-3 py-2 text-sm text-muted">
                لا توجد صلاحيات فعلية لهذا المستخدم.
              </p>
            ) : (
              <div className="grid gap-2 sm:grid-cols-2">
                {groupedPermissions.map((group) => (
                  <div
                    key={group.module}
                    className="rounded-lg border border-border px-3 py-2"
                  >
                    <p className="mb-1.5 text-xs font-semibold text-muted">
                      {group.label}
                    </p>
                    <div className="flex flex-wrap gap-1">
                      {group.items.map((item) => (
                        <span
                          key={item.code}
                          title={item.code}
                          className="rounded-md bg-surface-muted px-2 py-0.5 text-xs text-foreground"
                        >
                          {item.name}
                        </span>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </section>

          <section className="flex flex-col gap-3 border-t border-border pt-4">
            <h3 className="flex items-center gap-2 text-sm font-semibold text-foreground">
              <Network className="size-4 text-accent" aria-hidden />
              إسنادات الأقسام
            </h3>

            {assignments.length === 0 ? (
              <p className="rounded-lg bg-surface-muted px-3 py-2 text-sm text-muted">
                لا توجد إسنادات أقسام لهذا المستخدم.
              </p>
            ) : (
              <ul className="flex flex-col gap-2">
                {assignments.map((assignment) => {
                  const department = departmentById.get(assignment.departmentId)
                  const typeLabel =
                    assignment.assignmentTypeId === ASSIGNMENT_TYPE.Primary
                      ? 'أساسي'
                      : 'إضافي'

                  return (
                    <li
                      key={assignment.assignmentId}
                      className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-border px-3 py-2"
                    >
                      <div className="flex min-w-0 flex-col gap-1">
                        <span className="flex flex-wrap items-center gap-2 text-sm text-foreground">
                          {department ? (
                            <>
                              <span className="font-medium">{department.nameAr}</span>
                              <span
                                dir="ltr"
                                className="rounded border border-border px-1.5 py-0.5 font-mono text-[11px] text-muted"
                              >
                                {department.code}
                              </span>
                            </>
                          ) : (
                            <span dir="ltr" className="font-mono text-xs text-muted">
                              {formatShortId(assignment.departmentId)}
                            </span>
                          )}
                          <Badge variant="neutral">{typeLabel}</Badge>
                          {assignment.includeSubtree && (
                            <Badge variant="accent">يشمل الفروع</Badge>
                          )}
                          <Badge variant={assignment.isActive ? 'success' : 'outline'}>
                            {assignment.isActive ? 'فعّال' : 'منتهٍ'}
                          </Badge>
                        </span>
                        <span className="flex items-center gap-1 text-xs text-muted">
                          <CalendarDays className="size-3.5" aria-hidden />
                          <span dir="ltr">{formatDate(assignment.startDate)}</span>
                          <span>—</span>
                          <span dir="ltr">
                            {assignment.endDate
                              ? formatDate(assignment.endDate)
                              : 'مفتوح'}
                          </span>
                        </span>
                      </div>

                      {assignment.isActive && canManageDepartments && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          className="text-danger"
                          loading={endingId === assignment.assignmentId}
                          disabled={endMutation.isPending}
                          onClick={() => handleEnd(assignment.assignmentId)}
                        >
                          إنهاء
                        </Button>
                      )}
                    </li>
                  )
                })}
              </ul>
            )}

            {!canManageDepartments ? (
              <p
                role="status"
                className="rounded-lg bg-warning/10 px-3 py-2 text-xs text-warning"
              >
                تحتاج صلاحية إدارة الأقسام لعرض قائمة الأقسام وإدارتها.
              </p>
            ) : (
              <form
                onSubmit={onSubmit}
                noValidate
                className="grid gap-3 rounded-lg bg-surface-muted/60 p-3 sm:grid-cols-2"
              >
                <p className="text-xs font-semibold text-muted sm:col-span-2">
                  إضافة إسناد قسم
                </p>

                <FormField
                  label="القسم"
                  required
                  error={errors.departmentId?.message}
                >
                  <Controller
                    control={control}
                    name="departmentId"
                    render={({ field }) => (
                      <Select
                        value={field.value || undefined}
                        onValueChange={field.onChange}
                        options={departmentOptions}
                        disabled={departmentsQuery.isLoading}
                        invalid={Boolean(errors.departmentId)}
                        placeholder={
                          departmentsQuery.isLoading
                            ? 'جارٍ تحميل الأقسام…'
                            : 'اختر القسم…'
                        }
                        aria-label="قسم الإسناد"
                      />
                    )}
                  />
                </FormField>

                <FormField
                  label="نوع الإسناد"
                  required
                  error={errors.assignmentTypeId?.message}
                >
                  <Controller
                    control={control}
                    name="assignmentTypeId"
                    render={({ field }) => (
                      <Select
                        value={field.value || undefined}
                        onValueChange={field.onChange}
                        options={assignmentTypeOptions}
                        invalid={Boolean(errors.assignmentTypeId)}
                        placeholder="اختر النوع…"
                        aria-label="نوع الإسناد"
                      />
                    )}
                  />
                </FormField>

                <FormField
                  label="تاريخ البدء"
                  required
                  error={errors.startDate?.message}
                >
                  <Input
                    id="assignment-start"
                    type="date"
                    dir="ltr"
                    invalid={Boolean(errors.startDate)}
                    {...register('startDate')}
                  />
                </FormField>

                <FormField label="تاريخ الانتهاء" error={errors.endDate?.message}>
                  <Input
                    id="assignment-end"
                    type="date"
                    dir="ltr"
                    invalid={Boolean(errors.endDate)}
                    {...register('endDate')}
                  />
                </FormField>

                <div className="sm:col-span-2">
                  <Controller
                    control={control}
                    name="includeSubtree"
                    render={({ field }) => (
                      <Checkbox
                        checked={field.value}
                        onCheckedChange={field.onChange}
                        label="يشمل القسم والأقسام الفرعية"
                      />
                    )}
                  />
                </div>

                {departmentsQuery.isError && (
                  <p
                    role="alert"
                    className="sm:col-span-2 rounded-lg bg-danger/10 px-3 py-2 text-xs text-danger"
                  >
                    {toArabicErrorMessage(departmentsQuery.error)}
                  </p>
                )}

                <div className="flex justify-end sm:col-span-2">
                  <Button
                    type="submit"
                    size="sm"
                    loading={assignMutation.isPending}
                    disabled={departmentOptions.length === 0}
                  >
                    إضافة الإسناد
                  </Button>
                </div>
              </form>
            )}
          </section>
        </>
      ) : null}

      {actionError && (
        <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          {actionError}
        </p>
      )}
    </Dialog>
  )
}
