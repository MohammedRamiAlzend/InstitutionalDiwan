import { t, useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { Hash, Languages } from 'lucide-react'
import { toast } from 'sonner'
import type { LookupEntryResponse } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Label } from '@/shared/ui'
import {
  useCreateLookupEntryMutation,
  useRelabelLookupEntryMutation,
} from '../api/lookupMutations'
import { lookupEntryFormSchema, type LookupEntryFormValues } from '../model/schemas'

interface LookupEntryFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  lookup: string
  /** `null` adds an entry; an entry relabels the existing one. */
  entry: LookupEntryResponse | null
  /** Pre-filled id for a new entry (next free id). */
  suggestedId: number
}

/** Add/relabel form for `POST` and `PUT /api/lookups/{lookup}`. */
export function LookupEntryFormDialog({
  open,
  onOpenChange,
  lookup,
  entry,
  suggestedId,
}: LookupEntryFormDialogProps) {
  const isEdit = entry !== null
  const createMutation = useCreateLookupEntryMutation()
  const relabelMutation = useRelabelLookupEntryMutation()
  const pending = createMutation.isPending || relabelMutation.isPending
  const mutationError = createMutation.error ?? relabelMutation.error

  const {
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<LookupEntryFormValues>({ formId: `lookup-entry:${lookup}:${entry?.id ?? 'new'}` }, {
    resolver: zodResolver(lookupEntryFormSchema),
    defaultValues: {
      id: String(entry?.id ?? suggestedId),
      descAr: entry?.descAr ?? '',
      desc: entry?.desc ?? '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (entry) {
        await relabelMutation.mutateAsync({
          lookup,
          id: entry.id,
          request: { desc: values.desc, descAr: values.descAr },
        })
        toast.success(t('lookup.form.toast-updated'))
      } else {
        await createMutation.mutateAsync({
          lookup,
          request: { id: Number(values.id), desc: values.desc, descAr: values.descAr },
        })
        toast.success(t('lookup.form.toast-created'))
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
      title={isEdit ? t('lookup.form.edit-title') : t('lookup.form.create-title')}
      description={
        isEdit
          ? t('lookup.form.edit-description')
          : t('lookup.form.create-description')
      }
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
            <Label k="common.action.cancel" />
          </Button>
          <Button type="submit" form="lookup-entry-form" loading={pending}>
            <Label k={isEdit ? 'lookup.form.save-edit' : 'lookup.form.add'} />
          </Button>
        </>
      }
    >
      <form id="lookup-entry-form" onSubmit={onSubmit} noValidate className="grid gap-4">
        <FormField labelKey="lookup.column.id" required error={errors.id?.message}>
          <Input
            id="lookup-entry-id"
            dir="ltr"
            inputMode="numeric"
            startIcon={<Hash className="size-4" aria-hidden />}
            invalid={Boolean(errors.id)}
            disabled={isEdit || pending}
            {...register('id')}
          />
        </FormField>
        <FormField labelKey="lookup.form.desc-ar" required error={errors.descAr?.message}>
          <Input
            id="lookup-entry-desc-ar"
            startIcon={<Languages className="size-4" aria-hidden />}
            invalid={Boolean(errors.descAr)}
            {...register('descAr')}
          />
        </FormField>
        <FormField labelKey="lookup.form.desc" required error={errors.desc?.message}>
          <Input
            id="lookup-entry-desc"
            dir="ltr"
            startIcon={<Languages className="size-4" aria-hidden />}
            invalid={Boolean(errors.desc)}
            {...register('desc')}
          />
        </FormField>
        {mutationError && (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(mutationError)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
