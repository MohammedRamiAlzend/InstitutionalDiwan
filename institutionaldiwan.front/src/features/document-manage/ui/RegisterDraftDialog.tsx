import { useMemo } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Hash } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import type { DocumentResponse } from '@/entities/document'
import { LOOKUP_SLUGS, useLookupEntries, usePreselectLookupDefault } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Select } from '@/shared/ui'
import { useRegisterDraftDocumentMutation } from '../api/documentMutations'
import {
  registerDraftSchema,
  type RegisterDraftFormValues,
} from '../model/schemas'

interface RegisterDraftDialogProps {
  document: DocumentResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

const NO_DESTINATION = '__none__'

const todayIso = () => new Date().toISOString().slice(0, 10)

/**
 * Registers a draft: the system issues the central number, the user types the number and date of
 * their department register line, and may send the document on in the same step.
 */
export function RegisterDraftDialog({
  document,
  open,
  onOpenChange,
}: RegisterDraftDialogProps) {
  const mutation = useRegisterDraftDocumentMutation(document.documentId)
  const departmentsQuery = useDepartmentOptions({ enabled: open })
  const movementTypesQuery = useLookupEntries(LOOKUP_SLUGS.MovementTypes)

  const {
    register,
    control,
    getValues,
    setValue,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterDraftFormValues>({
    resolver: zodResolver(registerDraftSchema),
    defaultValues: {
      registryNumberText: '',
      registryDate: todayIso(),
      destinationDepartmentId: '',
      movementTypeId: '',
      movementInstructions: '',
    },
  })

  usePreselectLookupDefault(movementTypesQuery.data, getValues('movementTypeId'), (value) =>
    setValue('movementTypeId', value),
  )

  const destinationDepartmentId = useWatch({ control, name: 'destinationDepartmentId' })

  const destinationOptions = useMemo(
    () => [
      { value: NO_DESTINATION, label: 'بدون إرسال الآن' },
      ...(departmentsQuery.data ?? [])
        .filter((department) => department.isActive)
        .map((department) => ({
          value: department.departmentId,
          label: `${department.nameAr} · ${department.code}`,
        })),
    ],
    [departmentsQuery.data],
  )
  const movementTypeOptions = useMemo(
    () =>
      (movementTypesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [movementTypesQuery.data],
  )

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        departmentRegistryNumberText: values.registryNumberText.trim(),
        departmentRegistryDate: values.registryDate,
        movement: values.destinationDepartmentId
          ? {
              destinationDepartmentId: values.destinationDepartmentId,
              movementTypeId: Number(values.movementTypeId),
              instructions: values.movementInstructions.trim() || null,
            }
          : null,
      })
      toast.success(
        values.destinationDepartmentId
          ? 'تم تسجيل الوثيقة وإرسالها إلى القسم المحدد.'
          : 'تم تسجيل الوثيقة وكتابة سطر السجل.',
      )
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="تسجيل المسودة"
      description="يصدر النظام رقم الوثيقة تلقائيًا. اكتب رقم السجل وتاريخه في دفتر قسمك، ويمكنك إرسال الوثيقة مباشرة."
      size="sm"
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
          <Button type="submit" form="document-register-form" loading={mutation.isPending}>
            تسجيل الوثيقة
          </Button>
        </>
      }
    >
      <form id="document-register-form" onSubmit={onSubmit} noValidate className="grid gap-4">
        <FormField
          label="رقم السجل (في دفتر قسمك)"
          required
          error={errors.registryNumberText?.message}
        >
          <Input
            dir="ltr"
            placeholder="1447/2026"
            startIcon={<Hash className="size-4" aria-hidden />}
            invalid={Boolean(errors.registryNumberText)}
            {...register('registryNumberText')}
          />
        </FormField>

        <FormField label="تاريخ السجل" required error={errors.registryDate?.message}>
          <Input
            type="date"
            dir="ltr"
            invalid={Boolean(errors.registryDate)}
            {...register('registryDate')}
          />
        </FormField>

        <FormField label="القسم المستلم (فارغ = تُفتح الحركة لاحقًا)">
          <Controller
            control={control}
            name="destinationDepartmentId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={(value) => field.onChange(value === NO_DESTINATION ? '' : value)}
                options={destinationOptions}
                disabled={mutation.isPending || departmentsQuery.isLoading}
                placeholder={departmentsQuery.isLoading ? 'جارٍ التحميل…' : 'بدون إرسال الآن'}
                aria-label="القسم المستلم للحركة"
              />
            )}
          />
        </FormField>

        {destinationDepartmentId && (
          <>
            <FormField label="نوع الحركة" required error={errors.movementTypeId?.message}>
              <Controller
                control={control}
                name="movementTypeId"
                render={({ field }) => (
                  <Select
                    value={field.value || undefined}
                    onValueChange={field.onChange}
                    options={movementTypeOptions}
                    disabled={mutation.isPending || movementTypesQuery.isLoading}
                    invalid={Boolean(errors.movementTypeId)}
                    placeholder="اختر النوع…"
                    aria-label="نوع الحركة"
                  />
                )}
              />
            </FormField>

            <FormField
              label="تعليمات (اختياري)"
              error={errors.movementInstructions?.message}
            >
              <Input
                invalid={Boolean(errors.movementInstructions)}
                {...register('movementInstructions')}
              />
            </FormField>
          </>
        )}

        {mutation.error && (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
