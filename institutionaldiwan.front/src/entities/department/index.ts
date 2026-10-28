export type { DepartmentResponse, DepartmentTreeNodeResponse } from './model/types'
export {
  departmentKeys,
  useDepartments,
  useDepartmentTree,
  useDepartmentById,
} from './model/queries'
export { departmentTreeToMermaid } from './model/toMermaid'
export { DepartmentBadge } from './ui/DepartmentBadge'
