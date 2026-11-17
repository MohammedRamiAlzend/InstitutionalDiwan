/** `PUT /api/ui-labels/{key}` body — the new text for one label key. */
export interface SetUiLabelOverrideRequest {
  value: string
}

/** Variables for the upsert mutation: which key, and its new value. */
export interface SetUiLabelOverrideVariables {
  key: string
  value: string
}