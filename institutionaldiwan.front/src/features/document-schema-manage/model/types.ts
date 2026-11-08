import type { FieldBehavior, FieldDataType, FieldOption } from '@/entities/document-schema'

/** `DocumentFieldSettingsRequest`. */
export interface FieldSettingsRequest {
  labelAr: string
  isRequired: boolean
  displayOrder: number
  regexPattern?: string
  regexErrorMessageAr?: string
  minNumber?: number
  maxNumber?: number
  maxLength?: number
  options?: FieldOption[]
  behavior: FieldBehavior
}

/** `CreateDocumentFieldRequest`. */
export interface CreateFieldRequest {
  key: string
  dataType: FieldDataType
}

/** `AddSchemaFieldRequest`. */
export interface AddSchemaFieldRequest {
  documentFieldId: string
  settings: FieldSettingsRequest
}
