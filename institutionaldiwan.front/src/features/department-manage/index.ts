export type {
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
} from './model/types'
export type { DepartmentFormValues } from './model/schemas'
export {
  useCreateDepartmentMutation,
  useUpdateDepartmentMutation,
  useSetDepartmentActiveMutation,
} from './api/departmentMutations'
export { DepartmentFormDialog } from './ui/DepartmentFormDialog'
export type { DepartmentFormTarget } from './ui/DepartmentFormDialog'
