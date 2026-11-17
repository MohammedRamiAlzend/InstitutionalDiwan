/** `GET /api/ui-labels` (any authenticated caller): only the changed labels, plus their version. */
export interface UiLabelOverridesResponse {
  /** Newest override's `UpdatedAt` ticks, or 0 when nothing is overridden; changes on every save/reset. */
  version: number
  overrides: Record<string, string>
}