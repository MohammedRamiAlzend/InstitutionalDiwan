import { useMemo } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Select, Textarea } from '@/shared/ui'
import { useRecordDepartmentRegistrationMutation } from '../api/numberMutations'
import {
  departmentRegistrationSchema,
  type DepartmentRegistrationFormValues,
} from '../model/schemas'

function todayIsoDate(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

interface DepartmentRegistrationDialogProps {
  documentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * Records the number and date a department wrote in its own register. Repeating the
 * same number for the same department appends a dated registration event.
 */
export function DepartmentRegistrationDialog({
  documentId,
  open,
  onOpenChange,
}: DepartmentRegistrationDialogProps) {
  const mutation = useRecordDepartmentRegistrationMutation(documentId)
  const departmentsQuery = useDepartmentOptions({ enabled: open })

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<DepartmentRegistrationFormValues>({
    resolver: zodResolver(departmentRegistrationSchema),
    defaultValues: {
      departmentId: '',
      registryNumberText: '',
      registryDate: todayIsoDate(),
      notes: '',
    },
  })

  const departmentOptions = useMemo(
    () =>
      (departmentsQuery.data ?? [])
        .filter((department) => department.isActive)
        .map((department) => ({
          value: department.departmentId,
          label: `${department.code} — ${department.nameAr}`,
        })),
    [departmentsQuery.data],
  )

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        departmentId: values.departmentId,
        registryNumberText: values.registryNumberText.trim(),
        registryDate: values.registryDate,
        notes: values.notes.trim() || null,
      })
      toast.success('تم تسجيل رقم الدائرة في سجلها.')
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="تسجيل رقم الدائرة"
      description="يُسجَّل الرقم الذي كتبته الدائرة في سجلها الخاص مع تاريخه، ولا يتغيّر رقم الديوان المركزي. تكرار التسجيل لنفس الدائرة يضيف حدثًا مؤرخًا."
      size="lg"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            إلغاء
          </Button>
          <Button
            type="submit"
            form="department-registration-form"
            loading={mutation.isPending}
          >
            تسجيل الرقم
          </Button>
        </>
      }
    >
      <form
        id="department-registration-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <FormField
          label="الدائرة"
          required
          error={errors.departmentId?.message}
          className="sm:col-span-2"
        >
          <Controller
            control={control}
            name="departmentId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={departmentOptions}
                disabled={mutation.isPending || departmentsQuery.isLoading}
                invalid={Boolean(errors.departmentId)}
                placeholder={
                  departmentsQuery.isLoading ? 'جارٍ التحميل…' : 'اختر الدائرة…'
                }
                aria-label="الدائرة"
              />
            )}
          />
          <p className="text-xs text-muted">
            يجب أن يكون للدائرة سجل ترقيم نشط؛ أضفه من صفحة «الترقيم» إن لم يكن موجودًا.
          </p>
        </FormField>

        <FormField
          label="رقم سجل الدائرة"
          required
          error={errors.registryNumberText?.message}
          htmlFor="department-registry-number"
        >
          <Input
            id="department-registry-number"
            dir="ltr"
            className="font-mono"
            placeholder="FIN-001/2026"
            disabled={mutation.isPending}
            invalid={Boolean(errors.registryNumberText)}
            {...register('registryNumberText')}
          />
        </FormField>

        <FormField
          label="تاريخ التسجيل"
          required
          error={errors.registryDate?.message}
          htmlFor="department-registry-date"
        >
          <Input
            id="department-registry-date"
            type="date"
            dir="ltr"
            disabled={mutation.isPending}
            invalid={Boolean(errors.registryDate)}
            {...register('registryDate')}
          />
        </FormField>

        <FormField label="ملاحظات" error={errors.notes?.message} className="sm:col-span-2">
          <Textarea
            rows={2}
            placeholder="مثال: سُلّم إلى قسم المتابعة."
            invalid={Boolean(errors.notes)}
            {...register('notes')}
          />
        </FormField>

        {mutation.error && (
          <p
            role="alert"
            className="sm:col-span-2 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
