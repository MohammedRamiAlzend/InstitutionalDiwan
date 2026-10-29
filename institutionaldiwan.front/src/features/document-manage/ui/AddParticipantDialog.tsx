import { useMemo, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Building2, Landmark, Search, UserRound } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { useExternalOrganizations } from '@/entities/external-organization'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { cn, useDebouncedValue } from '@/shared/lib'
import {
  Button,
  Checkbox,
  Dialog,
  FormField,
  Input,
  Select,
  Textarea,
} from '@/shared/ui'
import { useAddDocumentParticipantMutation } from '../api/documentMutations'
import {
  addParticipantSchema,
  type AddParticipantFormValues,
  type ParticipantTargetKey,
} from '../model/schemas'

interface AddParticipantDialogProps {
  documentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

const TARGET_OPTIONS: ReadonlyArray<{
  key: ParticipantTargetKey
  label: string
  hint: string
  icon: typeof Building2
}> = [
  {
    key: 'external',
    label: 'جهة خارجية',
    hint: 'وزارة أو شركة أو منظمة مسجّلة مسبقًا.',
    icon: Building2,
  },
  {
    key: 'department',
    label: 'قسم داخلي',
    hint: 'دائرة أو قسم داخل المؤسسة.',
    icon: Landmark,
  },
  {
    key: 'person',
    label: 'شخص',
    hint: 'اسم شخص غير مرتبط بقسم أو جهة.',
    icon: UserRound,
  },
]

/** Adds one participant; the server allows only one external party per document. */
export function AddParticipantDialog({
  documentId,
  open,
  onOpenChange,
}: AddParticipantDialogProps) {
  const mutation = useAddDocumentParticipantMutation(documentId)
  const rolesQuery = useLookupEntries(LOOKUP_SLUGS.ParticipantRoles)

  const [organizationSearch, setOrganizationSearch] = useState('')
  const debouncedOrganizationSearch = useDebouncedValue(organizationSearch)

  const {
    register,
    control,
    handleSubmit,
    setValue,
    clearErrors,
    formState: { errors },
  } = useForm<AddParticipantFormValues>({
    resolver: zodResolver(addParticipantSchema),
    defaultValues: {
      roleId: '',
      targetType: 'external',
      externalOrganizationId: '',
      internalDepartmentId: '',
      personName: '',
      isPrimary: false,
      notes: '',
    },
  })

  const targetType = useWatch({ control, name: 'targetType' })

  const organizationsQuery = useExternalOrganizations(
    {
      page: 1,
      pageSize: 20,
      search: debouncedOrganizationSearch.trim() || undefined,
    },
    { enabled: open && targetType === 'external' },
  )

  const departmentsQuery = useDepartmentOptions({
    enabled: open && targetType === 'department',
  })

  const organizationOptions = useMemo(
    () =>
      (organizationsQuery.data?.items ?? []).map((organization) => ({
        value: organization.externalOrganizationId,
        label: `${organization.nameAr} · ${organization.code}`,
      })),
    [organizationsQuery.data],
  )

  const departmentOptions = useMemo(
    () =>
      (departmentsQuery.data ?? [])
        .filter((department) => department.isActive)
        .map((department) => ({
          value: department.departmentId,
          label: `${department.nameAr} · ${department.code}`,
        })),
    [departmentsQuery.data],
  )

  const roleOptions = useMemo(
    () =>
      (rolesQuery.data ?? []).map((role) => ({
        value: String(role.id),
        label: role.descAr,
      })),
    [rolesQuery.data],
  )

  const onSubmit = handleSubmit(async (values) => {
    const target =
      values.targetType === 'external'
        ? { externalOrganizationId: values.externalOrganizationId }
        : values.targetType === 'department'
          ? { internalDepartmentId: values.internalDepartmentId }
          : { personName: values.personName.trim() }

    try {
      await mutation.mutateAsync({
        roleId: Number(values.roleId),
        isPrimary: values.isPrimary,
        notes: values.notes.trim() || null,
        ...target,
      })
      toast.success('تمت إضافة المشارك إلى الوثيقة.')
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  const targetCardClass = (selected: boolean) =>
    cn(
      'flex flex-col gap-1 rounded-lg border p-3 text-start transition-colors',
      selected
        ? 'border-accent bg-accent/10'
        : 'border-border bg-surface hover:bg-surface-muted/60',
    )

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="إضافة مشارك"
      description="اختر دور المشارك ونوعه. تُحدَّد الجهة المشاركة بمُعرّف مسجَّل؛ تسمح الوثيقة بجهة خارجية واحدة فقط."
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
          <Button type="submit" form="document-participant-form" loading={mutation.isPending}>
            إضافة المشارك
          </Button>
        </>
      }
    >
      <form
        id="document-participant-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <FormField
          label="دور المشارك"
          required
          error={errors.roleId?.message}
          className="sm:col-span-2"
        >
          <Controller
            control={control}
            name="roleId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={roleOptions}
                disabled={mutation.isPending || rolesQuery.isLoading}
                invalid={Boolean(errors.roleId)}
                placeholder="اختر الدور…"
                aria-label="دور المشارك"
              />
            )}
          />
        </FormField>

        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <span className="text-sm font-medium text-foreground">
            نوع المشارك <span className="text-danger">*</span>
          </span>
          <div
            role="radiogroup"
            aria-label="نوع المشارك"
            className="grid gap-2 sm:grid-cols-3"
          >
            {TARGET_OPTIONS.map((option) => {
              const Icon = option.icon
              return (
                <button
                  key={option.key}
                  type="button"
                  role="radio"
                  aria-checked={targetType === option.key}
                  onClick={() => {
                    setValue('targetType', option.key, { shouldValidate: false })
                    clearErrors([
                      'externalOrganizationId',
                      'internalDepartmentId',
                      'personName',
                    ])
                  }}
                  disabled={mutation.isPending}
                  className={targetCardClass(targetType === option.key)}
                >
                  <span className="flex items-center gap-1.5 text-sm font-medium text-foreground">
                    <Icon className="size-4" aria-hidden />
                    {option.label}
                  </span>
                  <span className="text-xs text-muted">{option.hint}</span>
                </button>
              )
            })}
          </div>
        </div>

        {targetType === 'external' && (
          <>
            <FormField
              label="البحث في الجهات الخارجية"
              className="sm:col-span-2"
              htmlFor="participant-organization-search"
            >
              <Input
                id="participant-organization-search"
                placeholder="ابحث بالاسم أو الرمز…"
                startIcon={<Search className="size-4" aria-hidden />}
                value={organizationSearch}
                onChange={(event) => setOrganizationSearch(event.target.value)}
                disabled={mutation.isPending}
              />
            </FormField>

            <FormField
              label="الجهة الخارجية"
              required
              error={errors.externalOrganizationId?.message}
              className="sm:col-span-2"
            >
              <Controller
                control={control}
                name="externalOrganizationId"
                render={({ field }) => (
                  <Select
                    value={field.value || undefined}
                    onValueChange={field.onChange}
                    options={organizationOptions}
                    disabled={mutation.isPending || organizationsQuery.isLoading}
                    invalid={Boolean(errors.externalOrganizationId)}
                    placeholder={
                      organizationsQuery.isLoading ? 'جارٍ التحميل…' : 'اختر الجهة…'
                    }
                    aria-label="الجهة الخارجية"
                  />
                )}
              />
            </FormField>

            {organizationsQuery.isSuccess && organizationOptions.length === 0 && (
              <p className="sm:col-span-2 text-xs text-muted">
                لا توجد جهات خارجية مطابقة. تُدار الجهات الخارجية من شاشة مستقلة، ويمكن
                إضافة مشاركين من الأقسام أو الأشخاص في الوقت الحالي.
              </p>
            )}
          </>
        )}

        {targetType === 'department' && (
          <FormField
            label="القسم المشارك"
            required
            error={errors.internalDepartmentId?.message}
            className="sm:col-span-2"
          >
            <Controller
              control={control}
              name="internalDepartmentId"
              render={({ field }) => (
                <Select
                  value={field.value || undefined}
                  onValueChange={field.onChange}
                  options={departmentOptions}
                  disabled={mutation.isPending || departmentsQuery.isLoading}
                  invalid={Boolean(errors.internalDepartmentId)}
                  placeholder={
                    departmentsQuery.isLoading ? 'جارٍ التحميل…' : 'اختر القسم…'
                  }
                  aria-label="القسم المشارك"
                />
              )}
            />
          </FormField>
        )}

        {targetType === 'person' && (
          <FormField
            label="اسم الشخص"
            required
            error={errors.personName?.message}
            className="sm:col-span-2"
          >
            <Input
              invalid={Boolean(errors.personName)}
              placeholder="مثال: أحمد محمد علي"
              {...register('personName')}
            />
          </FormField>
        )}

        <FormField
          label="ملاحظات"
          error={errors.notes?.message}
          className="sm:col-span-2"
        >
          <Textarea
            rows={2}
            placeholder="ملاحظة اختيارية عن المشارك…"
            invalid={Boolean(errors.notes)}
            {...register('notes')}
          />
        </FormField>

        <Controller
          control={control}
          name="isPrimary"
          render={({ field }) => (
            <Checkbox
              checked={field.value}
              onCheckedChange={field.onChange}
              label="مشارك أساسي"
              description="يُبرز هذا المشارك كطرف رئيسي في الوثيقة."
              disabled={mutation.isPending}
              className="sm:col-span-2"
            />
          )}
        />

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
