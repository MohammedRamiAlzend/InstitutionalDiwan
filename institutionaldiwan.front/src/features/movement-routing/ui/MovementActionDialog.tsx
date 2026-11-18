import { useMemo } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import type { MovementAction } from '@/entities/movement'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import {
  Button,
  Dialog,
  FormField,
  Label,
  Select,
  Textarea,
} from '@/shared/ui'
import {
  useCancelMovementMutation,
  useCloseMovementMutation,
  useCompleteMovementMutation,
  useRedirectMovementMutation,
  useRejectMovementMutation,
  useReturnMovementMutation,
  useSendMovementMutation,
} from '../api/movementMutations'
import {
  movementNotesSchema,
  movementReasonSchema,
  redirectMovementSchema,
  type MovementNotesFormValues,
  type MovementReasonFormValues,
  type RedirectMovementFormValues,
} from '../model/schemas'
import { MOVEMENT_ACTION_LABEL_KEYS } from '../model/constants'
import { ReceiveMovementDialog } from './ReceiveMovementDialog'

interface MovementActionDialogProps {
  action: MovementAction
  movementId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** One entry point for every routing action except opening a movement. */
export function MovementActionDialog({
  action,
  movementId,
  open,
  onOpenChange,
}: MovementActionDialogProps) {
  switch (action) {
    case 'send':
      return (
        <SendActionDialog
          movementId={movementId}
          open={open}
          onOpenChange={onOpenChange}
        />
      )
    case 'receive':
      return (
        <ReceiveMovementDialog
          movementId={movementId}
          open={open}
          onOpenChange={onOpenChange}
        />
      )
    case 'complete':
      return (
        <CompleteActionDialog
          movementId={movementId}
          open={open}
          onOpenChange={onOpenChange}
        />
      )
    case 'close':
      return (
        <CloseActionDialog
          movementId={movementId}
          open={open}
          onOpenChange={onOpenChange}
        />
      )
    case 'reject':
      return (
        <RejectActionDialog
          movementId={movementId}
          open={open}
          onOpenChange={onOpenChange}
        />
      )
    case 'return':
      return (
        <ReturnActionDialog
          movementId={movementId}
          open={open}
          onOpenChange={onOpenChange}
        />
      )
    case 'cancel':
      return (
        <CancelActionDialog
          movementId={movementId}
          open={open}
          onOpenChange={onOpenChange}
        />
      )
    case 'redirect':
      return (
        <RedirectActionDialog
          movementId={movementId}
          open={open}
          onOpenChange={onOpenChange}
        />
      )
    default:
      return null
  }
}

interface ActionDialogBaseProps {
  movementId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

interface NotesFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: string
  notesLabel: string
  placeholder: string
  submitLabel: string
  isPending: boolean
  error: unknown
  onSubmit: (notes: string | null) => Promise<void>
}

/** Shared optional-notes dialog for send, complete, and close. */
function NotesFormDialog({
  open,
  onOpenChange,
  title,
  description,
  notesLabel,
  placeholder,
  submitLabel,
  isPending,
  error,
  onSubmit,
}: NotesFormDialogProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<MovementNotesFormValues>({
    resolver: zodResolver(movementNotesSchema),
    defaultValues: { notes: '' },
  })

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values.notes.trim() || null)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={title}
      description={description}
      size="sm"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isPending}
          >
            <Label k="common.action.cancel" />
          </Button>
          <Button type="submit" form="movement-notes-form" loading={isPending}>
            {submitLabel}
          </Button>
        </>
      }
    >
      <form id="movement-notes-form" onSubmit={submit} noValidate>
        <FormField label={notesLabel} error={errors.notes?.message}>
          <Textarea
            rows={3}
            placeholder={placeholder}
            invalid={Boolean(errors.notes)}
            {...register('notes')}
          />
        </FormField>

        {error ? (
          <p
            role="alert"
            className="mt-4 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {toArabicErrorMessage(error)}
          </p>
        ) : null}
      </form>
    </Dialog>
  )
}

interface ReasonFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: string
  submitLabel: string
  destructive?: boolean
  isPending: boolean
  error: unknown
  onSubmit: (reason: string) => Promise<void>
}

/** Shared required-reason dialog for reject, return, and cancel. */
function ReasonFormDialog({
  open,
  onOpenChange,
  title,
  description,
  submitLabel,
  destructive,
  isPending,
  error,
  onSubmit,
}: ReasonFormDialogProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<MovementReasonFormValues>({
    resolver: zodResolver(movementReasonSchema),
    defaultValues: { reason: '' },
  })

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values.reason.trim())
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={title}
      description={description}
      size="sm"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isPending}
          >
            <Label k="common.action.cancel" />
          </Button>
          <Button
            type="submit"
            form="movement-reason-form"
            variant={destructive ? 'destructive' : 'primary'}
            loading={isPending}
          >
            {submitLabel}
          </Button>
        </>
      }
    >
      <form id="movement-reason-form" onSubmit={submit} noValidate>
        <FormField labelKey="movements.field.reason" required error={errors.reason?.message}>
          <Textarea
            rows={3}
            placeholder={t('movements.reason-placeholder')}
            invalid={Boolean(errors.reason)}
            {...register('reason')}
          />
        </FormField>

        {error ? (
          <p
            role="alert"
            className="mt-4 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {toArabicErrorMessage(error)}
          </p>
        ) : null}
      </form>
    </Dialog>
  )
}

function SendActionDialog({ movementId, open, onOpenChange }: ActionDialogBaseProps) {
  const mutation = useSendMovementMutation()

  return (
    <NotesFormDialog
      open={open}
      onOpenChange={onOpenChange}
      title={t(MOVEMENT_ACTION_LABEL_KEYS.send)}
      description={t('movements.send.description')}
      notesLabel={t('movements.send.notes-label')}
      placeholder={t('movements.send.notes-placeholder')}
      submitLabel={t('movements.send.submit')}
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (notes) => {
        await mutation.mutateAsync({ movementId, notes })
        toast.success(t('movements.send.toast'))
        onOpenChange(false)
      }}
    />
  )
}

function CompleteActionDialog({
  movementId,
  open,
  onOpenChange,
}: ActionDialogBaseProps) {
  const mutation = useCompleteMovementMutation()

  return (
    <NotesFormDialog
      open={open}
      onOpenChange={onOpenChange}
      title={t(MOVEMENT_ACTION_LABEL_KEYS.complete)}
      description={t('movements.complete.description')}
      notesLabel={t('movements.complete.notes-label')}
      placeholder={t('movements.complete.notes-placeholder')}
      submitLabel={t('movements.complete.submit')}
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (notes) => {
        await mutation.mutateAsync({ movementId, notes })
        toast.success(t('movements.complete.toast'))
        onOpenChange(false)
      }}
    />
  )
}

function CloseActionDialog({ movementId, open, onOpenChange }: ActionDialogBaseProps) {
  const mutation = useCloseMovementMutation()

  return (
    <NotesFormDialog
      open={open}
      onOpenChange={onOpenChange}
      title={t(MOVEMENT_ACTION_LABEL_KEYS.close)}
      description={t('movements.close.description')}
      notesLabel={t('movements.close.notes-label')}
      placeholder={t('movements.close.notes-placeholder')}
      submitLabel={t('movements.close.submit')}
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (notes) => {
        await mutation.mutateAsync({ movementId, notes })
        toast.success(t('movements.close.toast'))
        onOpenChange(false)
      }}
    />
  )
}

function RejectActionDialog({ movementId, open, onOpenChange }: ActionDialogBaseProps) {
  const mutation = useRejectMovementMutation()

  return (
    <ReasonFormDialog
      open={open}
      onOpenChange={onOpenChange}
      title={t(MOVEMENT_ACTION_LABEL_KEYS.reject)}
      description={t('movements.reject.description')}
      submitLabel={t('movements.reject.submit')}
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (reason) => {
        await mutation.mutateAsync({ movementId, reason })
        toast.success(t('movements.reject.toast'))
        onOpenChange(false)
      }}
    />
  )
}

function ReturnActionDialog({ movementId, open, onOpenChange }: ActionDialogBaseProps) {
  const mutation = useReturnMovementMutation()

  return (
    <ReasonFormDialog
      open={open}
      onOpenChange={onOpenChange}
      title={t(MOVEMENT_ACTION_LABEL_KEYS.return)}
      description={t('movements.return.description')}
      submitLabel={t('movements.return.submit')}
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (reason) => {
        await mutation.mutateAsync({ movementId, reason })
        toast.success(t('movements.return.toast'))
        onOpenChange(false)
      }}
    />
  )
}

function CancelActionDialog({ movementId, open, onOpenChange }: ActionDialogBaseProps) {
  const mutation = useCancelMovementMutation()

  return (
    <ReasonFormDialog
      open={open}
      onOpenChange={onOpenChange}
      title={t(MOVEMENT_ACTION_LABEL_KEYS.cancel)}
      description={t('movements.cancel.description')}
      submitLabel={t('movements.action.cancel')}
      destructive
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (reason) => {
        await mutation.mutateAsync({ movementId, reason })
        toast.success(t('movements.cancel.toast'))
        onOpenChange(false)
      }}
    />
  )
}

function RedirectActionDialog({
  movementId,
  open,
  onOpenChange,
}: ActionDialogBaseProps) {
  const mutation = useRedirectMovementMutation()
  const departmentsQuery = useDepartmentOptions({ enabled: open })

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<RedirectMovementFormValues>({
    resolver: zodResolver(redirectMovementSchema),
    defaultValues: {
      newDestinationDepartmentId: '',
      instructions: '',
    },
  })

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

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        movementId,
        newDestinationDepartmentId: values.newDestinationDepartmentId,
        instructions: values.instructions.trim() || null,
      })
      toast.success(t('movements.redirect.toast'))
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t(MOVEMENT_ACTION_LABEL_KEYS.redirect)}
      description={t('movements.redirect.description')}
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
            form="redirect-movement-form"
            loading={mutation.isPending}
          >
            <Label k="movements.redirect.submit" />
          </Button>
        </>
      }
    >
      <form
        id="redirect-movement-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <p className="rounded-lg border border-border bg-surface-muted px-3 py-2 text-xs text-muted sm:col-span-2">
          <Label k="movements.redirect.central-hint" />
        </p>

        <FormField
          labelKey="movements.redirect.field-destination"
          required
          error={errors.newDestinationDepartmentId?.message}
          className="sm:col-span-2"
        >
          <Controller
            control={control}
            name="newDestinationDepartmentId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={departmentOptions}
                disabled={mutation.isPending || departmentsQuery.isLoading}
                invalid={Boolean(errors.newDestinationDepartmentId)}
                placeholder={
                  departmentsQuery.isLoading ? t('movements.loading') : t('movements.destination-placeholder')
                }
                aria-label={t('movements.redirect.field-destination')}
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
            placeholder={t('movements.redirect.instructions-placeholder')}
            invalid={Boolean(errors.instructions)}
            {...register('instructions')}
          />
        </FormField>

        {mutation.error && (
          <p
            role="alert"
            className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger sm:col-span-2"
          >
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
