import { useEffect, useMemo } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Building2, Hash, UserRound } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentById, useDepartments } from '@/entities/department'
import { EmployeePicker } from '@/entities/employee'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import {
  Button,
  Checkbox,
  Dialog,
  FormField,
  Input,
  Select,
  Skeleton,
  Textarea,
} from '@/shared/ui'
import {
  useCreateDepartmentMutation,
  useUpdateDepartmentMutation,
} from '../api/departmentMutations'
import { collectDescendantIds } from '../model/hierarchy'
import { departmentFormSchema, type DepartmentFormValues } from '../model/schemas'

/** Radix Select forbids empty item values, so optional pickers use a sentinel. */
const NONE = 'none'

export type DepartmentFormTarget =
  | { mode: 'create'; parentDepartmentId: string | null }
  | { mode: 'edit'; departmentId: string }

interface DepartmentFormDialogProps {
  /** `null` keeps the dialog closed. */
  target: DepartmentFormTarget | null
  onOpenChange: (open: boolean) => void
}

/** Create/edit form for `POST /api/departments` and `PUT /api/departments/{id}`. */
export function DepartmentFormDialog({ target, onOpenChange }: DepartmentFormDialogProps) {
  if (!target) return null

  return (
    <DepartmentFormDialogContent
      key={
        target.mode === 'edit'
          ? `edit:${target.departmentId}`
          : `create:${target.parentDepartmentId ?? 'root'}`
      }
      target={target}
      onOpenChange={onOpenChange}
    />
  )
}

function DepartmentFormDialogContent({
  target,
  onOpenChange,
}: {
  target: DepartmentFormTarget
  onOpenChange: (open: boolean) => void
}) {
  const isEdit = target.mode === 'edit'
  const editingId = target.mode === 'edit' ? target.departmentId : null

  const detailQuery = useDepartmentById(editingId)
  const department = detailQuery.data
  const departmentsQuery = useDepartments()
  const typeQuery = useLookupEntries(LOOKUP_SLUGS.DepartmentTypes)

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canPickEmployees = hasPermission(Permissions.EmployeeManage)

  const createMutation = useCreateDepartmentMutation()
  const updateMutation = useUpdateDepartmentMutation()
  const pending = createMutation.isPending || updateMutation.isPending
  const mutationError = createMutation.error ?? updateMutation.error

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<DepartmentFormValues>({
    resolver: zodResolver(departmentFormSchema),
    defaultValues: {
      code: '',
      nameAr: '',
      nameEn: '',
      departmentTypeId: '',
      parentDepartmentId:
        target.mode === 'create' ? (target.parentDepartmentId ?? NONE) : NONE,
      managerEmployeeId: NONE,
      canSendExternal: false,
      canReceiveExternal: false,
      notes: '',
    },
  })

  useEffect(() => {
    if (target.mode !== 'edit' || !department) return

    reset({
      code: department.code,
      nameAr: department.nameAr,
      nameEn: department.nameEn ?? '',
      departmentTypeId: String(department.departmentTypeId),
      parentDepartmentId: department.parentDepartmentId ?? NONE,
      managerEmployeeId: department.managerEmployeeId ?? NONE,
      canSendExternal: department.canSendExternal,
      canReceiveExternal: department.canReceiveExternal,
      notes: department.notes ?? '',
    })
  }, [target, department, reset])

  const typeOptions = useMemo(
    () =>
      (typeQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [typeQuery.data],
  )

  const parentOptions = useMemo(() => {
    const departments = [...(departmentsQuery.data ?? [])].sort((left, right) =>
      left.nameAr.localeCompare(right.nameAr, 'ar'),
    )
    const excluded = new Set<string>()
    if (editingId) {
      excluded.add(editingId)
      collectDescendantIds(departments, editingId).forEach((id) => excluded.add(id))
    }

    return [
      { value: NONE, label: 'بدون قسم أعلى (قسم رئيسي)' },
      ...departments
        .filter((item) => !excluded.has(item.departmentId))
        .map((item) => ({
          value: item.departmentId,
          label: `${item.nameAr} (${item.code})`,
          disabled: !item.isActive,
        })),
    ]
  }, [departmentsQuery.data, editingId])

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (target.mode === 'edit') {
        await updateMutation.mutateAsync({
          departmentId: target.departmentId,
          request: {
            nameAr: values.nameAr,
            parentDepartmentId:
              values.parentDepartmentId === NONE ? null : values.parentDepartmentId,
            canSendExternal: values.canSendExternal,
            canReceiveExternal: values.canReceiveExternal,
            nameEn: values.nameEn || null,
            managerEmployeeId:
              values.managerEmployeeId === NONE ? null : values.managerEmployeeId,
            notes: values.notes || null,
          },
        })
        toast.success('تم تحديث بيانات القسم.')
      } else {
        await createMutation.mutateAsync({
          code: values.code,
          nameAr: values.nameAr,
          departmentTypeId: Number(values.departmentTypeId),
          parentDepartmentId:
            values.parentDepartmentId === NONE ? null : values.parentDepartmentId,
          nameEn: values.nameEn || null,
          canSendExternal: values.canSendExternal,
          canReceiveExternal: values.canReceiveExternal,
          notes: values.notes || null,
        })
        toast.success('تم إنشاء القسم بنجاح.')
      }
      onOpenChange(false)
    } catch {
      // The mutation error is rendered below in Arabic.
    }
  })

  const loadingDetail = isEdit && detailQuery.isLoading

  return (
    <Dialog
      open
      onOpenChange={onOpenChange}
      title={isEdit ? 'تعديل القسم' : 'قسم جديد'}
      description={
        isEdit
          ? 'حدّث اسم القسم وقسمه الأعلى ومديره وملاحظاته. لا يمكن تغيير الرمز أو النوع.'
          : 'أضف قسمًا تنظيميًا واربطه بقسم أعلى. يمكن إسناد مدير القسم بعد إنشائه.'
      }
      size="lg"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={pending}
          >
            إلغاء
          </Button>
          <Button
            type="submit"
            form="department-form"
            loading={pending}
            disabled={loadingDetail || (isEdit && detailQuery.isError)}
          >
            {isEdit ? 'حفظ التعديلات' : 'إنشاء القسم'}
          </Button>
        </>
      }
    >
      {loadingDetail ? (
        <div className="flex flex-col gap-3">
          {Array.from({ length: 6 }, (_, index) => (
            <Skeleton key={index} className="h-10 w-full" />
          ))}
        </div>
      ) : isEdit && detailQuery.error ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
        >
          <span>{toArabicErrorMessage(detailQuery.error)}</span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => void detailQuery.refetch()}
          >
            إعادة المحاولة
          </Button>
        </div>
      ) : (
        <form
          id="department-form"
          onSubmit={onSubmit}
          noValidate
          className="grid gap-4 sm:grid-cols-2"
        >
          <FormField label="رمز القسم" htmlFor="department-code" required error={errors.code?.message}>
            <Input
              id="department-code"
              dir="ltr"
              placeholder="مثال: HR-01"
              startIcon={<Hash className="size-4" aria-hidden />}
              disabled={isEdit}
              invalid={Boolean(errors.code)}
              {...register('code')}
            />
          </FormField>

          <FormField label="نوع القسم" required error={errors.departmentTypeId?.message}>
            <Controller
              control={control}
              name="departmentTypeId"
              render={({ field }) => (
                <Select
                  value={field.value || undefined}
                  onValueChange={field.onChange}
                  options={typeOptions}
                  disabled={isEdit || typeQuery.isLoading}
                  invalid={Boolean(errors.departmentTypeId)}
                  placeholder={typeQuery.isLoading ? 'جارٍ تحميل الأنواع…' : 'اختر النوع…'}
                  aria-label="نوع القسم"
                />
              )}
            />
          </FormField>

          <FormField
            label="اسم القسم بالعربية"
            htmlFor="department-name-ar"
            required
            error={errors.nameAr?.message}
            className="sm:col-span-2"
          >
            <Input
              id="department-name-ar"
              placeholder="مثال: دائرة الموارد البشرية"
              startIcon={<Building2 className="size-4" aria-hidden />}
              invalid={Boolean(errors.nameAr)}
              {...register('nameAr')}
            />
          </FormField>

          <FormField
            label="اسم القسم بالإنجليزية"
            htmlFor="department-name-en"
            error={errors.nameEn?.message}
            className="sm:col-span-2"
          >
            <Input
              id="department-name-en"
              dir="ltr"
              placeholder="Example: Human Resources Department"
              invalid={Boolean(errors.nameEn)}
              {...register('nameEn')}
            />
          </FormField>

          <FormField
            label="القسم الأعلى"
            error={errors.parentDepartmentId?.message}
            className="sm:col-span-2"
          >
            <Controller
              control={control}
              name="parentDepartmentId"
              render={({ field }) => (
                <Select
                  value={field.value}
                  onValueChange={field.onChange}
                  options={parentOptions}
                  disabled={pending || departmentsQuery.isLoading}
                  invalid={Boolean(errors.parentDepartmentId)}
                  placeholder={
                    departmentsQuery.isLoading ? 'جارٍ تحميل الأقسام…' : 'اختر القسم الأعلى…'
                  }
                  aria-label="القسم الأعلى"
                />
              )}
            />
          </FormField>

          {isEdit && (
            <FormField
              label="مدير القسم"
              error={errors.managerEmployeeId?.message}
              className="sm:col-span-2"
            >
              <Controller
                control={control}
                name="managerEmployeeId"
                render={({ field }) => (
                  <EmployeePicker
                    value={field.value === NONE ? null : field.value}
                    onValueChange={(id) => field.onChange(id ?? NONE)}
                    disabled={pending || !canPickEmployees}
                    invalid={Boolean(errors.managerEmployeeId)}
                    label="مدير القسم"
                    noneLabel="بدون مدير"
                  />
                )}
              />
              {!canPickEmployees && (
                <p role="status" className="mt-1.5 text-xs text-warning">
                  لا تملك صلاحية إدارة الموظفين، لذلك لا يمكنك تغيير مدير القسم.
                </p>
              )}
            </FormField>
          )}

          <div className="flex flex-col gap-3 sm:col-span-2">
            <Controller
              control={control}
              name="canSendExternal"
              render={({ field }) => (
                <Checkbox
                  checked={field.value}
                  onCheckedChange={field.onChange}
                  label="السماح بالمراسلات الخارجية الصادرة"
                  description="يستطيع القسم إرسال وثائق إلى جهات خارج المؤسسة."
                />
              )}
            />
            <Controller
              control={control}
              name="canReceiveExternal"
              render={({ field }) => (
                <Checkbox
                  checked={field.value}
                  onCheckedChange={field.onChange}
                  label="السماح بالمراسلات الخارجية الواردة"
                  description="يستطيع القسم استقبال وثائق من جهات خارج المؤسسة."
                />
              )}
            />
          </div>

          <FormField
            label="ملاحظات"
            htmlFor="department-notes"
            error={errors.notes?.message}
            className="sm:col-span-2"
          >
            <Textarea
              id="department-notes"
              placeholder="ملاحظات إدارية عن القسم (اختياري)…"
              invalid={Boolean(errors.notes)}
              {...register('notes')}
            />
          </FormField>

          {!isEdit && (
            <p role="status" className="sm:col-span-2 rounded-lg bg-surface-muted px-3 py-2 text-xs text-muted">
              <UserRound className="me-1 inline size-3.5" aria-hidden />
              لإسناد مدير للقسم، افتحه للتعديل بعد إنشائه.
            </p>
          )}

          {(departmentsQuery.isError || typeQuery.isError) && (
            <div role="alert" className="sm:col-span-2 flex items-center justify-between gap-2 rounded-lg bg-danger/10 px-3 py-2 text-xs text-danger">
              <span>{toArabicErrorMessage(departmentsQuery.error ?? typeQuery.error)}</span>
              <Button type="button" variant="outline" size="sm" onClick={() => {
                void departmentsQuery.refetch()
                void typeQuery.refetch()
              }}>
                إعادة المحاولة
              </Button>
            </div>
          )}

          {mutationError && (
            <p
              role="alert"
              className="sm:col-span-2 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
            >
              {toArabicErrorMessage(mutationError)}
            </p>
          )}
        </form>
      )}
    </Dialog>
  )
}
