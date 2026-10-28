/** `EmployeeResponse` from `/api/employees` (camelCase JSON). */
export interface EmployeeResponse {
  employeeId: string
  employeeNumber: string | null
  fullNameAr: string
  fullNameEn: string | null
  departmentId: string
  jobTitleAr: string | null
  email: string | null
  phone: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

/** Server-side paging + name/number search for the employee list. */
export interface EmployeeListParams {
  page: number
  pageSize: number
  search?: string
  /** Restricts the page to one department. */
  departmentId?: string
  /** Widens `departmentId` to every descendant department. */
  includeSubDepartments?: boolean
  sortBy?: string
  sortDescending?: boolean
}
