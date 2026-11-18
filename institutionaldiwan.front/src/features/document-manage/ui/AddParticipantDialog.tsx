import { useEffect, useMemo, useState } from 'react'
import { Controller, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Building2, Landmark, Search, UserRound } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { useExternalOrganizations } from '@/entities/external-organization'
import { LOOKUP_SLUGS, useLookupEntries, usePreselectLookupDefault } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { FEATURES, type LabelKey } from '@/shared/config'
import { cn, t, useDebouncedValue, useDraftForm } from '@/shared/lib'
import {
  Button,
  Checkbox,
  Dialog,
  FormField,
  Input,
  Label,
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
  labelKey: LabelKey
  hintKey: LabelKey
  icon: typeof Building2
}> = [
  {
    key: 'external',
    labelKey: 'docdetails.target.external',
    hintKey: 'docdetails.target.external-hint',
    icon: Building2,
  },
  {
    key: 'department',
    labelKey: 'docdetails.target.department',
    hintKey: 'docdetails.target.department-hint',
    icon: Landmark,
  },
  {
    key: 'person',
    labelKey: 'docdetails.target.person',
    hintKey: 'docdetails.target.person-hint',
    icon: UserRound,
  },
]

const VISIBLE_TARGET_OPTIONS = TARGET_OPTIONS.filter(
  (option) =>
    (option.key !== 'external' || FEATURES.externalOrganizations) &&
    (option.key !== 'person' || FEATURES.personParticipants),
)

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
    getValues,
    handleSubmit,
    setValue,
    clearErrors,
    formState: { errors },
    draft,
  } = useDraftForm<AddParticipantFormValues>({ formId: `add-participant:${documentId}` }, {
    resolver: zodResolver(addParticipantSchema),
    defaultValues: {
      roleId: '',
      targetType: VISIBLE_TARGET_OPTIONS[0].key,
      externalOrganizationId: '',
      internalDepartmentId: '',
      personName: '',
      isPrimary: false,
      notes: '',
    },
  })

  usePreselectLookupDefault(rolesQuery.data, getValues('roleId'), (value) => setValue('roleId', value))

  const targetType = useWatch({ control, name: 'targetType' })

  // A form restored from before a type was suspended must not stay on a choice that is gone.
  useEffect(() => {
    if (!VISIBLE_TARGET_OPTIONS.some((option) => option.key === targetType)) {
      setValue('targetType', VISIBLE_TARGET_OPTIONS[0].key)
    }
  }, [targetType, setValue])

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
      toast.success(t('docdetails.participant.toast-added'))
      draft.discard()
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
      title={t('docdetails.action.add-participant')}
      description={
        FEATURES.externalOrganizations
          ? t('docdetails.participant.description-full')
          : VISIBLE_TARGET_OPTIONS.length > 1
            ? t('docdetails.participant.description-multi')
            : t('docdetails.participant.description-dept')
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
          <Button type="submit" form="document-participant-form" loading={mutation.isPending}>
            <Label k="docdetails.participant.submit" />
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
          labelKey="docdetails.participant.field-role"
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
                placeholder={t('docdetails.participant.role-placeholder')}
                aria-label={t('docdetails.participant.field-role')}
              />
            )}
          />
        </FormField>

        {VISIBLE_TARGET_OPTIONS.length > 1 && (
        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <span className="text-sm font-medium text-foreground">
            <Label k="docdetails.participant.field-target-type" /> <span className="text-danger">*</span>
          </span>
          <div
            role="radiogroup"
            aria-label={t('docdetails.participant.field-target-type')}
            className={cn('grid gap-2', VISIBLE_TARGET_OPTIONS.length === 3 ? 'sm:grid-cols-3' : 'sm:grid-cols-2')}
          >
            {VISIBLE_TARGET_OPTIONS.map((option) => {
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
                    <Label k={option.labelKey} />
                  </span>
                  <span className="text-xs text-muted"><Label k={option.hintKey} /></span>
                </button>
              )
            })}
          </div>
        </div>
        )}

        {targetType === 'external' && (
          <>
            <FormField
              labelKey="docdetails.participant.field-org-search"
              className="sm:col-span-2"
              htmlFor="participant-organization-search"
            >
              <Input
                id="participant-organization-search"
                placeholder={t('docdetails.participant.org-search-placeholder')}
                startIcon={<Search className="size-4" aria-hidden />}
                value={organizationSearch}
                onChange={(event) => setOrganizationSearch(event.target.value)}
                disabled={mutation.isPending}
              />
            </FormField>

            <FormField
              labelKey="docdetails.participant.field-org"
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
                      organizationsQuery.isLoading ? t('documents.create.loading') : t('docdetails.participant.org-placeholder')
                    }
                    aria-label={t('docdetails.participant.field-org')}
                  />
                )}
              />
            </FormField>

            {organizationsQuery.isSuccess && organizationOptions.length === 0 && (
              <p className="sm:col-span-2 text-xs text-muted">
                {t('docdetails.participant.org-empty')}
              </p>
            )}
          </>
        )}

        {targetType === 'department' && (
          <FormField
            labelKey="docdetails.participant.field-dept"
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
                    departmentsQuery.isLoading ? t('documents.create.loading') : t('docdetails.participant.dept-placeholder')
                  }
                  aria-label={t('docdetails.participant.field-dept')}
                />
              )}
            />
          </FormField>
        )}

        {targetType === 'person' && (
          <FormField
            labelKey="docdetails.participant.field-person"
            required
            error={errors.personName?.message}
            className="sm:col-span-2"
          >
            <Input
              invalid={Boolean(errors.personName)}
              placeholder={t('docdetails.participant.person-placeholder')}
              {...register('personName')}
            />
          </FormField>
        )}

        <FormField
          labelKey="docdetails.participant.field-notes"
          error={errors.notes?.message}
          className="sm:col-span-2"
        >
          <Textarea
            rows={2}
            placeholder={t('docdetails.participant.notes-placeholder')}
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
              label={t('docdetails.participant.primary-label')}
              description={t('docdetails.participant.primary-description')}
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
