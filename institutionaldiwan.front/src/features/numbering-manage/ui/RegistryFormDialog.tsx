import { useMemo } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { NUMBERING_RESET_POLICY } from '@/shared/config'
import type { NumberingRegistryResponse } from '@/entities/numbering-registry'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Select } from '@/shared/ui'
import {
  useCreateNumberingRegistryMutation,
  useUpdateNumberingRegistryMutation,
} from '../api/registryMutations'
import { registryFormSchema, type RegistryFormValues } from '../model/schemas'

const NONE = '__none'
const DEFAULT_PATTERN = '{YEAR}/{SEQ:D6}'

interface RegistryFormDialogProps {
  /** `null` creates a registry; otherwise the registry is edited. */
  registry: NumberingRegistryResponse | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Create/edit a numbering registry: scope, reset policy, and number template. */
export function RegistryFormDialog({
  registry,
  open,
  onOpenChange,
}: RegistryFormDialogProps) {
  const isEdit = registry !== null
  const createMutation = useCreateNumberingRegistryMutation()
  const updateMutation = useUpdateNumberingRegistryMutation(registry?.registryId ?? '')
  const mutation = isEdit ? updateMutation : createMutation

  const departmentsQuery = useDepartmentOptions({ enabled: open })
  const documentTypesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)
  const directionsQuery = useLookupEntries(LOOKUP_SLUGS.DocumentDirections)
  const resetPoliciesQuery = useLookupEntries(LOOKUP_SLUGS.NumberingResetPolicies)

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<RegistryFormValues>({
    resolver: zodResolver(registryFormSchema),
    defaultValues: {
      code: registry?.code ?? '',
      nameAr: registry?.nameAr ?? '',
      formatPattern: registry?.formatPattern ?? '',
      resetPolicyId: String(registry?.resetPolicyId ?? NUMBERING_RESET_POLICY.Yearly),
      departmentId: registry?.departmentId ?? NONE,
      documentTypeId: registry?.documentTypeId ? String(registry.documentTypeId) : NONE,
      directionId: registry?.directionId ? String(registry.directionId) : NONE,
    },
  })

  const departmentOptions = useMemo(
    () => [
      { value: NONE, label: 'كل الدوائر (سجل شامل)' },
      ...(departmentsQuery.data ?? [])
        .filter((department) => department.isActive)
        .map((department) => ({
          value: department.departmentId,
          label: `${department.code} — ${department.nameAr}`,
        })),
    ],
    [departmentsQuery.data],
  )

  const documentTypeOptions = useMemo(
    () => [
      { value: NONE, label: 'كل الأنواع' },
      ...(documentTypesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [documentTypesQuery.data],
  )

  const directionOptions = useMemo(
    () => [
      { value: NONE, label: 'كل الاتجاهات' },
      ...(directionsQuery.data ?? [])
        .filter((entry) => entry.id !== 4)
        .map((entry) => ({
          value: String(entry.id),
          label: entry.descAr,
        })),
    ],
    [directionsQuery.data],
  )

  const resetPolicyOptions = useMemo(
    () =>
      (resetPoliciesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [resetPoliciesQuery.data],
  )

  const templateLocked = isEdit && registry.counters.length > 0

  const onSubmit = handleSubmit(async (values) => {
    const scope = {
      nameAr: values.nameAr.trim(),
      formatPattern: values.formatPattern.trim() || null,
      resetPolicyId: Number(values.resetPolicyId),
      departmentId: values.departmentId === NONE ? null : values.departmentId,
      documentTypeId: values.documentTypeId === NONE ? null : Number(values.documentTypeId),
      directionId: values.directionId === NONE ? null : Number(values.directionId),
    }

    try {
      if (isEdit) {
        await updateMutation.mutateAsync(scope)
        toast.success('تم تحديث سجل الترقيم.')
      } else {
        await createMutation.mutateAsync({
          code: values.code.trim().toUpperCase(),
          ...scope,
        })
        toast.success('تم إنشاء سجل الترقيم.')
      }
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={isEdit ? 'تعديل سجل الترقيم' : 'سجل ترقيم جديد'}
      description={
        isEdit
          ? 'يمكنك تغيير النطاق وسياسة التصفير. يُقفل القالب بعد إصدار أول رقم من السجل.'
          : 'يحدد السجل نطاق الأرقام (دائرة/نوع/اتجاه) وقالب صياغتها. الرمز لا يتغير بعد الإنشاء.'
      }
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
            form="numbering-registry-form"
            loading={mutation.isPending}
          >
            {isEdit ? 'حفظ التعديلات' : 'إنشاء السجل'}
          </Button>
        </>
      }
    >
      <form
        id="numbering-registry-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <FormField label="الرمز" required error={errors.code?.message} htmlFor="registry-code">
          <Input
            id="registry-code"
            dir="ltr"
            className="font-mono uppercase"
            placeholder="FIN-2026"
            disabled={isEdit || mutation.isPending}
            invalid={Boolean(errors.code)}
            {...register('code')}
          />
          {isEdit && (
            <p className="text-xs text-muted">رمز السجل ثابت ولا يمكن تعديله.</p>
          )}
        </FormField>

        <FormField label="الاسم" required error={errors.nameAr?.message} htmlFor="registry-name">
          <Input
            id="registry-name"
            placeholder="سجل الإدارة المالية"
            disabled={mutation.isPending}
            invalid={Boolean(errors.nameAr)}
            {...register('nameAr')}
          />
        </FormField>

        <FormField
          label="الدائرة"
          error={errors.departmentId?.message}
          className="sm:col-span-2"
        >
          <Controller
            control={control}
            name="departmentId"
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={field.onChange}
                options={departmentOptions}
                disabled={mutation.isPending || departmentsQuery.isLoading}
                placeholder="كل الدوائر (سجل شامل)"
                aria-label="الدائرة"
              />
            )}
          />
        </FormField>

        <FormField label="نوع الوثيقة">
          <Controller
            control={control}
            name="documentTypeId"
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={field.onChange}
                options={documentTypeOptions}
                disabled={mutation.isPending || documentTypesQuery.isLoading}
                aria-label="نوع الوثيقة"
              />
            )}
          />
        </FormField>

        <FormField label="الاتجاه">
          <Controller
            control={control}
            name="directionId"
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={field.onChange}
                options={directionOptions}
                disabled={mutation.isPending || directionsQuery.isLoading}
                aria-label="الاتجاه"
              />
            )}
          />
        </FormField>

        <FormField
          label="سياسة التصفير"
          required
          error={errors.resetPolicyId?.message}
        >
          <Controller
            control={control}
            name="resetPolicyId"
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={field.onChange}
                options={resetPolicyOptions}
                disabled={mutation.isPending || resetPoliciesQuery.isLoading}
                invalid={Boolean(errors.resetPolicyId)}
                aria-label="سياسة التصفير"
              />
            )}
          />
        </FormField>

        <FormField
          label="قالب الرقم"
          error={errors.formatPattern?.message}
          htmlFor="registry-pattern"
        >
          <Input
            id="registry-pattern"
            dir="ltr"
            className="font-mono"
            placeholder={DEFAULT_PATTERN}
            disabled={templateLocked || mutation.isPending}
            invalid={Boolean(errors.formatPattern)}
            {...register('formatPattern')}
          />
          <p className="text-xs text-muted">
            {templateLocked
              ? 'لا يمكن تعديل القالب بعد إصدار أرقام من السجل.'
              : `اتركه فارغًا لاستخدام ${DEFAULT_PATTERN}، ويجب أن يتضمن {SEQ}.`}
          </p>
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
