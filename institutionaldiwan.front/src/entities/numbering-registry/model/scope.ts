import type { NumberingRegistryResponse } from './types'

/** The three optional scope dimensions a registry can constrain. */
export interface RegistryScopeFields {
  departmentId: string | null
  documentTypeId: number | null
  directionId: number | null
}

export interface RegistryScopeLabels {
  departmentName?: string | null
  documentTypeLabel?: string | null
  directionLabel?: string | null
}

export interface DuplicateRegistryScope {
  key: string
  registries: NumberingRegistryResponse[]
}

const WILDCARD = '*'

/**
 * Number of constrained scope dimensions (0 = global registry, 3 = fully specific). Mirrors the
 * server's `Specificity` scoring in `NumberingRegistryRepository`.
 */
export function registryScopeRank(scope: RegistryScopeFields): number {
  let rank = 0
  if (scope.departmentId !== null) rank++
  if (scope.documentTypeId !== null) rank++
  if (scope.directionId !== null) rank++
  return rank
}

/** Stable key for a full scope triple; unconstrained dimensions use `*`. */
export function registryScopeKey(scope: RegistryScopeFields): string {
  return [scope.departmentId ?? WILDCARD, scope.documentTypeId ?? WILDCARD, scope.directionId ?? WILDCARD].join(
    '|',
  )
}

/** Arabic summary of a scope; unresolved names fall back to readable placeholders. */
export function describeRegistryScope(
  scope: RegistryScopeFields,
  labels: RegistryScopeLabels = {},
): string {
  if (registryScopeRank(scope) === 0) return 'كل النطاقات (سجل شامل)'

  const parts: string[] = []
  if (scope.departmentId !== null) {
    parts.push(`الدائرة: ${labels.departmentName ?? 'دائرة محددة'}`)
  }
  if (scope.documentTypeId !== null) {
    parts.push(`النوع: ${labels.documentTypeLabel ?? `#${scope.documentTypeId}`}`)
  }
  if (scope.directionId !== null) {
    parts.push(`الاتجاه: ${labels.directionLabel ?? `#${scope.directionId}`}`)
  }
  return parts.join(' · ')
}

/**
 * Active registries that share an identical scope triple. The server resolves such a tie by
 * creation date, so the UI warns administrators instead of silently picking one.
 */
export function findDuplicateActiveScopes(
  registries: readonly NumberingRegistryResponse[],
): DuplicateRegistryScope[] {
  const groups = new Map<string, NumberingRegistryResponse[]>()

  for (const registry of registries) {
    if (!registry.isActive) continue
    const key = registryScopeKey(registry)
    const group = groups.get(key)
    if (group) group.push(registry)
    else groups.set(key, [registry])
  }

  return [...groups.entries()]
    .filter(([, group]) => group.length > 1)
    .map(([key, group]) => ({ key, registries: group }))
}
