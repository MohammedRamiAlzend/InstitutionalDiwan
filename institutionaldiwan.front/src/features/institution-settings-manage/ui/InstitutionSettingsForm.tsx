import { useEffect, useMemo, type ReactNode } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Archive,
  Building2,
  Clock3,
  Hash,
  Landmark,
  UserRound,
  type LucideIcon,
} from 'lucide-react'
import { toast } from 'sonner'
import { useDepartments } from '@/entities/department'
import { EmployeePicker } from '@/entities/employee'
import {
  useInstitutionSettings,
  type InstitutionSettingResponse,
} from '@/entities/institution-setting'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { DEPARTMENT_TYPE, Permissions } from '@/shared/config'
import { formatDateTime, t } from '@/shared/lib'
import {
  Button,
  Checkbox,
  FormField,
  GuideChip,
  GuideRegion,
  Input,
  Label,
  Select,
  Skeleton,
  Textarea,
} from '@/shared/ui'
import { useUpdateInstitutionSettingsMutation } from '../api/institutionSettingMutations'
import { InstitutionLogoField } from './InstitutionLogoField'
import {
  institutionSettingsSchema,
  type InstitutionSettingsFormValues,
} from '../model/schemas'

/** Radix Select forbids empty item values, so the optional employee uses a sentinel. */
const NONE = 'none'

function toFormValues(setting: InstitutionSettingResponse): InstitutionSettingsFormValues {
  return {
    institutionNameAr: setting.institutionNameAr,
    institutionNameEn: setting.institutionNameEn ?? '',
    defaultTimezone: setting.defaultTimezone,
    numberingModeId: String(setting.numberingModeId),
    numberingResetPolicyId: String(setting.numberingResetPolicyId),
    centralDiwanDepartmentId: setting.centralDiwanDepartmentId ?? '',
    responsibleEmployeeId: setting.responsibleEmployeeId ?? NONE,
    archiveDiwanDepartmentId: setting.archiveDiwanDepartmentId ?? NONE,
    otherDiwansMayCreateDocuments: setting.otherDiwansMayCreateDocuments,
    centralDiwanTransferReason: '',
  }
}

/** Editable singleton settings for `GET`/`PUT /api/institution-settings`. */
export function InstitutionSettingsForm() {
  const query = useInstitutionSettings()
  const setting = query.data

  if (query.isLoading) {
    return (
      <div className="flex flex-col gap-3">
        {Array.from({ length: 8 }, (_, index) => (
          <Skeleton key={index} className="h-11 w-full" />
        ))}
      </div>
    )
  }

  if (query.error) {
    return (
      <div
        role="alert"
        className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
      >
        <span>{toArabicErrorMessage(query.error)}</span>
        <Button type="button" variant="outline" size="sm" onClick={() => void query.refetch()}>
          <Label k="common.action.retry" />
        </Button>
      </div>
    )
  }

  if (!setting) return null

  return <InstitutionSettingsFormContent key={setting.settingId} setting={setting} />
}

function InstitutionSettingsFormContent({ setting }: { setting: InstitutionSettingResponse }) {
  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canPickEmployees = hasPermission(Permissions.EmployeeManage)

  const departmentsQuery = useDepartments()
  const modeQuery = useLookupEntries(LOOKUP_SLUGS.NumberingModes)
  const resetPolicyQuery = useLookupEntries(LOOKUP_SLUGS.NumberingResetPolicies)

  const updateMutation = useUpdateInstitutionSettingsMutation()
  const pending = updateMutation.isPending
  const mutationError = updateMutation.error

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<InstitutionSettingsFormValues>({
    resolver: zodResolver(institutionSettingsSchema),
    defaultValues: toFormValues(setting),
  })

  useEffect(() => {
    reset(toFormValues(setting))
  }, [setting, reset])

  const selectedCentralDiwanId = useWatch({ control, name: 'centralDiwanDepartmentId' })
  const centralDiwanChanged = selectedCentralDiwanId !== (setting.centralDiwanDepartmentId ?? '')

  const departmentOptions = useMemo(() => {
    const departments = [...(departmentsQuery.data ?? [])].sort((left, right) =>
      left.nameAr.localeCompare(right.nameAr, 'ar'),
    )

    return departments.map((department) => ({
      value: department.departmentId,
      label: `${department.nameAr} (${department.code})`,
      disabled: !(
        department.isActive &&
        department.departmentTypeId === DEPARTMENT_TYPE.CentralDiwan &&
        department.parentDepartmentId === null
      ),
    }))
  }, [departmentsQuery.data])

  // Any active department except the Central Diwan itself; the current one stays listed if stopped.
  const archiveDiwanOptions = useMemo(() => {
    const departments = [...(departmentsQuery.data ?? [])].sort((left, right) =>
      left.nameAr.localeCompare(right.nameAr, 'ar'),
    )

    return [
      { value: NONE, label: t('institution.archive.none') },
      ...departments
        .filter((department) => department.departmentId !== selectedCentralDiwanId)
        .map((department) => ({
          value: department.departmentId,
          label: department.isActive
            ? `${department.nameAr} (${department.code})`
            : `${department.nameAr} (${department.code}) (${t('institution.status.suspended')})`,
          disabled:
            !department.isActive && department.departmentId !== setting.archiveDiwanDepartmentId,
        })),
    ]
  }, [departmentsQuery.data, selectedCentralDiwanId, setting.archiveDiwanDepartmentId])

  const modeOptions = useMemo(
    () => (modeQuery.data ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    [modeQuery.data],
  )
  const resetPolicyOptions = useMemo(
    () =>
      (resetPolicyQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [resetPolicyQuery.data],
  )

  const onSubmit = handleSubmit(async (values) => {
    const changed =
      values.centralDiwanDepartmentId !== (setting.centralDiwanDepartmentId ?? '')

    if (changed && !values.centralDiwanTransferReason.trim()) {
      setError('centralDiwanTransferReason', {
        type: 'manual',
        message: t('institution.transfer.required'),
      })
      return
    }

    try {
      const updated = await updateMutation.mutateAsync({
        institutionNameAr: values.institutionNameAr,
        numberingModeId: Number(values.numberingModeId),
        numberingResetPolicyId: Number(values.numberingResetPolicyId),
        centralDiwanDepartmentId: values.centralDiwanDepartmentId,
        institutionNameEn: values.institutionNameEn || null,
        // The old free-text path is no longer edited here; the uploaded logo replaces it.
        logoPath: setting.logoPath,
        defaultTimezone: values.defaultTimezone || null,
        responsibleEmployeeId:
          values.responsibleEmployeeId === NONE ? null : values.responsibleEmployeeId,
        archiveDiwanDepartmentId:
          values.archiveDiwanDepartmentId === NONE ? null : values.archiveDiwanDepartmentId,
        otherDiwansMayCreateDocuments: values.otherDiwansMayCreateDocuments,
        centralDiwanTransferReason: changed
          ? values.centralDiwanTransferReason.trim()
          : null,
      })
      reset(toFormValues(updated))
      toast.success(t('institution.toast.saved'))
    } catch {
      // The mutation error is rendered below in Arabic.
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-6">
      <GuideRegion id="institution-settings.identity">
      <Section
        icon={Building2}
        title={t('institution.identity.title')}
        description={t('institution.identity.description')}
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            labelKey="institution.name-ar"
            htmlFor="institution-name-ar"
            required
            error={errors.institutionNameAr?.message}
            hint={t('institution.name-ar-hint')}
            className="sm:col-span-2"
          >
            <Input
              id="institution-name-ar"
              placeholder={t('institution.name-ar-placeholder')}
              invalid={Boolean(errors.institutionNameAr)}
              {...register('institutionNameAr')}
            />
          </FormField>

          <FormField
            labelKey="institution.name-en"
            htmlFor="institution-name-en"
            error={errors.institutionNameEn?.message}
          >
            <Input
              id="institution-name-en"
              dir="ltr"
              placeholder="Example: Central Diwan"
              invalid={Boolean(errors.institutionNameEn)}
              {...register('institutionNameEn')}
            />
          </FormField>

          <FormField labelKey="institution.logo" className="sm:col-span-2">
            <InstitutionLogoField />
          </FormField>

          <FormField
            labelKey="institution.timezone"
            htmlFor="institution-timezone"
            required
            error={errors.defaultTimezone?.message}
          >
            <Input
              id="institution-timezone"
              dir="ltr"
              placeholder="Asia/Damascus"
              startIcon={<Clock3 className="size-4" aria-hidden />}
              invalid={Boolean(errors.defaultTimezone)}
              {...register('defaultTimezone')}
            />
          </FormField>
        </div>
      </Section>
      </GuideRegion>

      <GuideRegion id="institution-settings.central">
      <Section
        icon={Landmark}
        title={t('institution.central.title')}
        description={t('institution.central.description')}
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            labelKey="institution.central.field"
            required
            error={errors.centralDiwanDepartmentId?.message}
            className="sm:col-span-2"
          >
            <Controller
              control={control}
              name="centralDiwanDepartmentId"
              render={({ field }) => (
                <Select
                  value={field.value || undefined}
                  onValueChange={field.onChange}
                  options={departmentOptions}
                  disabled={pending || departmentsQuery.isLoading}
                  invalid={Boolean(errors.centralDiwanDepartmentId)}
                  placeholder={
                    departmentsQuery.isLoading ? t('institution.central.loading') : t('institution.central.placeholder')
                  }
                  aria-label={t('institution.central.field')}
                />
              )}
            />
          </FormField>

          {centralDiwanChanged && (
            <FormField
              labelKey="institution.transfer.field"
              htmlFor="institution-transfer-reason"
              required
              error={errors.centralDiwanTransferReason?.message}
              className="sm:col-span-2"
            >
              <Textarea
                id="institution-transfer-reason"
                placeholder={t('institution.transfer.placeholder')}
                invalid={Boolean(errors.centralDiwanTransferReason)}
                {...register('centralDiwanTransferReason')}
              />
            </FormField>
          )}

          <div className="sm:col-span-2">
            <Controller
              control={control}
              name="otherDiwansMayCreateDocuments"
              render={({ field }) => (
                <Checkbox
                  checked={field.value}
                  onCheckedChange={(checked) => field.onChange(checked === true)}
                  disabled={pending}
                  label={t('institution.other-create.label')}
                  description={t('institution.other-create.description')}
                />
              )}
            />
          </div>

          {departmentsQuery.isError && (
            <p role="alert" className="sm:col-span-2 text-xs text-danger">
              {toArabicErrorMessage(departmentsQuery.error)}
            </p>
          )}
        </div>
      </Section>
      </GuideRegion>

      <GuideRegion id="institution-settings.archive">
      <Section
        icon={Archive}
        title={t('institution.archive.title')}
        description={t('institution.archive.description')}
      >
        <FormField labelKey="institution.archive.field" error={errors.archiveDiwanDepartmentId?.message}>
          <Controller
            control={control}
            name="archiveDiwanDepartmentId"
            render={({ field }) => (
              <Select
                value={field.value || NONE}
                onValueChange={field.onChange}
                options={archiveDiwanOptions}
                disabled={pending || departmentsQuery.isLoading}
                invalid={Boolean(errors.archiveDiwanDepartmentId)}
                aria-label={t('institution.archive.field')}
              />
            )}
          />
        </FormField>
        <p className="mt-2 text-xs text-muted">
          {t('institution.archive.note')}
        </p>
      </Section>
      </GuideRegion>

      <GuideRegion id="institution-settings.numbering">
      <Section
        icon={Hash}
        title={t('institution.numbering.title')}
        description={t('institution.numbering.description')}
      >
        {(modeQuery.isError || resetPolicyQuery.isError) && (
          <div role="alert" className="mb-3 flex items-center justify-between gap-2 text-sm text-danger">
            <span>{toArabicErrorMessage(modeQuery.error ?? resetPolicyQuery.error)}</span>
            <Button type="button" variant="outline" size="sm" onClick={() => {
              void modeQuery.refetch()
              void resetPolicyQuery.refetch()
            }}>
              <Label k="common.action.retry" />
            </Button>
          </div>
        )}
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField labelKey="institution.numbering.mode" required error={errors.numberingModeId?.message}>
            <Controller
              control={control}
              name="numberingModeId"
              render={({ field }) => (
                <Select
                  value={field.value || undefined}
                  onValueChange={field.onChange}
                  options={modeOptions}
                  disabled={pending || modeQuery.isLoading}
                  invalid={Boolean(errors.numberingModeId)}
                  placeholder={modeQuery.isLoading ? t('institution.numbering.loading') : t('institution.numbering.mode-placeholder')}
                  aria-label={t('institution.numbering.mode')}
                />
              )}
            />
          </FormField>

          <FormField
            labelKey="institution.numbering.reset"
            required
            error={errors.numberingResetPolicyId?.message}
          >
            <Controller
              control={control}
              name="numberingResetPolicyId"
              render={({ field }) => (
                <Select
                  value={field.value || undefined}
                  onValueChange={field.onChange}
                  options={resetPolicyOptions}
                  disabled={pending || resetPolicyQuery.isLoading}
                  invalid={Boolean(errors.numberingResetPolicyId)}
                  placeholder={resetPolicyQuery.isLoading ? t('institution.numbering.loading') : t('institution.numbering.reset-placeholder')}
                  aria-label={t('institution.numbering.reset-aria')}
                />
              )}
            />
          </FormField>
        </div>
      </Section>
      </GuideRegion>

      <GuideRegion id="institution-settings.responsible">
      <Section
        icon={UserRound}
        title={t('institution.responsible.title')}
        description={t('institution.responsible.description')}
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            labelKey="institution.responsible.field"
            error={errors.responsibleEmployeeId?.message}
            className="sm:col-span-2"
          >
            <Controller
              control={control}
              name="responsibleEmployeeId"
              render={({ field }) => (
                <EmployeePicker
                  value={field.value === NONE ? null : field.value}
                  onValueChange={(id) => field.onChange(id ?? NONE)}
                  disabled={pending || !canPickEmployees}
                  invalid={Boolean(errors.responsibleEmployeeId)}
                  label={t('institution.responsible.field')}
                  noneLabel={t('institution.responsible.none')}
                />
              )}
            />
            {!canPickEmployees && (
              <p role="status" className="mt-1.5 text-xs text-warning">
                {t('institution.responsible.denied')}
              </p>
            )}
          </FormField>
        </div>
      </Section>
      </GuideRegion>

      {mutationError && (
        <p
          role="alert"
          className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
        >
          {toArabicErrorMessage(mutationError)}
        </p>
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <span className="text-xs text-muted">
          {t('institution.updated-at')} <span dir="ltr">{formatDateTime(setting.updatedAt)}</span>
        </span>
        <Button type="submit" loading={pending}>
          <Label k="institution.save" />
        </Button>
        <GuideChip id="institution-settings.save" />
      </div>
    </form>
  )
}

function Section({
  icon: Icon,
  title,
  description,
  children,
}: {
  icon: LucideIcon
  title: string
  description: string
  children: ReactNode
}) {
  return (
    <section className="rounded-xl border border-border bg-surface p-4 sm:p-5">
      <div className="mb-4 flex items-start gap-3">
        <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg bg-surface-muted text-muted">
          <Icon className="size-4.5" aria-hidden />
        </span>
        <div className="flex flex-col gap-0.5">
          <h2 className="text-sm font-semibold text-foreground">{title}</h2>
          <p className="text-xs text-muted">{description}</p>
        </div>
      </div>
      {children}
    </section>
  )
}
