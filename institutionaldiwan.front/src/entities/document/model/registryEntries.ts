import type {
  DepartmentRegistryNumberResponse,
  DocumentRegistryNumbersResponse,
} from './types'

/** The department's register line for that year; a department has at most one per document per year. */
export function findDepartmentEntry(
  entries: DocumentRegistryNumbersResponse | undefined,
  departmentId: string | null | undefined,
  year: number,
): DepartmentRegistryNumberResponse | undefined {
  if (!entries || !departmentId) return undefined

  return entries.departmentNumbers.find(
    (line) => line.departmentId === departmentId && line.numberingYear === year,
  )
}
