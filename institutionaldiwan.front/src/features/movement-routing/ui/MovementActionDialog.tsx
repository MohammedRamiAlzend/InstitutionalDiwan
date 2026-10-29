import { useMemo, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Search } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import type { MovementAction } from '@/entities/movement'
import { toArabicErrorMessage } from '@/shared/api'
import {
  Button,
  Dialog,
  FormField,
  Input,
  Select,
  Textarea,
} from '@/shared/ui'
import {
  useCancelMovementMutation,
  useCloseMovementMutation,
  useCompleteMovementMutation,
  useReceiveMovementMutation,
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
import { useAssigneeOptions } from '../model/useAssigneeOptions'
import { MOVEMENT_ACTION_LABELS } from '../model/constants'

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
        <ReceiveActionDialog
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

/** Shared optional-notes dialog for send, receive, complete, and close. */
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
            إلغاء
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
            إلغاء
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
        <FormField label="السبب" required error={errors.reason?.message}>
          <Textarea
            rows={3}
            placeholder="اكتب سببًا واضحًا يظهر في سجل الحركة…"
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
      title={MOVEMENT_ACTION_LABELS.send}
      description="سيُعلَّم القسم الوجهة بأن الوثيقة في طريقه؛ تبقى الوثيقة قيد التداول حتى الإغلاق."
      notesLabel="ملاحظات الإرسال (اختياري)"
      placeholder="ملاحظة اختيارية تُرفق بالإرسال…"
      submitLabel="إرسال الحركة"
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (notes) => {
        await mutation.mutateAsync({ movementId, notes })
        toast.success('تم إرسال الحركة إلى القسم الوجهة.')
        onOpenChange(false)
      }}
    />
  )
}

function ReceiveActionDialog({
  movementId,
  open,
  onOpenChange,
}: ActionDialogBaseProps) {
  const mutation = useReceiveMovementMutation()

  return (
    <NotesFormDialog
      open={open}
      onOpenChange={onOpenChange}
      title={MOVEMENT_ACTION_LABELS.receive}
      description="يشترط الاستلام أن يكون القسم الوجهة قد سجّل رقمه في سجلّه لهذه الوثيقة؛ سيُربط الاستلام برقم التسجيل."
      notesLabel="ملاحظات الاستلام (اختياري)"
      placeholder="ملاحظة اختيارية تُرفق بالاستلام…"
      submitLabel="استلام الوثيقة"
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (notes) => {
        const receipt = await mutation.mutateAsync({ movementId, notes })
        toast.success(
          `تم استلام الوثيقة برقم الدائرة ${receipt.registryNumberText}.`,
        )
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
      title={MOVEMENT_ACTION_LABELS.complete}
      description="تُغلق الخطوة الحالية كمكتملة، وتبقى الحركة مفتوحة حتى تُغلق."
      notesLabel="ملاحظات النتيجة (اختياري)"
      placeholder="نتيجة المعالجة في هذا القسم…"
      submitLabel="إكمال الخطوة"
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (notes) => {
        await mutation.mutateAsync({ movementId, notes })
        toast.success('اكتملت الخطوة الحالية للحركة.')
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
      title={MOVEMENT_ACTION_LABELS.close}
      description="يُغلق سجل الحركة وتعود الوثيقة إلى حالة مستقرة قابلة للتوجيه من جديد أو للأرشفة."
      notesLabel="ملاحظات الإغلاق (اختياري)"
      placeholder="ملاحظة ختامية عن الحركة…"
      submitLabel="إغلاق الحركة"
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (notes) => {
        await mutation.mutateAsync({ movementId, notes })
        toast.success('أُغلقت الحركة وأُطلقت الوثيقة.')
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
      title={MOVEMENT_ACTION_LABELS.reject}
      description="الرفض يتطلب سببًا. إذا لم يكن قسمك هو الديوان المركزي فستُعاد الوثيقة إليه في خطوة جديدة."
      submitLabel="رفض الحركة"
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (reason) => {
        await mutation.mutateAsync({ movementId, reason })
        toast.success('تم رفض الحركة.')
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
      title={MOVEMENT_ACTION_LABELS.return}
      description="إرجاع الوثيقة إلى القسم المُرسل يتطلب سببًا، وتُعرض الحالة «مُعاد»."
      submitLabel="إرجاع الوثيقة"
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (reason) => {
        await mutation.mutateAsync({ movementId, reason })
        toast.success('أُعيدت الوثيقة إلى القسم المُرسل.')
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
      title={MOVEMENT_ACTION_LABELS.cancel}
      description="إلغاء الحركة نهائي ولا يمكن التراجع عنه، ويتطلب سببًا يظهر في سجل الحركة."
      submitLabel="إلغاء الحركة"
      destructive
      isPending={mutation.isPending}
      error={mutation.error}
      onSubmit={async (reason) => {
        await mutation.mutateAsync({ movementId, reason })
        toast.success('أُلغيت الحركة.')
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
  const [assigneeSearch, setAssigneeSearch] = useState('')
  const assigneeOptions = useAssigneeOptions(assigneeSearch, open)

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<RedirectMovementFormValues>({
    resolver: zodResolver(redirectMovementSchema),
    defaultValues: {
      newDestinationDepartmentId: '',
      assignedToUserId: '',
      instructions: '',
      dueDate: '',
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
        assignedToUserId: values.assignedToUserId || null,
        instructions: values.instructions.trim() || null,
        dueDate: values.dueDate || null,
      })
      toast.success('أُعيد توجيه الحركة إلى القسم الجديد.')
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={MOVEMENT_ACTION_LABELS.redirect}
      description="تُضاف خطوة جديدة وتُرسل مباشرة إلى القسم الجديد دون إغلاق الحركة."
      size="lg"
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
            form="redirect-movement-form"
            loading={mutation.isPending}
          >
            إعادة التوجيه
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
          يجب أن يكون الديوان المركزي على أحد طرفي التوجيه الجديد؛ لا تنتقل الوثيقة مباشرة بين قسمين
          عاديين.
        </p>

        <FormField
          label="القسم الوجهة الجديد"
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
                  departmentsQuery.isLoading ? 'جارٍ التحميل…' : 'اختر القسم…'
                }
                aria-label="القسم الوجهة الجديد"
              />
            )}
          />
        </FormField>

        <FormField label="تاريخ الاستحقاق" error={errors.dueDate?.message}>
          <Input type="date" dir="ltr" invalid={Boolean(errors.dueDate)} {...register('dueDate')} />
        </FormField>

        <FormField
          label="البحث عن مكلَّف (اختياري)"
          htmlFor="redirect-movement-assignee-search"
        >
          <Input
            id="redirect-movement-assignee-search"
            placeholder="ابحث باسم المستخدم…"
            startIcon={<Search className="size-4" aria-hidden />}
            value={assigneeSearch}
            onChange={(event) => setAssigneeSearch(event.target.value)}
            disabled={mutation.isPending}
          />
        </FormField>

        <FormField label="المكلَّف" className="sm:col-span-2">
          <Controller
            control={control}
            name="assignedToUserId"
            render={({ field }) => (
              <Select
                value={field.value || 'none'}
                onValueChange={(value) => field.onChange(value === 'none' ? '' : value)}
                options={assigneeOptions.options}
                disabled={mutation.isPending || assigneeOptions.isLoading}
                aria-label="المكلَّف"
              />
            )}
          />
        </FormField>

        <FormField
          label="تعليمات"
          error={errors.instructions?.message}
          className="sm:col-span-2"
        >
          <Textarea
            rows={2}
            placeholder="إرشادات اختيارية للقسم الجديد…"
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
