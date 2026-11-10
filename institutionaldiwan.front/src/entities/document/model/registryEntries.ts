import { DOCUMENT_NUMBER_STATUS } from '@/shared/config'
import type {
  DepartmentRegistryNumberResponse,
  DocumentRegistryNumbersResponse,
} from './types'

/**
 * The department's register line in use for that year. A department that renumbered the document
 * has replaced lines beside it; those are skipped unless nothing else is left.
 */
export function findDepartmentEntry(
  entries: DocumentRegistryNumbersResponse | undefined,
  departmentId: string | null | undefined,
  year: number,
): DepartmentRegistryNumberResponse | undefined {
  if (!entries || !departmentId) return undefined

  const lines = entries.departmentNumbers.filter(
    (line) => line.departmentId === departmentId && line.numberingYear === year,
  )
  return lines.find((line) => line.statusId !== DOCUMENT_NUMBER_STATUS.Replaced) ?? lines[0]
}
