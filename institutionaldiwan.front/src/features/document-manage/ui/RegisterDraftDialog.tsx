import { useMemo } from 'react'
import { Controller, useWatch } from 'react-hook-form'
import { resolveLabel, t, useDraftForm, useLabelsStore } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { Hash } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import type { DocumentResponse } from '@/entities/document'
import { LOOKUP_SLUGS, useLookupEntries, usePreselectLookupDefault } from '@/entities/lookup'
import { DepartmentRegistrySelect, useDepartmentRegistryOptions } from '@/entities/numbering-registry'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { Button, Dialog, FormField, Input, Label, Select } from '@/shared/ui'
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
  const overrides = useLabelsStore((state) => state.overrides)

  const {
    register,
    control,
    getValues,
    setValue,
    setError,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<RegisterDraftFormValues>({ formId: `register-draft:${document.documentId}` }, {
    resolver: zodResolver(registerDraftSchema),
    defaultValues: {
      registryId: '',
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

  // Without NUMBERING_VIEW the registers cannot be listed, so the server picks the best match.
  const canViewNumbering = useSessionStore((state) =>
    state.hasPermission(Permissions.NumberingView),
  )
  const registries = useDepartmentRegistryOptions(document.owningDepartmentId, {
    enabled: open && canViewNumbering,
  })

  const destinationOptions = useMemo(
    () => [
      { value: NO_DESTINATION, label: resolveLabel('documents.create.no-destination', overrides) },
      ...(departmentsQuery.data ?? [])
        .filter((department) => department.isActive)
        .map((department) => ({
          value: department.departmentId,
          label: `${department.nameAr} · ${department.code}`,
        })),
    ],
    [departmentsQuery.data, overrides],
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
    if (registries.options.length > 1 && !values.registryId) {
      setError('registryId', { message: t('documents.create.error.registry-required') })
      return
    }

    try {
      await mutation.mutateAsync({
        departmentRegistryNumberText: values.registryNumberText.trim(),
        departmentRegistryDate: values.registryDate,
        departmentRegistryId: values.registryId || null,
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
          ? t('documents.create.toast.registered-sent')
          : t('documents.create.toast.registered'),
      )
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
      title={t('docdetails.register-title')}
      description={t('docdetails.register-description')}
      size="sm"
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
          <Button type="submit" form="document-register-form" loading={mutation.isPending}>
            <Label k="documents.create.submit-register" />
          </Button>
        </>
      }
    >
      <form id="document-register-form" onSubmit={onSubmit} noValidate className="grid gap-4">
        {canViewNumbering && (
          <FormField
            labelKey="documents.create.field-registry"
            required={registries.options.length > 1}
            error={errors.registryId?.message}
          >
            <Controller
              control={control}
              name="registryId"
              render={({ field }) => (
                <DepartmentRegistrySelect
                  registries={registries}
                  value={field.value}
                  onChange={field.onChange}
                  disabled={mutation.isPending}
                  invalid={Boolean(errors.registryId)}
                />
              )}
            />
          </FormField>
        )}

        <FormField
          labelKey="docdetails.register-field-number"
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

        <FormField labelKey="documents.create.field-registry-date" required error={errors.registryDate?.message}>
          <Input
            type="date"
            dir="ltr"
            invalid={Boolean(errors.registryDate)}
            {...register('registryDate')}
          />
        </FormField>

        <FormField labelKey="docdetails.register-field-destination">
          <Controller
            control={control}
            name="destinationDepartmentId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={(value) => field.onChange(value === NO_DESTINATION ? '' : value)}
                options={destinationOptions}
                disabled={mutation.isPending || departmentsQuery.isLoading}
                placeholder={departmentsQuery.isLoading ? t('documents.create.loading') : t('documents.create.no-destination')}
                aria-label={t('documents.create.destination-aria')}
              />
            )}
          />
        </FormField>

        {destinationDepartmentId && (
          <>
            <FormField labelKey="documents.create.field-movement-type" required error={errors.movementTypeId?.message}>
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
                    placeholder={t('documents.create.type-placeholder')}
                    aria-label={t('documents.create.field-movement-type')}
                  />
                )}
              />
            </FormField>

            <FormField
              labelKey="docdetails.register-field-instructions"
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
