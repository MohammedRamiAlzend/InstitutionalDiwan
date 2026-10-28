import type { DepartmentTreeNodeResponse } from './types'

const MAX_LABEL_LENGTH = 80

/** Mermaid quoted labels keep Arabic readable; strip characters that break the parser. */
function toLabel(value: string): string {
  return value
    .replace(/[\r\n\t]+/g, ' ')
    .replace(/["<>#&;]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, MAX_LABEL_LENGTH)
}

export interface DepartmentMermaidDiagram {
  /** `flowchart TD` source for `shared/ui/mermaid-view`; empty when nothing to draw. */
  source: string
  /**
   * Rendered Mermaid node id (`d1`, `d2`, …) → department id, so the widget can
   * open a department when its diagram node is activated.
   */
  departmentIdByNodeId: ReadonlyMap<string, string>
}

/**
 * Renders a department hierarchy as Mermaid `flowchart TD` source for
 * `shared/ui/mermaid-view`. Ids are sequential (`d1`, `d2`, …) because GUIDs
 * are not valid Mermaid node ids; the Arabic name and LTR code form the label.
 * The returned map keeps the department behind each node id.
 */
export function departmentTreeToMermaid(
  nodes: readonly DepartmentTreeNodeResponse[],
): DepartmentMermaidDiagram {
  const departmentIdByNodeId = new Map<string, string>()
  if (nodes.length === 0) return { source: '', departmentIdByNodeId }

  let sequence = 0
  const lines: string[] = ['flowchart TD']

  const visit = (node: DepartmentTreeNodeResponse, parentId: string | null): void => {
    sequence += 1
    const id = `d${sequence}`
    const inactive = node.isActive ? '' : ' (موقوف)'
    lines.push(`  ${id}["${toLabel(`${node.nameAr} · ${node.code}`)}${inactive}"]`)
    departmentIdByNodeId.set(id, node.departmentId)
    if (parentId) lines.push(`  ${parentId} --> ${id}`)
    node.children.forEach((child) => visit(child, id))
  }

  nodes.forEach((node) => visit(node, null))
  return { source: lines.join('\n'), departmentIdByNodeId }
}
