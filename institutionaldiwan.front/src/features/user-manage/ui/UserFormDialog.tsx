import { useMemo } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Lock, User } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { EmployeePicker } from '@/entities/employee'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { type UserResponse } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { formatShortId } from '@/shared/lib'
import { Button, Checkbox, Dialog, FormField, Input, Select } from '@/shared/ui'
import {
  createUserFormSchema,
  updateUserFormSchema,
  type UserFormValues,
} from '../model/schemas'
import { useCreateUserMutation, useUpdateUserMutation } from '../api/userMutations'

const NO_EMPLOYEE = 'none'

interface UserFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** `null` creates a new account; a user edits the existing one. */
  user: UserResponse | null
}

/** Create/edit form for `POST /api/users` and `PUT /api/users/{id}`. */
export function UserFormDialog({ open, onOpenChange, user }: UserFormDialogProps) {
  const isEdit = user !== null
  const departmentsQuery = useDepartmentOptions({ enabled: open })
  const scopeQuery = useLookupEntries(LOOKUP_SLUGS.UserScopeTypes)

  const createMutation = useCreateUserMutation()
  const updateMutation = useUpdateUserMutation()
  const pending = createMutation.isPending || updateMutation.isPending
  const mutationError = createMutation.error ?? updateMutation.error

  const departmentOptions = useMemo(() => {
    const options = (departmentsQuery.data ?? []).map((department) => ({
      value: department.departmentId,
      label: `${department.nameAr} (${department.code})`,
    }))
    if (user && !options.some((option) => option.value === user.primaryDepartmentId)) {
      options.unshift({
        value: user.primaryDepartmentId,
        label: `القسم الحالي (${formatShortId(user.primaryDepartmentId)})`,
      })
    }
    return options
  }, [departmentsQuery.data, user])

  const scopeOptions = useMemo(
    () =>
      (scopeQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [scopeQuery.data],
  )

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<UserFormValues>({
    resolver: zodResolver(isEdit ? updateUserFormSchema : createUserFormSchema),
    defaultValues: {
      username: user?.username ?? '',
      password: '',
      primaryDepartmentId: user?.primaryDepartmentId ?? '',
      employeeId: user?.employeeId ?? NO_EMPLOYEE,
      scopeTypeId: String(user?.scopeTypeId ?? 1),
      mustChangePassword: user?.mustChangePassword ?? true,
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    const employeeId =
      values.employeeId === NO_EMPLOYEE ? null : values.employeeId || null

    try {
      if (user) {
        await updateMutation.mutateAsync({
          userId: user.userId,
          request: {
            primaryDepartmentId: values.primaryDepartmentId,
            employeeId,
            scopeTypeId: Number(values.scopeTypeId),
            mustChangePassword: values.mustChangePassword,
          },
        })
        toast.success('تم تحديث بيانات المستخدم.')
      } else {
        await createMutation.mutateAsync({
          username: values.username,
          password: values.password,
          primaryDepartmentId: values.primaryDepartmentId,
          employeeId,
          scopeTypeId: Number(values.scopeTypeId),
          mustChangePassword: values.mustChangePassword,
        })
        toast.success('تم إنشاء المستخدم بنجاح.')
      }
      onOpenChange(false)
    } catch {
      // The mutation error is rendered below in Arabic.
    }
  })

  const noDepartments = departmentOptions.length === 0

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={isEdit ? 'تعديل بيانات المستخدم' : 'مستخدم جديد'}
      description={
        isEdit
          ? 'حدّث القسم الأساسي وربط الموظف ونطاق الوصول. لا يمكن تغيير اسم المستخدم.'
          : 'أنشئ حسابًا جديدًا. تُمنح الأدوار لاحقًا من قائمة إدارة الأدوار.'
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
            form="user-form"
            loading={pending}
            disabled={noDepartments}
          >
            {isEdit ? 'حفظ التعديلات' : 'إنشاء المستخدم'}
          </Button>
        </>
      }
    >
      <form
        id="user-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <FormField
          label="اسم المستخدم"
          required
          error={errors.username?.message}
          className="sm:col-span-2"
        >
          <Input
            id="user-username"
            autoComplete="username"
            placeholder="مثال: ahmed.khalil"
            dir="ltr"
            disabled={isEdit}
            startIcon={<User className="size-4" aria-hidden />}
            invalid={Boolean(errors.username)}
            {...register('username')}
          />
        </FormField>

        {!isEdit && (
          <FormField
            label="كلمة المرور المؤقتة"
            required
            error={errors.password?.message}
            className="sm:col-span-2"
          >
            <Input
              id="user-password"
              type="password"
              autoComplete="new-password"
              placeholder="8 أحرف على الأقل"
              dir="ltr"
              startIcon={<Lock className="size-4" aria-hidden />}
              invalid={Boolean(errors.password)}
              {...register('password')}
            />
          </FormField>
        )}

        <FormField
          label="القسم الأساسي"
          required
          error={errors.primaryDepartmentId?.message}
        >
          <Controller
            control={control}
            name="primaryDepartmentId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={departmentOptions}
                disabled={pending || departmentsQuery.isLoading}
                invalid={Boolean(errors.primaryDepartmentId)}
                placeholder={
                  departmentsQuery.isLoading ? 'جارٍ تحميل الأقسام…' : 'اختر القسم…'
                }
                aria-label="القسم الأساسي"
              />
            )}
          />
        </FormField>

        <FormField label="الموظف المرتبط" error={errors.employeeId?.message}>
          <Controller
            control={control}
            name="employeeId"
            render={({ field }) => (
              <EmployeePicker
                value={field.value === NO_EMPLOYEE ? null : field.value || null}
                onValueChange={(id) => field.onChange(id ?? NO_EMPLOYEE)}
                disabled={pending}
                invalid={Boolean(errors.employeeId)}
                label="الموظف المرتبط"
                noneLabel="بدون ربط بموظف"
              />
            )}
          />
        </FormField>

        <FormField
          label="نطاق المستخدم"
          required
          error={errors.scopeTypeId?.message}
        >
          <Controller
            control={control}
            name="scopeTypeId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={scopeOptions}
                disabled={pending}
                invalid={Boolean(errors.scopeTypeId)}
                placeholder="اختر النطاق…"
                aria-label="نطاق المستخدم"
              />
            )}
          />
        </FormField>

        <div className="sm:col-span-2">
          <Controller
            control={control}
            name="mustChangePassword"
            render={({ field }) => (
              <Checkbox
                checked={field.value}
                onCheckedChange={field.onChange}
                label="إلزام المستخدم بتغيير كلمة المرور عند أول تسجيل دخول"
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

        {scopeQuery.isError && (
          <p role="alert" className="sm:col-span-2 text-sm text-danger">
            {toArabicErrorMessage(scopeQuery.error)}
          </p>
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
    </Dialog>
  )
}
