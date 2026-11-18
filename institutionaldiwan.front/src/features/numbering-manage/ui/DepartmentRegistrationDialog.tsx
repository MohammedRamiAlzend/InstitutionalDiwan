import { useEffect, useRef } from 'react'
import { Controller } from 'react-hook-form'
import { t, useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { findDepartmentEntry, useDocumentRegistryNumbers } from '@/entities/document'
import { DepartmentRegistrySelect, useDepartmentRegistryOptions } from '@/entities/numbering-registry'
import { useMyPrimaryDepartmentId } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Label, Textarea } from '@/shared/ui'
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
 * Records the number and date the caller's own department wrote in one of its registers. The
 * department is fixed (a department can only number in its own register); when it has several
 * active registers the clerk picks the one the number was written in. When the department already
 * has a number for the document in the date's year, the number is locked and only a new date is added.
 */
export function DepartmentRegistrationDialog({
  documentId,
  open,
  onOpenChange,
}: DepartmentRegistrationDialogProps) {
  const mutation = useRecordDepartmentRegistrationMutation(documentId)
  const departmentsQuery = useDepartmentOptions({ enabled: open })
  const { departmentId: ownDepartmentId, isLoading: profileLoading } =
    useMyPrimaryDepartmentId()

  const {
    register,
    control,
    handleSubmit,
    setValue,
    setError,
    watch,
    formState: { errors },
    draft,
  } = useDraftForm<DepartmentRegistrationFormValues>({ formId: `department-registration:${documentId}` }, {
    resolver: zodResolver(departmentRegistrationSchema),
    defaultValues: {
      departmentId: '',
      registryId: '',
      registryNumberText: '',
      registryDate: todayIsoDate(),
      notes: '',
    },
  })

  useEffect(() => {
    if (ownDepartmentId) setValue('departmentId', ownDepartmentId)
  }, [ownDepartmentId, setValue])

  const registries = useDepartmentRegistryOptions(ownDepartmentId, { enabled: open })
  const registryOptions = registries.options
  const entriesQuery = useDocumentRegistryNumbers(documentId, { enabled: open })
  const existingLine = findDepartmentEntry(
    entriesQuery.data,
    ownDepartmentId,
    Number(watch('registryDate').slice(0, 4)),
  )
  const lockedNumber = existingLine?.registryNumberText
  const hadLockedNumber = useRef(false)

  // A locked number fills the field; moving the date to a year without a line empties it again.
  useEffect(() => {
    if (lockedNumber) {
      setValue('registryNumberText', lockedNumber, { shouldValidate: true })
    } else if (hadLockedNumber.current) {
      setValue('registryNumberText', '')
    }
    hadLockedNumber.current = Boolean(lockedNumber)
  }, [lockedNumber, setValue])

  // A locked number stays in the register it was first written in.
  const lockedRegistryId = existingLine?.registryId
  useEffect(() => {
    if (lockedRegistryId && registryOptions.some((option) => option.value === lockedRegistryId)) {
      setValue('registryId', lockedRegistryId)
    }
  }, [lockedRegistryId, registryOptions, setValue])

  const ownDepartment = (departmentsQuery.data ?? []).find(
    (department) => department.departmentId === ownDepartmentId,
  )

  const loading = departmentsQuery.isLoading || registries.isLoading || profileLoading
  // The profile finished loading without a department: an outdated session or API.
  const missingOwnDepartment = !profileLoading && !ownDepartmentId

  const onSubmit = handleSubmit(async (values) => {
    if (!lockedNumber && registryOptions.length > 1 && !values.registryId) {
      setError('registryId', { message: t('numbering.registration.error.registry-required') })
      return
    }

    try {
      await mutation.mutateAsync({
        departmentId: values.departmentId,
        registryId: values.registryId || null,
        registryNumberText: values.registryNumberText.trim(),
        registryDate: values.registryDate,
        notes: values.notes.trim() || null,
      })
      toast.success(t('numbering.registration.toast.saved'))
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
      title={t('numbering.registration.title')}
      description={t('numbering.registration.description')}
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
            form="department-registration-form"
            loading={mutation.isPending}
            disabled={loading || missingOwnDepartment || registryOptions.length === 0}
          >
            <Label k="numbering.registration.submit" />
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
        <FormField labelKey="numbering.registration.department" error={errors.departmentId?.message}>
          <Input
            value={
              loading
                ? t('numbering.loading')
                : ownDepartment
                  ? `${ownDepartment.code} — ${ownDepartment.nameAr}`
                  : ''
            }
            readOnly
            aria-label={t('numbering.registration.department')}
          />
        </FormField>

        <FormField
          labelKey="numbering.registration.registry"
          required={registryOptions.length > 1}
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
                disabled={mutation.isPending || loading || Boolean(lockedNumber)}
                invalid={Boolean(errors.registryId)}
                hideEmptyHint={missingOwnDepartment}
              />
            )}
          />
        </FormField>

        <FormField
          labelKey="numbering.registration.number"
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
            readOnly={Boolean(lockedNumber)}
            invalid={Boolean(errors.registryNumberText)}
            {...register('registryNumberText')}
          />
          {lockedNumber && (
            <p className="text-xs text-muted">
              <Label k="numbering.registration.locked-hint" />
            </p>
          )}
        </FormField>

        <FormField
          labelKey="numbering.registration.date"
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

        <FormField labelKey="numbering.registration.notes" error={errors.notes?.message} className="sm:col-span-2">
          <Textarea
            rows={2}
            placeholder={t('numbering.registration.notes-placeholder')}
            invalid={Boolean(errors.notes)}
            {...register('notes')}
          />
        </FormField>

        {missingOwnDepartment && (
          <p
            role="alert"
            className="sm:col-span-2 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            <Label k="numbering.registration.missing-department" />
          </p>
        )}

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
