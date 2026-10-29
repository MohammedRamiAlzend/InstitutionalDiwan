import { useMemo } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Building2, Contact, Hash } from 'lucide-react'
import { toast } from 'sonner'
import type { ExternalOrganizationResponse } from '@/entities/external-organization'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Select } from '@/shared/ui'
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
  } = useForm<ExternalOrganizationFormValues>({
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
        toast.success('تم تحديث بيانات الجهة الخارجية.')
      } else {
        await createMutation.mutateAsync({
          code: values.code,
          nameAr: values.nameAr,
          organizationTypeId,
          nameEn: values.nameEn || null,
          contactInfo: values.contactInfo || null,
        })
        toast.success('تمت إضافة الجهة الخارجية.')
      }
      onOpenChange(false)
    } catch {
      // The mutation error is rendered below in Arabic.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={isEdit ? 'تعديل الجهة الخارجية' : 'جهة خارجية جديدة'}
      description={
        isEdit
          ? 'حدّث اسم الجهة ونوعها وبيانات الاتصال. لا يمكن تغيير الرمز بعد الإنشاء.'
          : 'أضف جهة مراسلة خارجية (وزارة أو شركة أو فرد) لتظهر في اختيار جهات المشاركين.'
      }
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
            form="external-organization-form"
            loading={pending}
            disabled={typesQuery.isLoading}
          >
            {isEdit ? 'حفظ التعديلات' : 'إضافة الجهة'}
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
        <FormField label="رمز الجهة" required error={errors.code?.message}>
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

        <FormField label="نوع الجهة" required error={errors.organizationTypeId?.message}>
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
                  typesQuery.isLoading ? 'جارٍ تحميل الأنواع…' : 'اختر النوع…'
                }
                aria-label="نوع الجهة"
              />
            )}
          />
        </FormField>

        <FormField
          label="اسم الجهة بالعربية"
          required
          error={errors.nameAr?.message}
          className="sm:col-span-2"
        >
          <Input
            id="external-organization-name-ar"
            placeholder="مثال: وزارة الداخلية"
            startIcon={<Building2 className="size-4" aria-hidden />}
            invalid={Boolean(errors.nameAr)}
            {...register('nameAr')}
          />
        </FormField>

        <FormField
          label="اسم الجهة بالإنجليزية"
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
          label="بيانات الاتصال (بريد إلكتروني أو هاتف)"
          error={errors.contactInfo?.message}
          className="sm:col-span-2"
        >
          <Input
            id="external-organization-contact"
            dir="ltr"
            placeholder="name@example.com أو +963 9xx xxx xxx"
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
