import { useMemo } from 'react'
import { Controller } from 'react-hook-form'
import { t, useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { Building2, Contact, Hash } from 'lucide-react'
import { toast } from 'sonner'
import type { ExternalOrganizationResponse } from '@/entities/external-organization'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Label, Select } from '@/shared/ui'
import {
  externalOrganizationFormSchema,
  type ExternalOrganizationFormValues,
} from '../model/schemas'
import {
  useCreateExternalOrganizationMutation,
  useUpdateExternalOrganizationMutation,
} from '../api/externalOrganizationMutations'

interface ExternalOrganizationFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** `null` creates a new record; an organization edits the existing one. */
  organization: ExternalOrganizationResponse | null
}

/** Create/edit form for `POST` and `PUT /api/external-organizations`. */
export function ExternalOrganizationFormDialog({
  open,
  onOpenChange,
  organization,
}: ExternalOrganizationFormDialogProps) {
  const isEdit = organization !== null

  const typesQuery = useLookupEntries(LOOKUP_SLUGS.OrganizationTypes, { enabled: open })

  const createMutation = useCreateExternalOrganizationMutation()
  const updateMutation = useUpdateExternalOrganizationMutation()
  const pending = createMutation.isPending || updateMutation.isPending
  const mutationError = createMutation.error ?? updateMutation.error

  const typeOptions = useMemo(
    () =>
      (typesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    [typesQuery.data],
  )

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<ExternalOrganizationFormValues>({ formId: `organization-form:${organization?.externalOrganizationId ?? 'new'}` }, {
    resolver: zodResolver(externalOrganizationFormSchema),
    defaultValues: {
      code: organization?.code ?? '',
      nameAr: organization?.nameAr ?? '',
      organizationTypeId: organization ? String(organization.organizationTypeId) : '',
      nameEn: organization?.nameEn ?? '',
      contactInfo: organization?.contactInfo ?? '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    const organizationTypeId = Number(values.organizationTypeId)

    try {
      if (organization) {
        await updateMutation.mutateAsync({
          externalOrganizationId: organization.externalOrganizationId,
          request: {
            nameAr: values.nameAr,
            organizationTypeId,
            nameEn: values.nameEn || null,
            contactInfo: values.contactInfo || null,
          },
        })
        toast.success(t('external-org.form.toast-updated'))
      } else {
        await createMutation.mutateAsync({
          code: values.code,
          nameAr: values.nameAr,
          organizationTypeId,
          nameEn: values.nameEn || null,
          contactInfo: values.contactInfo || null,
        })
        toast.success(t('external-org.form.toast-created'))
      }
      draft.discard()
      onOpenChange(false)
    } catch {
      // The mutation error is rendered below in Arabic.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={isEdit ? t('external-org.form.edit-title') : t('external-org.create')}
      description={
        isEdit
          ? t('external-org.form.edit-description')
          : t('external-org.form.create-description')
      }
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
            form="external-organization-form"
            loading={pending}
            disabled={typesQuery.isLoading}
          >
            {isEdit ? <Label k="external-org.form.save" /> : <Label k="external-org.form.submit" />}
          </Button>
        </>
      }
    >
      <form
        id="external-organization-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <FormField labelKey="external-org.form.code" required error={errors.code?.message}>
          <Input
            id="external-organization-code"
            dir="ltr"
            placeholder="MOI-01"
            startIcon={<Hash className="size-4" aria-hidden />}
            invalid={Boolean(errors.code)}
            disabled={isEdit || pending}
            {...register('code')}
          />
        </FormField>

        <FormField labelKey="external-org.form.type" required error={errors.organizationTypeId?.message}>
          <Controller
            control={control}
            name="organizationTypeId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={typeOptions}
                disabled={pending || typesQuery.isLoading}
                invalid={Boolean(errors.organizationTypeId)}
                placeholder={
                  typesQuery.isLoading ? t('external-org.form.type-loading') : t('external-org.form.type-placeholder')
                }
                aria-label={t('external-org.form.type')}
              />
            )}
          />
        </FormField>

        <FormField
          labelKey="external-org.form.name-ar"
          required
          error={errors.nameAr?.message}
          className="sm:col-span-2"
        >
          <Input
            id="external-organization-name-ar"
            placeholder={t('external-org.form.name-ar-placeholder')}
            startIcon={<Building2 className="size-4" aria-hidden />}
            invalid={Boolean(errors.nameAr)}
            {...register('nameAr')}
          />
        </FormField>

        <FormField
          labelKey="external-org.form.name-en"
          error={errors.nameEn?.message}
          className="sm:col-span-2"
        >
          <Input
            id="external-organization-name-en"
            dir="ltr"
            placeholder="Example: Ministry of Interior"
            startIcon={<Building2 className="size-4" aria-hidden />}
            invalid={Boolean(errors.nameEn)}
            {...register('nameEn')}
          />
        </FormField>

        <FormField
          labelKey="external-org.form.contact"
          error={errors.contactInfo?.message}
          className="sm:col-span-2"
        >
          <Input
            id="external-organization-contact"
            dir="ltr"
            placeholder={t('external-org.form.contact-placeholder')}
            startIcon={<Contact className="size-4" aria-hidden />}
            invalid={Boolean(errors.contactInfo)}
            {...register('contactInfo')}
          />
        </FormField>

        {typesQuery.isError && (
          <p
            role="alert"
            className="sm:col-span-2 rounded-lg bg-danger/10 px-3 py-2 text-xs text-danger"
          >
            {toArabicErrorMessage(typesQuery.error)}
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
