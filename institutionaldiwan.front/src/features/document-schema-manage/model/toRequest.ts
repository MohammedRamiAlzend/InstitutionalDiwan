import {
  FIELD_DATA_TYPE,
  FIELD_OPTION_SOURCE,
  TODAY_TOKEN,
  type FieldDataType,
  type FieldOption,
  type FieldOptionSource,
  type SchemaFieldResponse,
} from '@/entities/document-schema'
import type { FieldSettingsFormValues } from './schemas'
import type { FieldSettingsRequest } from './types'

function textOrNull(value: string): string | null {
  return value.trim() || null
}

function dateLimit(date: string, today: boolean): string | null {
  return today ? TODAY_TOKEN : date || null
}

/** Empty form strings become omitted members so the backend sees "not set", never an empty value. */
export function toSettingsRequest(
  values: FieldSettingsFormValues,
  dataType: FieldDataType,
): FieldSettingsRequest {
  const isChoice = dataType === FIELD_DATA_TYPE.Select || dataType === FIELD_DATA_TYPE.MultiSelect
  const source = (isChoice ? Number(values.optionSource) : FIELD_OPTION_SOURCE.Static) as FieldOptionSource
  const isStatic = source === FIELD_OPTION_SOURCE.Static
  const isText = dataType === FIELD_DATA_TYPE.Text || dataType === FIELD_DATA_TYPE.LongText
  const isDate = dataType === FIELD_DATA_TYPE.Date

  const request: FieldSettingsRequest = {
    labelAr: values.labelAr.trim(),
    isRequired: values.isRequired,
    displayOrder: Number(values.displayOrder),
    behavior: {
      defaultValue: !isStatic
        ? null
        : isDate && values.defaultToday
          ? TODAY_TOKEN
          : textOrNull(values.defaultValue),
      isReadOnly: isStatic && values.isReadOnly,
      isFilterable: values.isFilterable,
      showInTable: values.showInTable,
      placeholderAr: textOrNull(values.placeholderAr),
      helpTextAr: textOrNull(values.helpTextAr),
      minLength: isText && values.minLength ? Number(values.minLength) : null,
      minDate: isDate ? dateLimit(values.minDate, values.minDateToday) : null,
      maxDate: isDate ? dateLimit(values.maxDate, values.maxDateToday) : null,
      visibleWhenFieldKey: values.visibleWhenFieldKey || null,
      visibleWhenValue: values.visibleWhenFieldKey ? values.visibleWhenValue || null : null,
      isHalfWidth: values.isHalfWidth,
      sectionTitleAr: textOrNull(values.sectionTitleAr),
      optionSource: source,
      lookupCatalog: source === FIELD_OPTION_SOURCE.Lookup ? values.lookupCatalog : null,
    },
  }
  if (values.regexPattern.trim()) {
    request.regexPattern = values.regexPattern
    request.regexErrorMessageAr = values.regexErrorMessageAr.trim()
  }
  if (values.maxLength) request.maxLength = Number(values.maxLength)
  if (values.minNumber) request.minNumber = Number(values.minNumber)
  if (values.maxNumber) request.maxNumber = Number(values.maxNumber)
  if (isChoice && isStatic) {
    request.options = values.options.map((option) => ({
      key: option.key.trim(),
      labelAr: option.labelAr.trim(),
    }))
  }
  return request
}

export function defaultSettingsValues(
  field?: SchemaFieldResponse,
  nextOrder = 0,
): FieldSettingsFormValues {
  const behavior = field?.behavior
  const isStatic = (behavior?.optionSource ?? FIELD_OPTION_SOURCE.Static) === FIELD_OPTION_SOURCE.Static
  const fixedDate = (value: string | null | undefined) =>
    value && value !== TODAY_TOKEN ? value.slice(0, 10) : ''

  return {
    labelAr: field?.labelAr ?? '',
    isRequired: field?.isRequired ?? false,
    displayOrder: String(field?.displayOrder ?? nextOrder),
    regexPattern: field?.regexPattern ?? '',
    regexErrorMessageAr: field?.regexErrorMessageAr ?? '',
    maxLength: field?.maxLength?.toString() ?? '',
    minNumber: field?.minNumber?.toString() ?? '',
    maxNumber: field?.maxNumber?.toString() ?? '',
    // A sourced field's list comes from its source and is never sent back as typed options.
    options: isStatic
      ? (field?.options.map((option) => ({ key: option.key, labelAr: option.labelAr })) ?? [])
      : [],
    defaultValue: behavior?.defaultValue === TODAY_TOKEN ? '' : (behavior?.defaultValue ?? ''),
    defaultToday: behavior?.defaultValue === TODAY_TOKEN,
    isReadOnly: behavior?.isReadOnly ?? false,
    isFilterable: behavior?.isFilterable ?? false,
    showInTable: behavior?.showInTable ?? false,
    placeholderAr: behavior?.placeholderAr ?? '',
    helpTextAr: behavior?.helpTextAr ?? '',
    minLength: behavior?.minLength?.toString() ?? '',
    minDate: fixedDate(behavior?.minDate),
    maxDate: fixedDate(behavior?.maxDate),
    minDateToday: behavior?.minDate === TODAY_TOKEN,
    maxDateToday: behavior?.maxDate === TODAY_TOKEN,
    visibleWhenFieldKey: behavior?.visibleWhenFieldKey ?? '',
    visibleWhenValue: behavior?.visibleWhenValue ?? '',
    isHalfWidth: behavior?.isHalfWidth ?? false,
    sectionTitleAr: behavior?.sectionTitleAr ?? '',
    optionSource: String(behavior?.optionSource ?? FIELD_OPTION_SOURCE.Static),
    lookupCatalog: behavior?.lookupCatalog ?? '',
  }
}

/** A bare field of the chosen type, so the editor's default-value input is the real control. */
export function previewField(dataType: FieldDataType, options: readonly FieldOption[]): SchemaFieldResponse {
  return {
    documentFieldId: '',
    key: 'default',
    dataType,
    labelAr: 'القيمة الافتراضية',
    isRequired: false,
    displayOrder: 0,
    regexPattern: null,
    regexErrorMessageAr: null,
    minNumber: null,
    maxNumber: null,
    maxLength: null,
    options: options.filter((option) => option.key && option.labelAr),
    behavior: {
      defaultValue: null,
      isReadOnly: false,
      isFilterable: false,
      showInTable: false,
      placeholderAr: null,
      helpTextAr: null,
      minLength: null,
      minDate: null,
      maxDate: null,
      visibleWhenFieldKey: null,
      visibleWhenValue: null,
      isHalfWidth: false,
      sectionTitleAr: null,
      optionSource: FIELD_OPTION_SOURCE.Static,
      lookupCatalog: null,
    },
  }
}
