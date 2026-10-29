export type {
  NumberingCounterResponse,
  NumberingRegistryResponse,
  DepartmentRegisterLineResponse,
  DepartmentRegisterParams,
} from './model/types'
export {
  numberingRegistryKeys,
  useNumberingRegistries,
  useNumberingRegistryById,
  useDepartmentRegister,
} from './model/queries'
export { RegistryScopeCard } from './ui/RegistryScopeCard'
