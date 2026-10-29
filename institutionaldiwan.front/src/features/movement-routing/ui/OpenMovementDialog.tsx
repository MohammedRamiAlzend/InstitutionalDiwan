import { useMemo, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
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
  Select,
  Textarea,
} from '@/shared/ui'
import { useCreateMovementMutation } from '../api/movementMutations'
import { openMovementSchema, type OpenMovementFormValues } from '../model/schemas'
import { useAssigneeOptions } from '../model/useAssigneeOptions'

interface OpenMovementDialogProps {
  documentId: string
  documentTitle: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * Opens a draft movement from the caller's primary department. The Central Diwan must sit on one
 * side of every hop, so the dialog says so before the API answers that rule.
 */
export function OpenMovementDialog({
  documentId,
  documentTitle,
  open,
  onOpenChange,
}: OpenMovementDialogProps) {
  const mutation = useCreateMovementMutation()
  const departmentsQuery = useDepartmentOptions({ enabled: open })
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.MovementTypes)
  const [assigneeSearch, setAssigneeSearch] = useState('')
  const assigneeOptions = useAssigneeOptions(assigneeSearch, open)

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<OpenMovementFormValues>({
    resolver: zodResolver(openMovementSchema),
    defaultValues: {
      destinationDepartmentId: '',
      movementTypeId: String(MOVEMENT_TYPE.Referral),
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
      await mutation.mutateAsync({
        documentId,
        destinationDepartmentId: values.destinationDepartmentId,
        movementTypeId: Number(values.movementTypeId),
        assignedToUserId: values.assignedToUserId || null,
        instructions: values.instructions.trim() || null,
        dueDate: values.dueDate || null,
      })
      toast.success('فُتحت الحركة كمسودة. أرسلها الآن لتصل إلى القسم الوجهة.')
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="فتح حركة"
      description={`توجيه «${documentTitle}» من قسمك الأساسي إلى قسم آخر.`}
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
            form="open-movement-form"
            loading={mutation.isPending}
            disabled={departmentOptions.length === 0}
          >
            <Route className="size-4 rtl:-scale-x-100" aria-hidden />
            فتح الحركة
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
          الديوان المركزي إلزامي: يمر كل توجيه عبر الديوان المركزي ما لم يكن قسمك أو القسم الوجهة
          هو الديوان المركزي. تبقى الحركة مسودة حتى تُرسلها، ولا يمكن الاستلام قبل أن يسجّل القسم
          الوجهة رقمه في سجلّه.
        </p>

        <FormField
          label="القسم الوجهة"
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
                disabled={mutation.isPending || departmentsQuery.isLoading}
                invalid={Boolean(errors.destinationDepartmentId)}
                placeholder={
                  departmentsQuery.isLoading ? 'جارٍ التحميل…' : 'اختر القسم…'
                }
                aria-label="القسم الوجهة"
              />
            )}
          />
        </FormField>

        <FormField label="نوع الحركة" required error={errors.movementTypeId?.message}>
          <Controller
            control={control}
            name="movementTypeId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={typeOptions}
                disabled={mutation.isPending || typesQuery.isLoading}
                invalid={Boolean(errors.movementTypeId)}
                aria-label="نوع الحركة"
              />
            )}
          />
        </FormField>

        <FormField label="تاريخ الاستحقاق" error={errors.dueDate?.message}>
          <Input type="date" dir="ltr" invalid={Boolean(errors.dueDate)} {...register('dueDate')} />
        </FormField>

        <FormField
          label="البحث عن مكلَّف (اختياري)"
          htmlFor="open-movement-assignee-search"
          className="sm:col-span-2"
        >
          <Input
            id="open-movement-assignee-search"
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
            placeholder="إرشادات اختيارية للقسم الوجهة…"
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
