import { useMemo, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { findDepartmentEntry, useDocumentById, useDocumentRegistryNumbers } from '@/entities/document'
import {
  DynamicFieldInputs,
  toFieldValueRequests,
  useDocumentTypeSchema,
  validateFieldValues,
  withFieldDefaults,
  type FieldValueErrors,
  type FieldValueMap,
} from '@/entities/document-schema'
import { useMovementById } from '@/entities/movement'
import { DepartmentRegistrySelect, useDepartmentRegistryOptions } from '@/entities/numbering-registry'
import { useSessionStore } from '@/entities/user'
import { ApiError, toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { t } from '@/shared/lib'
import { Button, Checkbox, Dialog, FormField, Input, Label, Textarea } from '@/shared/ui'
import { useReceiveMovementMutation } from '../api/movementMutations'
import { MOVEMENT_ACTION_LABEL_KEYS } from '../model/constants'
import { receiveMovementSchema, type ReceiveMovementFormValues } from '../model/schemas'

function todayIsoDate(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

interface ReceiveMovementDialogProps {
  movementId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * Receives a movement and writes the dated visit in the destination department's register. When the
 * department already has a number for the document in the date's year the number is shown locked;
 * otherwise the clerk types it. A clerk may tick "تسجيل الوثيقة برقم جديد" to write a returning
 * document under a new number: the earlier number becomes replaced and stays linked to the new one. Callers without NUMBERING_VIEW cannot see the lines, so the number
 * stays optional for them and the server decides. When the department has a published record form for
 * the document type, its fields follow ("سجل الديوان") and the required ones must be filled to receive.
 */
export function ReceiveMovementDialog({
  movementId,
  open,
  onOpenChange,
}: ReceiveMovementDialogProps) {
  const mutation = useReceiveMovementMutation()
  const canViewNumbering = useSessionStore((state) =>
    state.hasPermission(Permissions.NumberingView),
  )
  const movementQuery = useMovementById(movementId, { enabled: open })
  const documentId = movementQuery.data?.documentId
  const entriesQuery = useDocumentRegistryNumbers(documentId, {
    enabled: open && canViewNumbering,
  })

  const {
    register,
    handleSubmit,
    setError,
    clearErrors,
    control,
    formState: { errors },
  } = useForm<ReceiveMovementFormValues>({
    resolver: zodResolver(receiveMovementSchema),
    defaultValues: { registryId: '', registryNumberText: '', registryDate: todayIsoDate(), notes: '' },
  })

  const year = Number(useWatch({ control, name: 'registryDate' }).slice(0, 4))
  const existingLine = findDepartmentEntry(
    entriesQuery.data,
    movementQuery.data?.destinationDepartmentId,
    year,
  )
  const registries = useDepartmentRegistryOptions(movementQuery.data?.destinationDepartmentId, {
    enabled: open && canViewNumbering,
  })
  const existingRegistryLabel = registries.options.find(
    (option) => option.value === existingLine?.registryId,
  )?.label
  const linesKnown = canViewNumbering && entriesQuery.isSuccess
  // A department may write a returning document under a new number; the earlier one stays linked.
  const [issueNewNumber, setIssueNewNumber] = useState(false)
  const reusedLine = issueNewNumber ? undefined : existingLine

  // The destination department's own record form for this document type, when one is published.
  const documentQuery = useDocumentById(documentId, { enabled: open && Boolean(documentId) })
  const recordFormQuery = useDocumentTypeSchema(documentQuery.data?.documentTypeId, {
    enabled: open && Boolean(movementQuery.data),
    departmentId: movementQuery.data?.destinationDepartmentId,
  })
  const recordFields = recordFormQuery.data?.schema?.fields ?? []
  const [recordValues, setRecordValues] = useState<FieldValueMap>({})
  const [recordErrors, setRecordErrors] = useState<FieldValueErrors>({})
  const effectiveRecordValues = useMemo(
    () => withFieldDefaults(recordFormQuery.data?.schema?.fields ?? [], recordValues),
    [recordFormQuery.data, recordValues],
  )

  const loading =
    movementQuery.isLoading ||
    documentQuery.isLoading ||
    recordFormQuery.isLoading ||
    (canViewNumbering && (entriesQuery.isLoading || registries.isLoading))

  const submit = handleSubmit(async (values) => {
    const typedNumber = values.registryNumberText.trim()
    if (!reusedLine && registries.options.length > 1 && !values.registryId) {
      setError('registryId', { message: t('movements.receive.error.registry-required') })
      return
    }
    if (linesKnown && !reusedLine && !typedNumber) {
      setError('registryNumberText', { message: t('movements.receive.error.number-required') })
      return
    }
    if (existingLine && issueNewNumber && typedNumber === existingLine.registryNumberText) {
      setError('registryNumberText', { message: t('movements.receive.error.number-same') })
      return
    }

    // Required record fields block the receipt here; the server checks them again.
    const recordFieldErrors = validateFieldValues(recordFields, effectiveRecordValues)
    setRecordErrors(recordFieldErrors)
    if (Object.keys(recordFieldErrors).length > 0) return

    try {
      const receipt = await mutation.mutateAsync({
        movementId,
        notes: values.notes.trim() || null,
        registryDate: values.registryDate,
        registryNumberText: reusedLine ? null : typedNumber || null,
        registryId: reusedLine ? null : values.registryId || null,
        ...(existingLine && issueNewNumber ? { issueNewNumber: true } : {}),
        ...(recordFields.length > 0
          ? { fieldValues: toFieldValueRequests(recordFields, effectiveRecordValues) }
          : {}),
      })
      toast.success(
        receipt.replacedNumberText
          ? t('movements.receive.toast.replaced', {
              number: receipt.registryNumberText,
              replaced: receipt.replacedNumberText,
            })
          : receipt.isReturnVisit
          ? t('movements.receive.toast.return', { number: receipt.registryNumberText })
          : t('movements.receive.toast.normal', { number: receipt.registryNumberText }),
      )
      onOpenChange(false)
    } catch (error) {
      if (!(error instanceof ApiError)) return
      const message = toArabicErrorMessage(error)
      if (error.code === 'Numbering.DepartmentRegistryMismatch') {
        setError('registryId', { type: 'server', message })
      } else if (error.code === 'Numbering.VisitDateDuplicate' || error.code === 'Numbering.RegistryDateInvalid') {
        setError('registryDate', { type: 'server', message })
      } else if (
        error.code === 'Numbering.RegistryNumberTaken' ||
        error.code === 'Numbering.DepartmentNumberLocked' ||
        error.code === 'Numbering.DepartmentRegistrationRequired'
      ) {
        setError('registryNumberText', { type: 'server', message })
      }
    }
  })

  const fieldErrorShown = Boolean(errors.registryId || errors.registryDate || errors.registryNumberText)

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t(MOVEMENT_ACTION_LABEL_KEYS.receive)}
      description={t('movements.receive.description')}
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
          <Button
            type="submit"
            form="movement-receive-form"
            loading={mutation.isPending}
            disabled={loading}
          >
            <Label k="movements.receive.submit" />
          </Button>
        </>
      }
    >
      <form
        id="movement-receive-form"
        onSubmit={submit}
        noValidate
        className="flex flex-col gap-4"
      >
        {reusedLine ? (
          existingRegistryLabel && (
            <FormField key="registry-locked" labelKey="movements.receive.field-registry" htmlFor="movement-receive-registry-locked">
              <Input id="movement-receive-registry-locked" value={existingRegistryLabel} readOnly />
            </FormField>
          )
        ) : (
          canViewNumbering && (
            <FormField
              key="registry-choice"
              labelKey="movements.receive.field-registry"
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
                    disabled={mutation.isPending || loading}
                    invalid={Boolean(errors.registryId)}
                  />
                )}
              />
            </FormField>
          )
        )}

        {reusedLine ? (
          <FormField key="locked" labelKey="movements.receive.field-number" htmlFor="movement-receive-number-locked">
            <Input
              id="movement-receive-number-locked"
              dir="ltr"
              className="font-mono"
              value={reusedLine.registryNumberText}
              readOnly
            />
            <p className="text-xs text-muted"><Label k="movements.receive.revisit-hint" /></p>
          </FormField>
        ) : (
          <FormField
            key="typed"
            labelKey={existingLine ? 'movements.receive.field-number-new' : 'movements.receive.field-number'}
            required={linesKnown}
            error={errors.registryNumberText?.message}
            htmlFor="movement-receive-number"
          >
            <Input
              id="movement-receive-number"
              dir="ltr"
              className="font-mono"
              placeholder="FIN-001/2026"
              disabled={mutation.isPending || loading}
              invalid={Boolean(errors.registryNumberText)}
              {...register('registryNumberText')}
            />
            {!linesKnown && !loading && (
              <p className="text-xs text-muted">
                <Label k="movements.receive.number-optional-hint" />
              </p>
            )}
            {existingLine && (
              <p className="text-xs text-muted">
                <Label k="movements.receive.replaces-prefix" />{' '}
                <span dir="ltr" className="font-mono text-foreground">
                  {existingLine.registryNumberText}
                </span>{' '}
                <Label k="movements.receive.replaces-suffix" />
              </p>
            )}
          </FormField>
        )}

        {existingLine && (
          <Checkbox
            checked={issueNewNumber}
            onCheckedChange={(checked) => {
              setIssueNewNumber(checked)
              clearErrors(['registryId', 'registryNumberText'])
            }}
            disabled={mutation.isPending}
            label={t('movements.receive.new-number-label')}
            description={t('movements.receive.new-number-description')}
          />
        )}

        <FormField
          labelKey="movements.receive.field-date"
          required
          error={errors.registryDate?.message}
          htmlFor="movement-receive-date"
        >
          <Input
            id="movement-receive-date"
            type="date"
            dir="ltr"
            disabled={mutation.isPending}
            invalid={Boolean(errors.registryDate)}
            {...register('registryDate')}
          />
        </FormField>

        {recordFields.length > 0 && (
          <section
            aria-label={t('movements.receive.record-section-aria')}
            className="flex flex-col gap-4 border-t border-border pt-4"
          >
            <div>
              <h3 className="text-sm font-semibold text-foreground">
                <Label k="movements.receive.record-title" />
              </h3>
              <p className="text-xs text-muted">
                <Label k="movements.receive.record-hint" />
              </p>
            </div>
            <DynamicFieldInputs
              fields={recordFields}
              values={effectiveRecordValues}
              errors={recordErrors}
              disabled={mutation.isPending}
              onChange={(key, value) => {
                setRecordValues((current) => ({ ...current, [key]: value }))
                setRecordErrors((current) => {
                  if (!(key in current)) return current
                  return Object.fromEntries(Object.entries(current).filter(([k]) => k !== key))
                })
              }}
            />
          </section>
        )}

        <FormField labelKey="movements.receive.notes-label" error={errors.notes?.message}>
          <Textarea
            rows={3}
            placeholder={t('movements.receive.notes-placeholder')}
            invalid={Boolean(errors.notes)}
            {...register('notes')}
          />
        </FormField>

        {mutation.error && !fieldErrorShown ? (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(mutation.error)}
          </p>
        ) : null}
      </form>
    </Dialog>
  )
}
