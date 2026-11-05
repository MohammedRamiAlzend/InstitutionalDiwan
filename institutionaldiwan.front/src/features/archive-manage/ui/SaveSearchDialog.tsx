import { Controller } from 'react-hook-form'
import { useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import type { SavedSearchResponse } from '@/entities/archive'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Checkbox, Dialog, FormField, Input } from '@/shared/ui'
import {
  useCreateSavedSearchMutation,
  useUpdateSavedSearchMutation,
} from '../api/archiveMutations'
import { saveSearchSchema, type SaveSearchFormValues } from '../model/schemas'

interface SaveSearchDialogProps {
  /** The grid's current criteria, already serialised; saved verbatim. */
  criteriaJson: string
  /** When set the dialog renames/re-captures that search instead of creating a new one. */
  existing?: SavedSearchResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Saves the archive grid's current filters under a name, optionally as the caller's default. */
export function SaveSearchDialog({
  criteriaJson,
  existing,
  open,
  onOpenChange,
}: SaveSearchDialogProps) {
  const createMutation = useCreateSavedSearchMutation()
  const updateMutation = useUpdateSavedSearchMutation(existing?.savedSearchId ?? '')
  const mutation = existing ? updateMutation : createMutation

  const {
    control,
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<SaveSearchFormValues>({ formId: 'save-search' }, {
    resolver: zodResolver(saveSearchSchema),
    defaultValues: { name: existing?.name ?? '', isDefault: existing?.isDefault ?? false },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        name: values.name.trim(),
        criteriaJson,
        isDefault: values.isDefault,
      })
      toast.success(existing ? 'حُدّث البحث المحفوظ.' : 'حُفظ البحث.')
      draft.discard()
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && mutation.isPending) return
        onOpenChange(nextOpen)
      }}
      title={existing ? 'تعديل البحث المحفوظ' : 'حفظ البحث الحالي'}
      description="يُحفظ البحث والمرشحات الحالية باسم تختاره، ولا يراه غيرك."
      size="sm"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            تراجع
          </Button>
          <Button type="submit" form="save-search-form" loading={mutation.isPending}>
            حفظ
          </Button>
        </>
      }
    >
      <form id="save-search-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField label="اسم البحث" required error={errors.name?.message}>
          <Input
            autoFocus
            placeholder="مثال: كتب الشؤون المالية 2026"
            invalid={Boolean(errors.name)}
            {...register('name')}
          />
        </FormField>

        <Controller
          control={control}
          name="isDefault"
          render={({ field }) => (
            <Checkbox
              checked={field.value}
              onCheckedChange={field.onChange}
              label="اجعله البحث الافتراضي"
              description="يُطبَّق تلقائيًا عند فتح صفحة الأرشيف دون مرشحات."
            />
          )}
        />

        {mutation.error && (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
