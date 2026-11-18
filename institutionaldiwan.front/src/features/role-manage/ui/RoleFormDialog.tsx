import { t, useDraftForm } from '@/shared/lib'
import { zodResolver } from '@hookform/resolvers/zod'
import { Hash } from 'lucide-react'
import { toast } from 'sonner'
import type { RoleResponse } from '@/entities/role'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Label, Textarea } from '@/shared/ui'
import { roleFormSchema, type RoleFormValues } from '../model/schemas'
import { useCreateRoleMutation, useUpdateRoleMutation } from '../api/roleMutations'

interface RoleFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** `null` creates a new role; a role edits the existing one. */
  role: RoleResponse | null
}

/** Create/edit form for `POST /api/roles` and `PUT /api/roles/{id}`. */
export function RoleFormDialog({ open, onOpenChange, role }: RoleFormDialogProps) {
  const isEdit = role !== null
  const createMutation = useCreateRoleMutation()
  const updateMutation = useUpdateRoleMutation()
  const pending = createMutation.isPending || updateMutation.isPending
  const mutationError = createMutation.error ?? updateMutation.error

  const {
    register,
    handleSubmit,
    formState: { errors },
    draft,
  } = useDraftForm<RoleFormValues>({ formId: `role-form:${role?.roleId ?? 'new'}` }, {
    resolver: zodResolver(roleFormSchema),
    defaultValues: {
      code: role?.code ?? '',
      nameAr: role?.nameAr ?? '',
      nameEn: role?.nameEn ?? '',
      description: role?.description ?? '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    const nameEn = values.nameEn || null
    const description = values.description || null

    try {
      if (role) {
        await updateMutation.mutateAsync({
          roleId: role.roleId,
          request: { nameAr: values.nameAr, nameEn, description },
        })
        toast.success(t('role.form.toast-updated'))
      } else {
        await createMutation.mutateAsync({
          code: values.code,
          nameAr: values.nameAr,
          nameEn,
          description,
        })
        toast.success(t('role.form.toast-created'))
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
      title={isEdit ? t('role.form.edit-title') : t('role.create')}
      description={
        isEdit
          ? t('role.form.edit-description')
          : t('role.form.create-description')
      }
      size="md"
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
          <Button type="submit" form="role-form" loading={pending}>
            {isEdit ? <Label k="role.form.save" /> : <Label k="role.form.submit" />}
          </Button>
        </>
      }
    >
      <form id="role-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField labelKey="role.form.code" required error={errors.code?.message}>
          <Input
            id="role-code"
            placeholder={t('role.form.code-placeholder')}
            dir="ltr"
            disabled={isEdit}
            startIcon={<Hash className="size-4" aria-hidden />}
            invalid={Boolean(errors.code)}
            {...register('code')}
          />
        </FormField>

        <FormField labelKey="role.form.name-ar" required error={errors.nameAr?.message}>
          <Input
            id="role-name-ar"
            placeholder={t('role.form.name-ar-placeholder')}
            invalid={Boolean(errors.nameAr)}
            {...register('nameAr')}
          />
        </FormField>

        <FormField labelKey="role.form.name-en" error={errors.nameEn?.message}>
          <Input
            id="role-name-en"
            placeholder="Finance Editor"
            dir="ltr"
            invalid={Boolean(errors.nameEn)}
            {...register('nameEn')}
          />
        </FormField>

        <FormField labelKey="role.form.description" error={errors.description?.message}>
          <Textarea
            id="role-description"
            rows={3}
            placeholder={t('role.form.description-placeholder')}
            invalid={Boolean(errors.description)}
            {...register('description')}
          />
        </FormField>

        {isEdit && role.isSystemRole && (
          <p role="status" className="rounded-lg bg-warning/10 px-3 py-2 text-xs text-warning">
            {t('role.form.system-note')}
          </p>
        )}

        {mutationError && (
          <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
            {toArabicErrorMessage(mutationError)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
