/** `POST /api/employees` payload; empty optional text is sent as `null`. */
export interface CreateEmployeeRequest {
  fullNameAr: string
  departmentId: string
  employeeNumber: string | null
  fullNameEn: string | null
  jobTitleAr: string | null
  email: string | null
  phone: string | null
}

/** `PUT /api/employees/{id}` payload; the employee id is immutable. */
export interface UpdateEmployeeRequest {
  fullNameAr: string
  departmentId: string
  employeeNumber: string | null
  fullNameEn: string | null
  jobTitleAr: string | null
  email: string | null
  phone: string | null
}
