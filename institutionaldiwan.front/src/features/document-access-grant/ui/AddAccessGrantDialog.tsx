import { useMemo, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Search } from 'lucide-react'
import { toast } from 'sonner'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { useUserOptions } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { useDebouncedValue } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Select, Textarea } from '@/shared/ui'
import { useGrantDocumentAccessMutation } from '../api/accessGrantMutations'
import { grantAccessSchema, type GrantAccessFormValues } from '../model/schemas'

interface AddAccessGrantDialogProps {
  documentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Grants one user an access level on this document; the caller is recorded as grantor. */
export function AddAccessGrantDialog({
  documentId,
  open,
  onOpenChange,
}: AddAccessGrantDialogProps) {
  const mutation = useGrantDocumentAccessMutation(documentId)
  const levelsQuery = useLookupEntries(LOOKUP_SLUGS.AccessLevels)

  const [userSearch, setUserSearch] = useState('')
  const debouncedSearch = useDebouncedValue(userSearch)
  /** Keeps the chosen label selectable after the search term changes. */
  const [selectedUser, setSelectedUser] = useState<{ value: string; label: string } | null>(
    null,
  )

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<GrantAccessFormValues>({
    resolver: zodResolver(grantAccessSchema),
    defaultValues: { userId: '', accessLevelId: '', reason: '', expiresAt: '' },
  })

  const usersQuery = useUserOptions(
    {
      page: 1,
      pageSize: 20,
      search: debouncedSearch.trim() || undefined,
    },
    { enabled: open },
  )

  const userOptions = useMemo(() => {
    const active = (usersQuery.data?.items ?? [])
      .filter((user) => user.isActive)
      .map((user) => ({ value: user.userId, label: user.username }))

    if (selectedUser && !active.some((option) => option.value === selectedUser.value)) {
      return [selectedUser, ...active]
    }
    return active
  }, [usersQuery.data, selectedUser])

  const levelOptions = useMemo(
    () =>
      (levelsQuery.data ?? []).map((level) => ({
        value: String(level.id),
        label: level.descAr,
      })),
    [levelsQuery.data],
  )

  const onSubmit = handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        userId: values.userId,
        accessLevelId: Number(values.accessLevelId),
        reason: values.reason.trim() || null,
        expiresAt: values.expiresAt || null,
      })
      toast.success('تم منح الوصول إلى الوثيقة.')
      onOpenChange(false)
    } catch {
      // The mutation error is rendered in the form-level banner below.
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="منح وصول إلى الوثيقة"
      description="اختر المستخدم ومستوى الوصول. يمكنك تحديد تاريخ انتهاء اختياري، ويُسجَّل اسمك كمانح للتصريح."
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
          <Button type="submit" form="document-grant-form" loading={mutation.isPending}>
            منح الوصول
          </Button>
        </>
      }
    >
      <form
        id="document-grant-form"
        onSubmit={onSubmit}
        noValidate
        className="grid gap-4 sm:grid-cols-2"
      >
        <FormField
          label="البحث في المستخدمين"
          className="sm:col-span-2"
          htmlFor="grant-user-search"
        >
          <Input
            id="grant-user-search"
            placeholder="ابحث باسم المستخدم…"
            startIcon={<Search className="size-4" aria-hidden />}
            value={userSearch}
            onChange={(event) => setUserSearch(event.target.value)}
            disabled={mutation.isPending}
          />
        </FormField>

        <FormField
          label="المستخدم"
          required
          error={errors.userId?.message}
          className="sm:col-span-2"
        >
          <Controller
            control={control}
            name="userId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={(value) => {
                  field.onChange(value)
                  setSelectedUser(
                    userOptions.find((option) => option.value === value) ?? null,
                  )
                }}
                options={userOptions}
                disabled={mutation.isPending || usersQuery.isLoading}
                invalid={Boolean(errors.userId)}
                placeholder={
                  usersQuery.isLoading ? 'جارٍ التحميل…' : 'اختر المستخدم…'
                }
                aria-label="المستخدم"
              />
            )}
          />
          {usersQuery.isSuccess && userOptions.length === 0 && (
            <p className="text-xs text-muted">
              لا يوجد مستخدمون مطابقون. تُدار حسابات المستخدمين من شاشة «المستخدمون».
            </p>
          )}
        </FormField>

        <FormField
          label="مستوى الوصول"
          required
          error={errors.accessLevelId?.message}
        >
          <Controller
            control={control}
            name="accessLevelId"
            render={({ field }) => (
              <Select
                value={field.value || undefined}
                onValueChange={field.onChange}
                options={levelOptions}
                disabled={mutation.isPending || levelsQuery.isLoading}
                invalid={Boolean(errors.accessLevelId)}
                placeholder="اختر المستوى…"
                aria-label="مستوى الوصول"
              />
            )}
          />
        </FormField>

        <FormField
          label="تاريخ الانتهاء (اختياري)"
          error={errors.expiresAt?.message}
          htmlFor="grant-expires-at"
        >
          <Input
            id="grant-expires-at"
            type="date"
            dir="ltr"
            invalid={Boolean(errors.expiresAt)}
            {...register('expiresAt')}
          />
        </FormField>

        <FormField
          label="السبب"
          error={errors.reason?.message}
          className="sm:col-span-2"
        >
          <Textarea
            rows={2}
            placeholder="مثال: مراجعة قانونية للعقد المرفق."
            invalid={Boolean(errors.reason)}
            {...register('reason')}
          />
        </FormField>

        {mutation.error && (
          <p
            role="alert"
            className="sm:col-span-2 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            {toArabicErrorMessage(mutation.error)}
          </p>
        )}
      </form>
    </Dialog>
  )
}
