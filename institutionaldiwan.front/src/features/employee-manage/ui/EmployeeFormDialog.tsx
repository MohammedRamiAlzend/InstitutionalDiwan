import { useMemo } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Briefcase, Hash, Mail, Phone, UserRound } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartments } from '@/entities/department'
import type { EmployeeResponse } from '@/entities/employee'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { formatShortId } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Select } from '@/shared/ui'
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
  const canManageDepartments = hasPermission(Permissions.DepartmentManage)

  const departmentsQuery = useDepartments({ enabled: canManageDepartments })

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
        label: `القسم الحالي (${formatShortId(employee.departmentId)})`,
      })
    }
    return options
  }, [departmentsQuery.data, employee])

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<EmployeeFormValues>({
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
        toast.success('تم تحديث بيانات الموظف.')
      } else {
        await createMutation.mutateAsync(request)
        toast.success('تم إنشاء الموظف بنجاح.')
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
      title={isEdit ? 'تعديل بيانات الموظف' : 'موظف جديد'}
      description={
        isEdit
          ? 'حدّث بيانات الموظف وقسمه الوظيفي. لا يمكن تغيير معرّف الموظف.'
          : 'أضف سجل موظف واربطه بقسم. يمكن ربط حساب مستخدم به لاحقًا.'
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
            form="employee-form"
            loading={pending}
            disabled={noDepartments}
          >
            {isEdit ? 'حفظ التعديلات' : 'إنشاء الموظف'}
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
          label="اسم الموظف الكامل بالعربية"
          required
          error={errors.fullNameAr?.message}
          className="sm:col-span-2"
        >
          <Input
            id="employee-full-name-ar"
            autoComplete="name"
            placeholder="مثال: أحمد محمد علي"
            startIcon={<UserRound className="size-4" aria-hidden />}
            invalid={Boolean(errors.fullNameAr)}
            {...register('fullNameAr')}
          />
        </FormField>

        <FormField label="الرقم الوظيفي" error={errors.employeeNumber?.message}>
          <Input
            id="employee-number"
            dir="ltr"
            placeholder="مثال: EMP-2024-001"
            startIcon={<Hash className="size-4" aria-hidden />}
            invalid={Boolean(errors.employeeNumber)}
            {...register('employeeNumber')}
          />
        </FormField>

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
                disabled={
                  pending || departmentsQuery.isLoading || !canManageDepartments
                }
                invalid={Boolean(errors.departmentId)}
                placeholder={
                  departmentsQuery.isLoading ? 'جارٍ تحميل الأقسام…' : 'اختر القسم…'
                }
                aria-label="القسم"
              />
            )}
          />
        </FormField>

        <FormField label="اسم الموظف الكامل بالإنجليزية" error={errors.fullNameEn?.message}>
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

        <FormField label="المسمى الوظيفي" error={errors.jobTitleAr?.message}>
          <Input
            id="employee-job-title"
            placeholder="مثال: مطوّر برمجيات"
            startIcon={<Briefcase className="size-4" aria-hidden />}
            invalid={Boolean(errors.jobTitleAr)}
            {...register('jobTitleAr')}
          />
        </FormField>

        <FormField label="البريد الإلكتروني" error={errors.email?.message}>
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

        <FormField label="رقم الهاتف" error={errors.phone?.message}>
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
              ? 'لا تملك صلاحية استعراض الأقسام، لذلك لا يمكنك نقل الموظف إلى قسم آخر.'
              : 'لا تملك صلاحية استعراض الأقسام، لذلك لا يمكنك إنشاء موظف دون اختيار قسم.'}
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
