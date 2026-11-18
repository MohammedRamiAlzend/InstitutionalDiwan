import { useMemo } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import type { DocumentPersonResponse } from '@/entities/person'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { Button, Dialog, FormField, Label, Select } from '@/shared/ui'
import { useChangeDocumentPersonRoleMutation } from '../api/documentPersonMutations'
import {
  changeDocumentPersonRoleSchema,
  type ChangeDocumentPersonRoleFormValues,
} from '../model/schemas'

interface ChangeDocumentPersonRoleDialogProps {
  documentId: string
  person: DocumentPersonResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Changes the role one person holds on the document. */
export function ChangeDocumentPersonRoleDialog({
  documentId,
  person,
  open,
  onOpenChange,
}: ChangeDocumentPersonRoleDialogProps) {
  const rolesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentPersonRoles)
  const changeMutation = useChangeDocumentPersonRoleMutation()

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<ChangeDocumentPersonRoleFormValues>({
    resolver: zodResolver(changeDocumentPersonRoleSchema),
    defaultValues: { roleId: String(person.roleId) },
  })

  const roleOptions = useMemo(
    () => (rolesQuery.data ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    [rolesQuery.data],
  )

  const onSubmit = handleSubmit(async (values) => {
    try {
      await changeMutation.mutateAsync({
        documentId,
        documentPersonId: person.documentPersonId,
        roleId: Number(values.roleId),
      })
      toast.success(t('persons.change-role.toast'))
      onOpenChange(false)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('persons.change-role.title')}
      description={t('persons.change-role.description', { name: person.displayName })}
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={changeMutation.isPending}>
            <Label k="common.action.cancel" />
          </Button>
          <Button type="submit" form="document-person-role-form" loading={changeMutation.isPending}>
            <Label k="common.action.save" />
          </Button>
        </>
      }
    >
      <form id="document-person-role-form" onSubmit={onSubmit} noValidate>
        <FormField labelKey="persons.field-role" required error={errors.roleId?.message}>
          <Controller
            control={control}
            name="roleId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={roleOptions}
                disabled={changeMutation.isPending || rolesQuery.isLoading}
                invalid={Boolean(errors.roleId)}
                placeholder={t('persons.role-placeholder')}
                aria-label={t('persons.role-aria')}
              />
            )}
          />
        </FormField>
      </form>
    </Dialog>
  )
}
