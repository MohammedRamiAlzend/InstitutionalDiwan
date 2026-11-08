import type { ReactNode } from 'react'
import { Controller, useWatch, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form'
import {
  FIELD_DATA_TYPE,
  FIELD_OPTION_SOURCE,
  FieldValueControl,
  type FieldDataType,
  type FieldOption,
  type SchemaFieldResponse,
} from '@/entities/document-schema'
import { Checkbox, FormField, Input, Select, Textarea } from '@/shared/ui'
import type { FieldSettingsFormValues } from '../model/schemas'
import { previewField } from '../model/toRequest'

interface FieldBehaviorSectionProps {
  control: Control<FieldSettingsFormValues>
  register: UseFormRegister<FieldSettingsFormValues>
  errors: FieldErrors<FieldSettingsFormValues>
  dataType: FieldDataType | undefined
  /** The draft's other fields, candidates for a visibility condition. */
  otherFields: readonly SchemaFieldResponse[]
}

const NO_CONDITION = '__none__'

const YES_NO = [
  { value: 'true', label: 'نعم' },
  { value: 'false', label: 'لا' },
]

function isConditionController(field: SchemaFieldResponse): boolean {
  return (
    field.dataType === FIELD_DATA_TYPE.Boolean ||
    field.dataType === FIELD_DATA_TYPE.Select ||
    field.dataType === FIELD_DATA_TYPE.MultiSelect
  )
}

function Group({ title, children }: { title: string; children: ReactNode }) {
  return (
    <fieldset className="flex flex-col gap-4 rounded-lg border border-border p-3">
      <legend className="px-1 text-xs font-semibold text-muted">{title}</legend>
      {children}
    </fieldset>
  )
}

/** The form behavior of a field: default and read-only, hints, limits, condition, table use and layout. */
export function FieldBehaviorSection({
  control,
  register,
  errors,
  dataType,
  otherFields,
}: FieldBehaviorSectionProps) {
  const values = useWatch({ control })
  const isText = dataType === FIELD_DATA_TYPE.Text || dataType === FIELD_DATA_TYPE.LongText
  const isDate = dataType === FIELD_DATA_TYPE.Date
  const isChoice = dataType === FIELD_DATA_TYPE.Select || dataType === FIELD_DATA_TYPE.MultiSelect
  const sourced = isChoice && values.optionSource !== String(FIELD_OPTION_SOURCE.Static)

  const controllers = otherFields.filter(isConditionController)
  const controller = controllers.find((field) => field.key === values.visibleWhenFieldKey)
  const controllerValues =
    controller?.dataType === FIELD_DATA_TYPE.Boolean
      ? YES_NO
      : (controller?.options ?? []).map((option) => ({ value: option.key, label: option.labelAr }))

  const draftOptions = (values.options ?? []) as FieldOption[]

  return (
    <>
      <Group title="القيمة والإدخال">
        {dataType !== undefined && !sourced && (
          <FormField label="القيمة الافتراضية" error={errors.defaultValue?.message}>
            <div className="flex flex-col gap-2">
              {!values.defaultToday && (
                <Controller
                  control={control}
                  name="defaultValue"
                  render={({ field }) => (
                    <FieldValueControl
                      field={previewField(dataType, draftOptions)}
                      value={field.value}
                      invalid={Boolean(errors.defaultValue)}
                      onChange={field.onChange}
                    />
                  )}
                />
              )}
              {isDate && (
                <Controller
                  control={control}
                  name="defaultToday"
                  render={({ field }) => (
                    <Checkbox checked={field.value} onCheckedChange={field.onChange} label="تاريخ اليوم عند الإدخال" />
                  )}
                />
              )}
            </div>
          </FormField>
        )}

        {!sourced && (
          <Controller
            control={control}
            name="isReadOnly"
            render={({ field }) => (
              <Checkbox
                checked={field.value}
                onCheckedChange={field.onChange}
                label="للقراءة فقط"
                description="يُعرض بالقيمة الافتراضية ولا يُعدَّل، ويكتبها النظام بنفسه."
              />
            )}
          />
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label="نص توضيحي داخل الحقل" error={errors.placeholderAr?.message}>
            <Input invalid={Boolean(errors.placeholderAr)} {...register('placeholderAr')} />
          </FormField>
          {isText && (
            <FormField label="الحد الأدنى للطول" error={errors.minLength?.message}>
              <Input dir="ltr" inputMode="numeric" invalid={Boolean(errors.minLength)} {...register('minLength')} />
            </FormField>
          )}
        </div>
        <FormField label="نص مساعدة تحت الحقل" error={errors.helpTextAr?.message}>
          <Textarea rows={2} invalid={Boolean(errors.helpTextAr)} {...register('helpTextAr')} />
        </FormField>

        {isDate && (
          <div className="grid gap-4 sm:grid-cols-2">
            {(
              [
                ['minDate', 'minDateToday', 'أقدم تاريخ مسموح'],
                ['maxDate', 'maxDateToday', 'أحدث تاريخ مسموح'],
              ] as const
            ).map(([name, todayName, label]) => (
              <FormField key={name} label={label} error={errors[name]?.message}>
                <div className="flex flex-col gap-2">
                  {!values[todayName] && (
                    <Input dir="ltr" type="date" invalid={Boolean(errors[name])} {...register(name)} />
                  )}
                  <Controller
                    control={control}
                    name={todayName}
                    render={({ field }) => (
                      <Checkbox checked={field.value} onCheckedChange={field.onChange} label="تاريخ اليوم" />
                    )}
                  />
                </div>
              </FormField>
            ))}
          </div>
        )}
      </Group>

      <Group title="إظهار الحقل بشرط">
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label="يظهر عندما يكون الحقل" hint={controllers.length === 0 ? 'أضف حقل نعم/لا أو قائمة اختيار أولًا.' : undefined}>
            <Controller
              control={control}
              name="visibleWhenFieldKey"
              render={({ field }) => (
                <Select
                  aria-label="الحقل الذي يعتمد عليه الإظهار"
                  value={field.value || NO_CONDITION}
                  onValueChange={(next) => field.onChange(next === NO_CONDITION ? '' : next)}
                  options={[
                    { value: NO_CONDITION, label: 'يظهر دائمًا' },
                    ...controllers.map((entry) => ({ value: entry.key, label: entry.labelAr })),
                  ]}
                />
              )}
            />
          </FormField>
          {controller && (
            <FormField label="يساوي" required error={errors.visibleWhenValue?.message}>
              <Controller
                control={control}
                name="visibleWhenValue"
                render={({ field }) => (
                  <Select
                    aria-label="قيمة الشرط"
                    value={field.value || undefined}
                    onValueChange={field.onChange}
                    options={controllerValues}
                    invalid={Boolean(errors.visibleWhenValue)}
                  />
                )}
              />
            </FormField>
          )}
        </div>
      </Group>

      <Group title="الجداول والتخطيط">
        <Controller
          control={control}
          name="isFilterable"
          render={({ field }) => (
            <Checkbox
              checked={field.value}
              onCheckedChange={field.onChange}
              label="قابل للتصفية"
              description="يظهر في شريط مرشحات سجل الوثائق والأرشيف."
            />
          )}
        />
        <Controller
          control={control}
          name="showInTable"
          render={({ field }) => (
            <Checkbox
              checked={field.value}
              onCheckedChange={field.onChange}
              label="عمود في سجل الوثائق"
              description="تظهر قيمته عمودًا عند اختيار نوع الوثيقة."
            />
          )}
        />
        <Controller
          control={control}
          name="isHalfWidth"
          render={({ field }) => (
            <Checkbox checked={field.value} onCheckedChange={field.onChange} label="نصف عرض النموذج" />
          )}
        />
        <FormField
          label="عنوان القسم"
          hint="يبدأ قسمًا جديدًا عند أول حقل يحمل هذا العنوان."
          error={errors.sectionTitleAr?.message}
        >
          <Input invalid={Boolean(errors.sectionTitleAr)} {...register('sectionTitleAr')} />
        </FormField>
      </Group>
    </>
  )
}
