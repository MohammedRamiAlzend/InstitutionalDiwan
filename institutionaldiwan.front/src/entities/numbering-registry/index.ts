export type {
  NumberingCounterResponse,
  NumberingRegistryResponse,
  DepartmentRegisterEntriesParams,
  DepartmentRegisterEntryResponse,
  DepartmentRegisterLineResponse,
  DepartmentRegisterParams,
} from './model/types'
export {
  numberingRegistryKeys,
  useNumberingRegistries,
  useNumberingRegistryById,
  useDepartmentRegister,
  useDepartmentRegisterEntries,
} from './model/queries'
export { useDepartmentRegistryOptions } from './model/departmentRegistries'
export type { DepartmentRegistryOptions } from './model/departmentRegistries'
export { DepartmentRegistrySelect } from './ui/DepartmentRegistrySelect'
export { RegistryScopeCard } from './ui/RegistryScopeCard'
export {
  DEFAULT_NUMBER_PATTERN,
  renderNumberPattern,
  unknownPatternTokens,
} from './model/format'
export type { NumberPatternValues } from './model/format'
export {
  registryScopeRank,
  registryScopeKey,
  describeRegistryScope,
  findDuplicateActiveScopes,
} from './model/scope'
export type {
  RegistryScopeFields,
  RegistryScopeLabels,
  DuplicateRegistryScope,
} from './model/scope'
