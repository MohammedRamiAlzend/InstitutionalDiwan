import type { DepartmentResponse } from '@/entities/department'

/** Every department below `rootId`, so the parent picker can never create a cycle. */
export function collectDescendantIds(
  departments: readonly DepartmentResponse[],
  rootId: string,
): Set<string> {
  const childrenById = new Map<string, string[]>()
  departments.forEach((department) => {
    if (!department.parentDepartmentId) return
    const siblings = childrenById.get(department.parentDepartmentId) ?? []
    siblings.push(department.departmentId)
    childrenById.set(department.parentDepartmentId, siblings)
  })

  const descendants = new Set<string>()
  const queue = [rootId]
  while (queue.length > 0) {
    const current = queue.shift() as string
    for (const childId of childrenById.get(current) ?? []) {
      if (descendants.has(childId)) continue
      descendants.add(childId)
      queue.push(childId)
    }
  }

  return descendants
}
