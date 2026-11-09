import { useMemo, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { LOOKUP_SLUGS, useLookupEntries, usePreselectLookupDefault } from '@/entities/lookup'
import { PersonNamesInput } from '@/entities/person'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Select } from '@/shared/ui'
import { useAddDocumentPersonMutation } from '../api/documentPersonMutations'
import { addDocumentPersonsSchema, type AddDocumentPersonsFormValues } from '../model/schemas'

interface AddDocumentPersonsDialogProps {
  documentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Names one or more people on a document, all in the chosen role. */
export function AddDocumentPersonsDialog({ documentId, open, onOpenChange }: AddDocumentPersonsDialogProps) {
  const rolesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentPersonRoles)
  const addMutation = useAddDocumentPersonMutation()
  const [failure, setFailure] = useState<string | null>(null)

  const {
    control,
    getValues,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<AddDocumentPersonsFormValues>({
    resolver: zodResolver(addDocumentPersonsSchema),
    defaultValues: { persons: [], roleId: '' },
  })

  usePreselectLookupDefault(rolesQuery.data, getValues('roleId'), (value) => setValue('roleId', value))

  const roleOptions = useMemo(
    () => (rolesQuery.data ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    [rolesQuery.data],
  )

  const onSubmit = handleSubmit(async (values) => {
    setFailure(null)

    for (const [index, person] of values.persons.entries()) {
      try {
        await addMutation.mutateAsync({
          documentId,
          request: {
            ...(person.personId ? { personId: person.personId } : { fullName: person.fullName }),
            roleId: Number(values.roleId),
          },
        })
      } catch (error) {
        // Those before it are saved; keep only what still needs attention in the form.
        setValue('persons', values.persons.slice(index))
        setFailure(`تعذّرت إضافة «${person.fullName}»: ${toArabicErrorMessage(error)}`)
        return
      }
    }

    toast.success(values.persons.length > 1 ? 'تمت إضافة الأشخاص.' : 'تمت إضافة الشخص.')
    onOpenChange(false)
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="إضافة أشخاص إلى الوثيقة"
      description="ابحث عن شخص مسجّل واختره، أو اكتب اسمًا جديدًا. يمكن إضافة أكثر من شخص."
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={addMutation.isPending}>
            إلغاء
          </Button>
          <Button type="submit" form="document-persons-add-form" loading={addMutation.isPending}>
            إضافة
          </Button>
        </>
      }
    >
      <form id="document-persons-add-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField label="الأسماء" required error={errors.persons?.message}>
          <Controller
            control={control}
            name="persons"
            render={({ field }) => (
              <PersonNamesInput
                value={field.value}
                onChange={field.onChange}
                disabled={addMutation.isPending}
                invalid={Boolean(errors.persons)}
              />
            )}
          />
        </FormField>

        <FormField label="الصفة" required error={errors.roleId?.message}>
          <Controller
            control={control}
            name="roleId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={roleOptions}
                disabled={addMutation.isPending || rolesQuery.isLoading}
                invalid={Boolean(errors.roleId)}
                placeholder="اختر الصفة…"
                aria-label="الصفة"
              />
            )}
          />
        </FormField>

        {failure && (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {failure}
          </p>
        )}
      </form>
    </Dialog>
  )
}
