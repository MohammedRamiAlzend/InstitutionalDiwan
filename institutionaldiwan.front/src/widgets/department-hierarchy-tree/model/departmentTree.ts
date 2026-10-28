import type { DepartmentTreeNodeResponse } from '@/entities/department'

export type DepartmentView = 'tree' | 'mermaid' | 'chart' | 'table'

const VIEWS: readonly DepartmentView[] = ['tree', 'mermaid', 'chart', 'table']

export function isDepartmentView(value: string | undefined): value is DepartmentView {
  return value !== undefined && VIEWS.includes(value as DepartmentView)
}

/** Name, English name, or code match for the URL search term. */
export function matchesDepartment(
  node: DepartmentTreeNodeResponse,
  query: string,
): boolean {
  const needle = query.trim().toLocaleLowerCase('ar')
  if (!needle) return true

  return (
    node.nameAr.toLocaleLowerCase('ar').includes(needle) ||
    (node.nameEn?.toLocaleLowerCase('ar').includes(needle) ?? false) ||
    node.code.toLocaleLowerCase('en').includes(needle)
  )
}

/**
 * Keeps matching nodes plus the ancestors that lead to them, so a search never
 * detaches a hit from its branch. Children of a hit are dropped (the match is
 * the node itself), which keeps the filtered tree readable.
 */
export function filterDepartmentTree(
  nodes: readonly DepartmentTreeNodeResponse[],
  query: string,
): DepartmentTreeNodeResponse[] {
  if (!query.trim()) return [...nodes]

  const visit = (node: DepartmentTreeNodeResponse): DepartmentTreeNodeResponse | null => {
    if (matchesDepartment(node, query)) return { ...node, children: [] }

    const children = node.children
      .map(visit)
      .filter((child): child is DepartmentTreeNodeResponse => child !== null)

    return children.length > 0 ? { ...node, children } : null
  }

  return nodes
    .map(visit)
    .filter((node): node is DepartmentTreeNodeResponse => node !== null)
}

export function collectDepartmentIds(
  nodes: readonly DepartmentTreeNodeResponse[],
): string[] {
  const ids: string[] = []

  const visit = (node: DepartmentTreeNodeResponse) => {
    ids.push(node.departmentId)
    node.children.forEach(visit)
  }

  nodes.forEach(visit)
  return ids
}

export function countDepartments(
  nodes: readonly DepartmentTreeNodeResponse[],
): number {
  return nodes.reduce((total, node) => total + 1 + countDepartments(node.children), 0)
}
