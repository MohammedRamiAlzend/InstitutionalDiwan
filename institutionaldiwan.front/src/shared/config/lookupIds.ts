/**
 * Deterministic lookup ids — must match the API's seeded lookup entities
 * (`UserScopeType`, `AssignmentType` in IdentityLookups.cs and `DepartmentType`).
 */
export const USER_SCOPE_TYPE = {
  Department: 1,
  DepartmentTree: 2,
  Institution: 3,
  System: 4,
} as const

export const ASSIGNMENT_TYPE = {
  Primary: 1,
  Additional: 2,
} as const

export const DEPARTMENT_TYPE = {
  Directorate: 1,
  Department: 2,
  Section: 3,
  Unit: 4,
  CentralDiwan: 5,
} as const
