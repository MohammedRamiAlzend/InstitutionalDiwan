import { FIELD_DATA_TYPE, type FieldDataType, type SchemaFieldResponse } from '@/entities/document-schema'
import type { FieldSettingsFormValues } from './schemas'
import type { FieldSettingsRequest } from './types'

/** Empty form strings become omitted members so the backend sees "not set", never an empty value. */
export function toSettingsRequest(
  values: FieldSettingsFormValues,
  dataType: FieldDataType,
): FieldSettingsRequest {
  const request: FieldSettingsRequest = {
    labelAr: values.labelAr.trim(),
    isRequired: values.isRequired,
    displayOrder: Number(values.displayOrder),
  }
  if (values.regexPattern.trim()) {
    request.regexPattern = values.regexPattern
    request.regexErrorMessageAr = values.regexErrorMessageAr.trim()
  }
  if (values.maxLength) request.maxLength = Number(values.maxLength)
  if (values.minNumber) request.minNumber = Number(values.minNumber)
  if (values.maxNumber) request.maxNumber = Number(values.maxNumber)
  if (dataType === FIELD_DATA_TYPE.Select) {
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
  return {
    labelAr: field?.labelAr ?? '',
    isRequired: field?.isRequired ?? false,
    displayOrder: String(field?.displayOrder ?? nextOrder),
    regexPattern: field?.regexPattern ?? '',
    regexErrorMessageAr: field?.regexErrorMessageAr ?? '',
    maxLength: field?.maxLength?.toString() ?? '',
    minNumber: field?.minNumber?.toString() ?? '',
    maxNumber: field?.maxNumber?.toString() ?? '',
    options: field?.options.map((option) => ({ key: option.key, labelAr: option.labelAr })) ?? [],
  }
}
