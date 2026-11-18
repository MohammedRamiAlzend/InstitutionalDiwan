import { useMemo } from 'react'
import { Controller, useWatch } from 'react-hook-form'
import { resolveLabel, t, useDraftForm, useLabelsStore } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { NUMBERING_RESET_POLICY } from '@/shared/config'
import {
  DEFAULT_NUMBER_PATTERN,
  renderNumberPattern,
  type NumberingRegistryResponse,
} from '@/entities/numbering-registry'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Label, Select } from '@/shared/ui'
import {
  useCreateNumberingRegistryMutation,
  useUpdateNumberingRegistryMutation,
} from '../api/registryMutations'
import { registryFormSchema, type RegistryFormValues } from '../model/schemas'

const NONE = '__none'

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
  const overrides = useLabelsStore((state) => state.overrides)

  const departmentsQuery = useDepartmentOptions({ enabled: open })
  const documentTypesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)
  const resetPoliciesQuery = useLookupEntries(LOOKUP_SLUGS.NumberingResetPolicies)

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<RegistryFormValues>({ formId: `registry-form:${registry?.registryId ?? 'new'}` }, {
    resolver: zodResolver(registryFormSchema),
    defaultValues: {
      code: registry?.code ?? '',
      nameAr: registry?.nameAr ?? '',
      formatPattern: registry?.formatPattern ?? '',
      resetPolicyId: String(registry?.resetPolicyId ?? NUMBERING_RESET_POLICY.Yearly),
      departmentId: registry?.departmentId ?? NONE,
      documentTypeId: registry?.documentTypeId ? String(registry.documentTypeId) : NONE,
    },
  })

  const departmentOptions = useMemo(
    () => [
      { value: NONE, label: resolveLabel('numbering.form.all-departments', overrides) },
      ...(departmentsQuery.data ?? [])
        // An inactive department stays visible only when the registry already targets it:
        // otherwise the select would render blank and hide the registry's real scope.
        .filter(
          (department) =>
            department.isActive || department.departmentId === registry?.departmentId,
        )
        .map((department) => ({
          value: department.departmentId,
          label: `${department.code} — ${department.nameAr}${department.isActive ? '' : resolveLabel('numbering.form.department-inactive-suffix', overrides)}`,
          disabled: !department.isActive,
        })),
    ],
    [departmentsQuery.data, registry?.departmentId, overrides],
  )

  const documentTypeOptions = useMemo(
    () => [
      { value: NONE, label: resolveLabel('numbering.form.all-types', overrides) },
      ...(documentTypesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [documentTypesQuery.data, overrides],
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

  const patternDraft = useWatch({ control, name: 'formatPattern' })
  const previewYear = new Date().getFullYear()
  const previewSequence =
    (registry?.counters.find((counter) => counter.numberingYear === previewYear)
      ?.lastValue ?? 0) + 1
  const previewNumber = renderNumberPattern(patternDraft, {
    numberingYear: previewYear,
    sequenceValue: previewSequence,
  })

  const onSubmit = handleSubmit(async (values) => {
    const scope = {
      nameAr: values.nameAr.trim(),
      formatPattern: values.formatPattern.trim() || null,
      resetPolicyId: Number(values.resetPolicyId),
      departmentId: values.departmentId === NONE ? null : values.departmentId,
      documentTypeId: values.documentTypeId === NONE ? null : Number(values.documentTypeId),
      // Direction scoping is suspended: the form no longer edits it, but an existing value is kept.
      directionId: registry?.directionId ?? null,
    }

    try {
      if (isEdit) {
        await updateMutation.mutateAsync(scope)
        toast.success(t('numbering.toast.updated'))
      } else {
        await createMutation.mutateAsync({
          code: values.code.trim().toUpperCase(),
          ...scope,
        })
        toast.success(t('numbering.toast.created'))
      }
      draft.discard()
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={isEdit ? t('numbering.form.edit-title') : t('numbering.form.create-title')}
      description={
        isEdit
          ? t('numbering.form.edit-description')
          : t('numbering.form.create-description')
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
            <Label k="common.action.cancel" />
          </Button>
          <Button
            type="submit"
            form="numbering-registry-form"
            loading={mutation.isPending}
          >
            <Label k={isEdit ? 'numbering.form.save-edit' : 'numbering.form.create'} />
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
        <FormField labelKey="numbering.form.code" required error={errors.code?.message} htmlFor="registry-code">
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
            <p className="text-xs text-muted"><Label k="numbering.form.code-locked" /></p>
          )}
        </FormField>

        <FormField labelKey="numbering.form.name" required error={errors.nameAr?.message} htmlFor="registry-name">
          <Input
            id="registry-name"
            placeholder={t('numbering.form.name-placeholder')}
            disabled={mutation.isPending}
            invalid={Boolean(errors.nameAr)}
            {...register('nameAr')}
          />
        </FormField>

        <FormField
          labelKey="numbering.form.department"
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
                placeholder={t('numbering.form.all-departments')}
                aria-label={t('numbering.form.department')}
              />
            )}
          />
        </FormField>

        <FormField labelKey="numbering.form.document-type">
          <Controller
            control={control}
            name="documentTypeId"
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={field.onChange}
                options={documentTypeOptions}
                disabled={mutation.isPending || documentTypesQuery.isLoading}
                aria-label={t('numbering.form.document-type')}
              />
            )}
          />
        </FormField>


        <FormField
          labelKey="numbering.form.reset-policy"
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
                aria-label={t('numbering.form.reset-policy')}
              />
            )}
          />
        </FormField>

        <FormField
          labelKey="numbering.form.pattern"
          error={errors.formatPattern?.message}
          htmlFor="registry-pattern"
        >
          <Input
            id="registry-pattern"
            dir="ltr"
            className="font-mono"
            placeholder={DEFAULT_NUMBER_PATTERN}
            disabled={templateLocked || mutation.isPending}
            invalid={Boolean(errors.formatPattern)}
            {...register('formatPattern')}
          />
          <p className="text-xs text-muted">
            {templateLocked ? (
              <Label k="numbering.form.pattern-locked-hint" />
            ) : (
              t('numbering.form.pattern-hint', { pattern: DEFAULT_NUMBER_PATTERN })
            )}
          </p>
          <p className="flex flex-wrap items-center gap-1.5 text-xs text-muted">
            <Label k="numbering.form.preview-label" />
            <code
              dir="ltr"
              className="rounded bg-surface-muted px-1.5 py-0.5 font-mono text-foreground"
            >
              {previewNumber}
            </code>
            <span>
              (<Label k="numbering.form.preview-year" /> <span dir="ltr">{previewYear}</span>،{' '}
              <Label k="numbering.form.preview-sequence" />{' '}
              <span dir="ltr">{previewSequence}</span>)
            </span>
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