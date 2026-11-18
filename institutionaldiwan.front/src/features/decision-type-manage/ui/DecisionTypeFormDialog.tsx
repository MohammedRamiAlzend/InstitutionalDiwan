import { useMemo } from 'react'
import { Controller, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { type DecisionTypeResponse } from '@/entities/decision-type'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { toArabicErrorMessage } from '@/shared/api'
import { DECISION_EFFECT, decisionEffectLabel } from '@/shared/config'
import { useDraftForm, t } from '@/shared/lib'
import { Button, Checkbox, Dialog, FormField, Input, Label, Select } from '@/shared/ui'
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

const EFFECT_IDS = [
  DECISION_EFFECT.Refer,
  DECISION_EFFECT.SendToArchive,
  DECISION_EFFECT.Join,
  DECISION_EFFECT.None,
]

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
  const effectOptions = EFFECT_IDS.map((effectId) => ({
    value: String(effectId),
    label: decisionEffectLabel(effectId),
  }))

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
      toast.success(decisionType ? t('decisions.type.toast.updated') : t('decisions.type.toast.created'))
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
      title={decisionType ? t('decisions.type.title-edit') : t('decisions.type.title-new')}
      description={t('decisions.type.description')}
      size="lg"
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
            <Label k="common.action.cancel" />
          </Button>
          <Button type="submit" form="decision-type-form" disabled={pending}>
            {pending ? t('decisions.type.submitting') : <Label k="common.action.save" />}
          </Button>
        </>
      }
    >
      <form id="decision-type-form" className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <FormField labelKey="decisions.type.field-name" required error={errors.nameAr?.message} guideId="decisions.type-name">
          <Input {...register('nameAr')} invalid={Boolean(errors.nameAr)} />
        </FormField>
        <FormField labelKey="decisions.type.field-effect" required error={errors.effectId?.message} guideId="decisions.type-effect">
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
                options={effectOptions}
                invalid={Boolean(errors.effectId)}
              />
            )}
          />
        </FormField>
        {isReferral && (
          <FormField
            labelKey="decisions.type.field-destination"
            error={errors.destinationDepartmentId?.message}
            hint={t('decisions.type.destination-hint')}
          >
            <Controller
              control={control}
              name="destinationDepartmentId"
              render={({ field }) => (
                <Select
                  value={field.value || undefined}
                  onValueChange={field.onChange}
                  options={departmentOptions}
                  placeholder={t('decisions.type.destination-placeholder')}
                  invalid={Boolean(errors.destinationDepartmentId)}
                />
              )}
            />
          </FormField>
        )}
        {effectId === DECISION_EFFECT.Join && (
          <p className="text-sm text-muted">
            <Label k="decisions.type.join-hint" />
          </p>
        )}
        <FormField labelKey="decisions.type.field-appealable">
          <Controller
            control={control}
            name="isAppealable"
            render={({ field }) => (
              <Checkbox checked={field.value} onCheckedChange={field.onChange} label={t('decisions.type.appealable-checkbox')} />
            )}
          />
        </FormField>
        <FormField labelKey="decisions.type.field-document-types" required error={errors.documentTypeIds?.message}>
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
