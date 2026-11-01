import type { DocumentFileResponse, DocumentFileVersionResponse } from './types'

/**
 * The version an action would act on: the list route returns versions newest first with one flagged
 * current, and neither promise alone is worth trusting, so both are checked.
 */
export function currentVersionOf(
  file: DocumentFileResponse,
): DocumentFileVersionResponse | undefined {
  return file.versions.find((version) => version.isCurrent) ?? file.versions[0]
}
