import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Hash } from 'lucide-react'
import { toast } from 'sonner'
import type { RoleResponse } from '@/entities/role'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, FormField, Input, Textarea } from '@/shared/ui'
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
  } = useForm<RoleFormValues>({
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
        toast.success('تم تحديث بيانات الدور.')
      } else {
        await createMutation.mutateAsync({
          code: values.code,
          nameAr: values.nameAr,
          nameEn,
          description,
        })
        toast.success('تم إنشاء الدور بنجاح.')
      }
      onOpenChange(false)
    } catch {
      // The mutation error is rendered below in Arabic.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={isEdit ? 'تعديل بيانات الدور' : 'دور جديد'}
      description={
        isEdit
          ? 'حدّث الاسم والوصف. لا يمكن تغيير رمز الدور بعد إنشائه.'
          : 'أنشئ دورًا مخصصًا ثم امنحه الصلاحيات من مصفوفة الصلاحيات.'
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
            إلغاء
          </Button>
          <Button type="submit" form="role-form" loading={pending}>
            {isEdit ? 'حفظ التعديلات' : 'إنشاء الدور'}
          </Button>
        </>
      }
    >
      <form id="role-form" onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <FormField label="رمز الدور" required error={errors.code?.message}>
          <Input
            id="role-code"
            placeholder="مثال: FINANCE_EDITOR"
            dir="ltr"
            disabled={isEdit}
            startIcon={<Hash className="size-4" aria-hidden />}
            invalid={Boolean(errors.code)}
            {...register('code')}
          />
        </FormField>

        <FormField label="الاسم العربي" required error={errors.nameAr?.message}>
          <Input
            id="role-name-ar"
            placeholder="مثال: محرر مالي"
            invalid={Boolean(errors.nameAr)}
            {...register('nameAr')}
          />
        </FormField>

        <FormField label="الاسم الإنجليزي" error={errors.nameEn?.message}>
          <Input
            id="role-name-en"
            placeholder="Finance Editor"
            dir="ltr"
            invalid={Boolean(errors.nameEn)}
            {...register('nameEn')}
          />
        </FormField>

        <FormField label="الوصف" error={errors.description?.message}>
          <Textarea
            id="role-description"
            rows={3}
            placeholder="استخدام الدور ونطاقه…"
            invalid={Boolean(errors.description)}
            {...register('description')}
          />
        </FormField>

        {isEdit && role.isSystemRole && (
          <p role="status" className="rounded-lg bg-warning/10 px-3 py-2 text-xs text-warning">
            هذا دور معرّف من النظام؛ لا يمكن إيقافه أو حذفه، لكن يمكن تعديل اسمه ووصفه وصلاحياته.
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
