import { useMemo } from 'react'
import { Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Lock, User } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { EmployeePicker } from '@/entities/employee'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { type UserResponse } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { formatShortId, t, useDraftForm } from '@/shared/lib'
import { Button, Checkbox, Dialog, FormField, Input, Label, Select } from '@/shared/ui'
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
        label: t('user.form.current-department', { id: formatShortId(user.primaryDepartmentId) }),
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
    draft,
  } = useDraftForm<UserFormValues>({ formId: `user-form:${user?.userId ?? 'new'}` }, {
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
        toast.success(t('user.form.toast-updated'))
      } else {
        await createMutation.mutateAsync({
          username: values.username,
          password: values.password,
          primaryDepartmentId: values.primaryDepartmentId,
          employeeId,
          scopeTypeId: Number(values.scopeTypeId),
          mustChangePassword: values.mustChangePassword,
        })
        toast.success(t('user.form.toast-created'))
      }
      draft.discard()
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
      title={isEdit ? t('user.form.edit-title') : t('user.create')}
      description={
        isEdit
          ? t('user.form.edit-description')
          : t('user.form.create-description')
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
            <Label k="common.action.cancel" />
          </Button>
          <Button
            type="submit"
            form="user-form"
            loading={pending}
            disabled={noDepartments}
          >
            {isEdit ? <Label k="user.form.save" /> : <Label k="user.form.submit" />}
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
          labelKey="user.column.username"
          required
          error={errors.username?.message}
          guideId="users.form-username"
          className="sm:col-span-2"
        >
          <Input
            id="user-username"
            autoComplete="username"
            placeholder={t('user.form.username-placeholder')}
            dir="ltr"
            disabled={isEdit}
            startIcon={<User className="size-4" aria-hidden />}
            invalid={Boolean(errors.username)}
            {...register('username')}
          />
        </FormField>

        {!isEdit && (
          <FormField
            labelKey="user.form.password"
            required
            error={errors.password?.message}
            guideId="users.form-password"
            className="sm:col-span-2"
          >
            <Input
              id="user-password"
              type="password"
              autoComplete="new-password"
              placeholder={t('user.form.password-placeholder')}
              dir="ltr"
              startIcon={<Lock className="size-4" aria-hidden />}
              invalid={Boolean(errors.password)}
              {...register('password')}
            />
          </FormField>
        )}

        <FormField
          labelKey="user.column.department"
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
                  departmentsQuery.isLoading ? t('user.form.department-loading') : t('user.form.department-placeholder')
                }
                aria-label={t('user.column.department')}
              />
            )}
          />
        </FormField>

        <FormField labelKey="user.column.employee" error={errors.employeeId?.message}>
          <Controller
            control={control}
            name="employeeId"
            render={({ field }) => (
              <EmployeePicker
                value={field.value === NO_EMPLOYEE ? null : field.value || null}
                onValueChange={(id) => field.onChange(id ?? NO_EMPLOYEE)}
                disabled={pending}
                invalid={Boolean(errors.employeeId)}
                label={t('user.column.employee')}
                noneLabel={t('user.form.employee-none')}
              />
            )}
          />
        </FormField>

        <FormField
          labelKey="user.form.scope"
          required
          error={errors.scopeTypeId?.message}
          guideId="users.form-scope"
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
                placeholder={t('user.form.scope-placeholder')}
                aria-label={t('user.form.scope')}
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
                label={t('user.form.must-change-label')}
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
