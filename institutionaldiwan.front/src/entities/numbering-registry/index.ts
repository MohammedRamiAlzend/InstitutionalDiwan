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
