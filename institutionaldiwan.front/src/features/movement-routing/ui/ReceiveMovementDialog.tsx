import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { findDepartmentEntry, useDocumentRegistryNumbers } from '@/entities/document'
import { useMovementById } from '@/entities/movement'
import { DepartmentRegistrySelect, useDepartmentRegistryOptions } from '@/entities/numbering-registry'
import { useSessionStore } from '@/entities/user'
import { ApiError, toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { Button, Dialog, FormField, Input, Textarea } from '@/shared/ui'
import { useReceiveMovementMutation } from '../api/movementMutations'
import { MOVEMENT_ACTION_LABELS } from '../model/constants'
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
 * otherwise the clerk types it. Callers without NUMBERING_VIEW cannot see the lines, so the number
 * stays optional for them and the server decides.
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
  const loading =
    movementQuery.isLoading ||
    (canViewNumbering && (entriesQuery.isLoading || registries.isLoading))

  const submit = handleSubmit(async (values) => {
    const typedNumber = values.registryNumberText.trim()
    if (!existingLine && registries.options.length > 1 && !values.registryId) {
      setError('registryId', { message: 'اختر السجل الذي سُجّل فيه الرقم.' })
      return
    }
    if (linesKnown && !existingLine && !typedNumber) {
      setError('registryNumberText', { message: 'أدخل رقم سجل الدائرة لهذه السنة.' })
      return
    }

    try {
      const receipt = await mutation.mutateAsync({
        movementId,
        notes: values.notes.trim() || null,
        registryDate: values.registryDate,
        registryNumberText: existingLine ? null : typedNumber || null,
        registryId: existingLine ? null : values.registryId || null,
      })
      toast.success(
        receipt.isReturnVisit
          ? `تم الاستلام بنفس رقم الدائرة ${receipt.registryNumberText} وبتاريخ جديد.`
          : `تم استلام الوثيقة برقم الدائرة ${receipt.registryNumberText}.`,
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
      title={MOVEMENT_ACTION_LABELS.receive}
      description="يُكتب الاستلام في سجل الدائرة الوجهة: رقم واحد للوثيقة في السنة، وتاريخ جديد في كل مرة تصل فيها."
      size="sm"
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
            form="movement-receive-form"
            loading={mutation.isPending}
            disabled={loading}
          >
            استلام الوثيقة
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
        {existingLine ? (
          existingRegistryLabel && (
            <FormField key="registry-locked" label="السجل" htmlFor="movement-receive-registry-locked">
              <Input id="movement-receive-registry-locked" value={existingRegistryLabel} readOnly />
            </FormField>
          )
        ) : (
          canViewNumbering && (
            <FormField
              key="registry-choice"
              label="السجل"
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

        {existingLine ? (
          <FormField key="locked" label="رقم سجل الدائرة" htmlFor="movement-receive-number-locked">
            <Input
              id="movement-receive-number-locked"
              dir="ltr"
              className="font-mono"
              value={existingLine.registryNumberText}
              readOnly
            />
            <p className="text-xs text-muted">زيارة متكررة، الرقم نفسه بتاريخ جديد.</p>
          </FormField>
        ) : (
          <FormField
            key="typed"
            label="رقم سجل الدائرة"
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
                اتركه فارغًا إذا كان لدائرتك رقم لهذه الوثيقة في هذه السنة.
              </p>
            )}
          </FormField>
        )}

        <FormField
          label="تاريخ التسجيل"
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

        <FormField label="ملاحظات الاستلام (اختياري)" error={errors.notes?.message}>
          <Textarea
            rows={3}
            placeholder="ملاحظة اختيارية تُرفق بالاستلام…"
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
