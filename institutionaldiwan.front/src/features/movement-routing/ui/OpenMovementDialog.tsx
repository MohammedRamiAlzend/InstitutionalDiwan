import { useMemo, useState } from 'react'
import { Controller, useWatch } from 'react-hook-form'
import { t, useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { Route, Search } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { MOVEMENT_TYPE } from '@/shared/config'
import { toArabicErrorMessage } from '@/shared/api'
import {
  Button,
  Dialog,
  FormField,
  Input,
  Label,
  Select,
  Textarea,
} from '@/shared/ui'
import {
  useCancelMovementMutation,
  useCreateMovementMutation,
  useSendMovementMutation,
} from '../api/movementMutations'
import { openMovementSchema, type OpenMovementFormValues } from '../model/schemas'
import { useAssigneeOptions } from '../model/useAssigneeOptions'
import { ArchiveRouteHint } from './ArchiveRouteHint'

interface OpenMovementDialogProps {
  documentId: string
  documentTitle: string
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Prefills the destination when a decision suggests the next hop. */
  initialDestinationId?: string
  /** Prefills the movement type; defaults to a referral. */
  initialMovementTypeId?: number
}

/**
 * Opens a movement from the caller's primary department and sends it immediately, so the document
 * leaves the caller's hands in one step and no unsent draft lingers. The Central Diwan must sit on
 * one side of every hop, so the dialog says so before the API answers that rule.
 */
export function OpenMovementDialog({
  documentId,
  documentTitle,
  open,
  onOpenChange,
  initialDestinationId,
  initialMovementTypeId,
}: OpenMovementDialogProps) {
  const createMutation = useCreateMovementMutation()
  const sendMutation = useSendMovementMutation()
  const cancelMutation = useCancelMovementMutation()
  const pending = createMutation.isPending || sendMutation.isPending || cancelMutation.isPending
  const departmentsQuery = useDepartmentOptions({ enabled: open })
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.MovementTypes)
  const [assigneeSearch, setAssigneeSearch] = useState('')
  const assigneeOptions = useAssigneeOptions(assigneeSearch, open)
  const suggested = Boolean(initialDestinationId)

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<OpenMovementFormValues>({
    formId: suggested
      ? `open-movement:${documentId}:suggested:${initialDestinationId}:${initialMovementTypeId ?? MOVEMENT_TYPE.Referral}`
      : `open-movement:${documentId}`,
  }, {
    resolver: zodResolver(openMovementSchema),
    defaultValues: {
      destinationDepartmentId: initialDestinationId ?? '',
      movementTypeId: String(initialMovementTypeId ?? MOVEMENT_TYPE.Referral),
      assignedToUserId: '',
      instructions: '',
      dueDate: '',
    },
  })

  const selectedTypeId = useWatch({ control, name: 'movementTypeId' })

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

  const typeOptions = useMemo(
    () =>
      (typesQuery.data ?? []).map((type) => ({
        value: String(type.id),
        label: type.descAr,
      })),
    [typesQuery.data],
  )

  const onSubmit = handleSubmit(async (values) => {
    try {
      const created = await createMutation.mutateAsync({
        documentId,
        destinationDepartmentId: values.destinationDepartmentId,
        movementTypeId: Number(values.movementTypeId),
        assignedToUserId: values.assignedToUserId || null,
        instructions: values.instructions.trim() || null,
        dueDate: values.dueDate || null,
      })
      try {
        await sendMutation.mutateAsync({ movementId: created.movementId })
      } catch (sendError) {
        // Roll the unsent draft back so it cannot block the document; a stray draft would
        // otherwise be stranded because the movements tab no longer offers «إرسال».
        await cancelMutation
          .mutateAsync({
            movementId: created.movementId,
            reason: t('movements.open.auto-cancel-reason'),
          })
          .catch(() => undefined)
        throw sendError
      }
      toast.success(t('movements.open.toast'))
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
      title={t('movements.open.title')}
      description={t('movements.open.description', { title: documentTitle })}
      size="lg"
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
            form="open-movement-form"
            loading={pending}
            disabled={departmentOptions.length === 0}
          >
            <Route className="size-4 rtl:-scale-x-100" aria-hidden />
            <Label k="movements.send.submit" />
          </Button>
        </>
      }
    >
      <form
        id="open-movement-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <p className="rounded-lg border border-border bg-surface-muted px-3 py-2 text-xs text-muted sm:col-span-2">
          <Label k="movements.open.central-hint" />
        </p>

        <FormField
          labelKey="movements.open.field-destination"
          required
          error={errors.destinationDepartmentId?.message}
          className="sm:col-span-2"
        >
          <Controller
            control={control}
            name="destinationDepartmentId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={departmentOptions}
                disabled={pending || departmentsQuery.isLoading}
                invalid={Boolean(errors.destinationDepartmentId)}
                placeholder={
                  departmentsQuery.isLoading ? t('movements.loading') : t('movements.destination-placeholder')
                }
                aria-label={t('movements.open.field-destination')}
              />
            )}
          />
        </FormField>

        <FormField labelKey="movements.field.movement-type" required error={errors.movementTypeId?.message}>
          <Controller
            control={control}
            name="movementTypeId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={typeOptions}
                disabled={pending || typesQuery.isLoading}
                invalid={Boolean(errors.movementTypeId)}
                aria-label={t('movements.field.movement-type')}
              />
            )}
          />
          {selectedTypeId === String(MOVEMENT_TYPE.ToArchive) && <ArchiveRouteHint />}
        </FormField>

        <FormField labelKey="movements.field.due-date" error={errors.dueDate?.message}>
          <Input type="date" dir="ltr" invalid={Boolean(errors.dueDate)} {...register('dueDate')} />
        </FormField>

        <FormField
          labelKey="movements.open.assignee-search-label"
          htmlFor="open-movement-assignee-search"
          className="sm:col-span-2"
        >
          <Input
            id="open-movement-assignee-search"
            placeholder={t('movements.open.assignee-search-placeholder')}
            startIcon={<Search className="size-4" aria-hidden />}
            value={assigneeSearch}
            onChange={(event) => setAssigneeSearch(event.target.value)}
            disabled={pending}
          />
        </FormField>

        <FormField labelKey="movements.field.assignee" className="sm:col-span-2">
          <Controller
            control={control}
            name="assignedToUserId"
            render={({ field }) => (
              <Select
                value={field.value || 'none'}
                onValueChange={(value) => field.onChange(value === 'none' ? '' : value)}
                options={assigneeOptions.options}
                disabled={pending || assigneeOptions.isLoading}
                aria-label={t('movements.field.assignee')}
              />
            )}
          />
        </FormField>

        <FormField
          labelKey="movements.field.instructions"
          error={errors.instructions?.message}
          className="sm:col-span-2"
        >
          <Textarea
            rows={2}
            placeholder={t('movements.open.instructions-placeholder')}
            invalid={Boolean(errors.instructions)}
            {...register('instructions')}
          />
        </FormField>

        {(createMutation.error ?? sendMutation.error) && (
          <p
            role="alert"
            className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger sm:col-span-2"
          >
            {toArabicErrorMessage(createMutation.error ?? sendMutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
