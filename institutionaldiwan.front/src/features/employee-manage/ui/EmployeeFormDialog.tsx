import { useMemo } from 'react'
import { Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Briefcase, Hash, Mail, Phone, UserRound } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import type { EmployeeResponse } from '@/entities/employee'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { formatShortId, t, useDraftForm } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Label, Select } from '@/shared/ui'
import { employeeFormSchema, type EmployeeFormValues } from '../model/schemas'
import {
  useCreateEmployeeMutation,
  useUpdateEmployeeMutation,
} from '../api/employeeMutations'

interface EmployeeFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** `null` creates a new record; an employee edits the existing one. */
  employee: EmployeeResponse | null
}

/** Create/edit form for `POST /api/employees` and `PUT /api/employees/{id}`. */
export function EmployeeFormDialog({
  open,
  onOpenChange,
  employee,
}: EmployeeFormDialogProps) {
  const isEdit = employee !== null
  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManageDepartments = hasPermission(Permissions.EmployeeManage)

  const departmentsQuery = useDepartmentOptions({ enabled: open && canManageDepartments })

  const createMutation = useCreateEmployeeMutation()
  const updateMutation = useUpdateEmployeeMutation()
  const pending = createMutation.isPending || updateMutation.isPending
  const mutationError = createMutation.error ?? updateMutation.error

  const departmentOptions = useMemo(() => {
    const options = (departmentsQuery.data ?? []).map((department) => ({
      value: department.departmentId,
      label: `${department.nameAr} (${department.code})`,
    }))
    if (employee && !options.some((option) => option.value === employee.departmentId)) {
      options.unshift({
        value: employee.departmentId,
        label: t('employee.form.current-department', { id: formatShortId(employee.departmentId) }),
      })
    }
    return options
  }, [departmentsQuery.data, employee])

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<EmployeeFormValues>({ formId: `employee-form:${employee?.employeeId ?? 'new'}` }, {
    resolver: zodResolver(employeeFormSchema),
    defaultValues: {
      fullNameAr: employee?.fullNameAr ?? '',
      departmentId: employee?.departmentId ?? '',
      employeeNumber: employee?.employeeNumber ?? '',
      fullNameEn: employee?.fullNameEn ?? '',
      jobTitleAr: employee?.jobTitleAr ?? '',
      email: employee?.email ?? '',
      phone: employee?.phone ?? '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    const request = {
      fullNameAr: values.fullNameAr,
      departmentId: values.departmentId,
      employeeNumber: values.employeeNumber || null,
      fullNameEn: values.fullNameEn || null,
      jobTitleAr: values.jobTitleAr || null,
      email: values.email || null,
      phone: values.phone || null,
    }

    try {
      if (employee) {
        await updateMutation.mutateAsync({
          employeeId: employee.employeeId,
          request,
        })
        toast.success(t('employee.form.toast-updated'))
      } else {
        await createMutation.mutateAsync(request)
        toast.success(t('employee.form.toast-created'))
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
      title={isEdit ? t('employee.form.edit-title') : t('employee.create')}
      description={
        isEdit
          ? t('employee.form.edit-description')
          : t('employee.form.create-description')
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
            form="employee-form"
            loading={pending}
            disabled={noDepartments}
          >
            {isEdit ? <Label k="employee.form.save" /> : <Label k="employee.form.submit" />}
          </Button>
        </>
      }
    >
      <form
        id="employee-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <FormField
          labelKey="employee.form.name-ar"
          required
          error={errors.fullNameAr?.message}
          className="sm:col-span-2"
        >
          <Input
            id="employee-full-name-ar"
            autoComplete="name"
            placeholder={t('employee.form.name-ar-placeholder')}
            startIcon={<UserRound className="size-4" aria-hidden />}
            invalid={Boolean(errors.fullNameAr)}
            {...register('fullNameAr')}
          />
        </FormField>

        <FormField labelKey="employee.column.number" error={errors.employeeNumber?.message}>
          <Input
            id="employee-number"
            dir="ltr"
            placeholder={t('employee.form.number-placeholder')}
            startIcon={<Hash className="size-4" aria-hidden />}
            invalid={Boolean(errors.employeeNumber)}
            {...register('employeeNumber')}
          />
        </FormField>

        <FormField
          labelKey="employee.column.department"
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
                disabled={
                  pending || departmentsQuery.isLoading || !canManageDepartments
                }
                invalid={Boolean(errors.departmentId)}
                placeholder={
                  departmentsQuery.isLoading ? t('employee.form.department-loading') : t('employee.form.department-placeholder')
                }
                aria-label={t('employee.column.department')}
              />
            )}
          />
        </FormField>

        <FormField labelKey="employee.form.name-en" error={errors.fullNameEn?.message}>
          <Input
            id="employee-full-name-en"
            dir="ltr"
            autoComplete="name"
            placeholder="Example: Ahmed Mohammed Ali"
            startIcon={<UserRound className="size-4" aria-hidden />}
            invalid={Boolean(errors.fullNameEn)}
            {...register('fullNameEn')}
          />
        </FormField>

        <FormField labelKey="employee.column.job-title" error={errors.jobTitleAr?.message}>
          <Input
            id="employee-job-title"
            placeholder={t('employee.form.job-title-placeholder')}
            startIcon={<Briefcase className="size-4" aria-hidden />}
            invalid={Boolean(errors.jobTitleAr)}
            {...register('jobTitleAr')}
          />
        </FormField>

        <FormField labelKey="employee.column.email" error={errors.email?.message}>
          <Input
            id="employee-email"
            type="email"
            dir="ltr"
            autoComplete="email"
            placeholder="name@example.com"
            startIcon={<Mail className="size-4" aria-hidden />}
            invalid={Boolean(errors.email)}
            {...register('email')}
          />
        </FormField>

        <FormField labelKey="employee.form.phone" error={errors.phone?.message}>
          <Input
            id="employee-phone"
            type="tel"
            dir="ltr"
            autoComplete="tel"
            placeholder="+963 9xx xxx xxx"
            startIcon={<Phone className="size-4" aria-hidden />}
            invalid={Boolean(errors.phone)}
            {...register('phone')}
          />
        </FormField>

        {!canManageDepartments && (
          <p
            role="status"
            className="sm:col-span-2 rounded-lg bg-warning/10 px-3 py-2 text-xs text-warning"
          >
            {isEdit
              ? t('employee.form.denied-edit')
              : t('employee.form.denied-create')}
          </p>
        )}

        {departmentsQuery.isError && canManageDepartments && (
          <p
            role="alert"
            className="sm:col-span-2 rounded-lg bg-danger/10 px-3 py-2 text-xs text-danger"
          >
            {toArabicErrorMessage(departmentsQuery.error)}
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
