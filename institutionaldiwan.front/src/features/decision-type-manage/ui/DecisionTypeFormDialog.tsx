import { useMemo } from 'react'
import { Controller, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { type DecisionTypeResponse } from '@/entities/decision-type'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { DECISION_EFFECT, decisionEffectLabel } from '@/shared/config'
import { useDraftForm } from '@/shared/lib'
import { Button, Checkbox, Dialog, FormField, Input, Select } from '@/shared/ui'
import {
  useCreateDecisionTypeMutation,
  useUpdateDecisionTypeMutation,
} from '../api/decisionTypeMutations'
import { decisionTypeSchema, type DecisionTypeFormValues } from '../model/schemas'

interface DecisionTypeFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  decisionType: DecisionTypeResponse | null
}

const EFFECT_OPTIONS = [
  DECISION_EFFECT.Refer,
  DECISION_EFFECT.SendToArchive,
  DECISION_EFFECT.Join,
  DECISION_EFFECT.None,
].map((effectId) => ({ value: String(effectId), label: decisionEffectLabel(effectId) }))

function toValues(decisionType: DecisionTypeResponse | null): DecisionTypeFormValues {
  return {
    nameAr: decisionType?.nameAr ?? '',
    isAppealable: decisionType?.isAppealable ?? false,
    effectId: String(decisionType?.effectId ?? DECISION_EFFECT.None),
    destinationDepartmentId: decisionType?.destinationDepartmentId ?? '',
    documentTypeIds: decisionType?.documentTypeIds ?? [],
  }
}

/** Creates or edits a decision type. A referral without a destination is stored inactive. */
export function DecisionTypeFormDialog({
  open,
  onOpenChange,
  decisionType,
}: DecisionTypeFormDialogProps) {
  const createMutation = useCreateDecisionTypeMutation()
  const updateMutation = useUpdateDecisionTypeMutation(decisionType?.decisionTypeId ?? '')
  const departmentsQuery = useDepartmentOptions({ enabled: open })
  const documentTypesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes, { enabled: open })
  const pending = createMutation.isPending || updateMutation.isPending

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

  const {
    register,
    control,
    handleSubmit,
    setValue,
    formState: { errors },
    draft,
  } = useDraftForm<DecisionTypeFormValues>(
    { formId: decisionType ? `decision-type:${decisionType.decisionTypeId}` : 'decision-type:new', enabled: open },
    {
      resolver: zodResolver(decisionTypeSchema),
      defaultValues: toValues(decisionType),
    },
  )

  const effectId = Number(useWatch({ control, name: 'effectId' }))
  const selectedDocumentTypes = useWatch({ control, name: 'documentTypeIds' })
  const isReferral = effectId === DECISION_EFFECT.Refer

  const onSubmit = handleSubmit(async (values) => {
    const request = {
      nameAr: values.nameAr.trim(),
      isAppealable: values.isAppealable,
      effectId: Number(values.effectId),
      destinationDepartmentId:
        Number(values.effectId) === DECISION_EFFECT.Refer
          ? values.destinationDepartmentId || null
          : null,
      documentTypeIds: values.documentTypeIds,
    }
    try {
      if (decisionType) await updateMutation.mutateAsync(request)
      else await createMutation.mutateAsync(request)
      toast.success(decisionType ? 'تم تحديث نوع القرار.' : 'تم إنشاء نوع القرار.')
      draft.discard()
      onOpenChange(false)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={decisionType ? 'تعديل نوع القرار' : 'نوع قرار جديد'}
      description="الإحالة بلا قسم وجهة تُحفظ موقوفة حتى يُسمّى القسم وتُفعَّل. القرارات الصادرة تحتفظ بنسخة الأثر كما كانت عند إصدارها."
      size="lg"
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
            إلغاء
          </Button>
          <Button type="submit" form="decision-type-form" disabled={pending}>
            {pending ? 'جارٍ الحفظ…' : 'حفظ'}
          </Button>
        </>
      }
    >
      <form id="decision-type-form" className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <FormField label="الاسم" required error={errors.nameAr?.message} guideId="decisions.type-name">
          <Input {...register('nameAr')} invalid={Boolean(errors.nameAr)} />
        </FormField>
        <FormField label="الأثر" required error={errors.effectId?.message} guideId="decisions.type-effect">
          <Controller
            control={control}
            name="effectId"
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={(value) => {
                  field.onChange(value)
                  if (Number(value) !== DECISION_EFFECT.Refer) setValue('destinationDepartmentId', '')
                }}
                options={EFFECT_OPTIONS}
                invalid={Boolean(errors.effectId)}
              />
            )}
          />
        </FormField>
        {isReferral && (
          <FormField
            label="قسم الوجهة"
            error={errors.destinationDepartmentId?.message}
            hint="مطلوب قبل التفعيل. بدونه يُحفظ النوع موقوفًا."
          >
            <Controller
              control={control}
              name="destinationDepartmentId"
              render={({ field }) => (
                <Select
                  value={field.value || undefined}
                  onValueChange={field.onChange}
                  options={departmentOptions}
                  placeholder="اختر الديوان الوجهة"
                  invalid={Boolean(errors.destinationDepartmentId)}
                />
              )}
            />
          </FormField>
        )}
        {effectId === DECISION_EFFECT.Join && (
          <p className="text-sm text-muted">
            إصدار هذا النوع يوحّد الملف مع ملف رئيسي يختاره المُصدر. لا يتغيّر رقم الملف ولا مكانه.
          </p>
        )}
        <FormField label="قابل للاعتراض">
          <Controller
            control={control}
            name="isAppealable"
            render={({ field }) => (
              <Checkbox checked={field.value} onCheckedChange={field.onChange} label="يمكن الاعتراض على القرار" />
            )}
          />
        </FormField>
        <FormField label="أنواع الوثائق" required error={errors.documentTypeIds?.message}>
          <div className="flex flex-col gap-2">
            {(documentTypesQuery.data ?? []).map((entry) => (
              <Checkbox
                key={entry.id}
                checked={selectedDocumentTypes.includes(entry.id)}
                onCheckedChange={(checked) => {
                  const next = checked
                    ? [...selectedDocumentTypes, entry.id]
                    : selectedDocumentTypes.filter((id) => id !== entry.id)
                  setValue('documentTypeIds', next, { shouldValidate: true })
                }}
                label={entry.descAr}
              />
            ))}
          </div>
        </FormField>
      </form>
    </Dialog>
  )
}
