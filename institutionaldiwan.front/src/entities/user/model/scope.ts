import { USER_SCOPE_TYPE } from '@/shared/config'
import type { UserScope } from './types'

/** Semantic scope → backend `ScopeTypeId` (mirrors the `UserScopeType` lookup). */
export const USER_SCOPE_TYPE_IDS: Record<UserScope, number> = {
  Department: USER_SCOPE_TYPE.Department,
  DepartmentTree: USER_SCOPE_TYPE.DepartmentTree,
  Institution: USER_SCOPE_TYPE.Institution,
  System: USER_SCOPE_TYPE.System,
}

const SCOPES_BY_ID = new Map<number, UserScope>(
  (Object.entries(USER_SCOPE_TYPE_IDS) as [UserScope, number][]).map(
    ([scope, id]) => [id, scope],
  ),
)

export function userScopeFromId(scopeTypeId: number): UserScope | undefined {
  return SCOPES_BY_ID.get(scopeTypeId)
}
