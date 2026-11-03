import type { RelationGraphResponse, RelationTypeResponse } from './types'

const MAX_LABEL_LENGTH = 70

/** Mermaid quoted labels keep Arabic readable; strip characters that break the parser. */
function toLabel(value: string): string {
  return value
    .replace(/[\r\n\t]+/g, ' ')
    .replace(/["<>#&;|]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, MAX_LABEL_LENGTH)
}

export interface RelationMermaidDiagram {
  /** `flowchart LR` source for `shared/ui/mermaid-view`; empty when there is nothing to draw. */
  source: string
  /** Rendered Mermaid node id (`n1`, `n2`, …) → document id, so a node can open its document. */
  documentIdByNodeId: ReadonlyMap<string, string>
}

function typeLabel(types: readonly RelationTypeResponse[], relationTypeId: number): string {
  return types.find((type) => type.relationTypeId === relationTypeId)?.descAr ?? `#${relationTypeId}`
}

/**
 * Renders the relations graph as Mermaid source. GUIDs are not valid node ids,
 * so nodes are numbered in depth order; the root gets a thicker outline.
 */
export function relationGraphToMermaid(graph: RelationGraphResponse): RelationMermaidDiagram {
  const documentIdByNodeId = new Map<string, string>()
  if (graph.nodes.length <= 1 && graph.edges.length === 0) {
    return { source: '', documentIdByNodeId }
  }

  const nodeIdByDocumentId = new Map<string, string>()
  const lines: string[] = ['flowchart LR']

  graph.nodes.forEach(({ document }, index) => {
    const nodeId = `n${index + 1}`
    nodeIdByDocumentId.set(document.documentId, nodeId)
    documentIdByNodeId.set(nodeId, document.documentId)
    const number = document.officialNumberText ? ` · ${document.officialNumberText}` : ''
    lines.push(`  ${nodeId}["${toLabel(`${document.title}${number}`)}"]`)
  })

  for (const edge of graph.edges) {
    const from = nodeIdByDocumentId.get(edge.sourceDocumentId)
    const to = nodeIdByDocumentId.get(edge.targetDocumentId)
    if (!from || !to) continue
    lines.push(`  ${from} -->|"${toLabel(typeLabel(graph.types, edge.relationTypeId))}"| ${to}`)
  }

  const rootNodeId = nodeIdByDocumentId.get(graph.rootDocumentId)
  if (rootNodeId) lines.push(`  style ${rootNodeId} stroke-width:3px`)

  return { source: lines.join('\n'), documentIdByNodeId }
}

export interface RelationTreeNode {
  documentId: string
  title: string
  officialNumberText: string | null
  isArchived: boolean
  /** Relation type of the edge from the parent; `null` for the root. */
  viaTypeId: number | null
  /** True when the link points from the parent to this node (the parent is the source). */
  viaOutgoing: boolean
  children: RelationTreeNode[]
}

/**
 * Accessible fallback for the diagram: a breadth-first tree from the root where
 * each document appears once (under the first parent that reaches it).
 */
export function relationGraphToTree(graph: RelationGraphResponse): RelationTreeNode | null {
  const summaries = new Map(graph.nodes.map(({ document }) => [document.documentId, document]))
  const root = summaries.get(graph.rootDocumentId)
  if (!root) return null

  const toNode = (
    documentId: string,
    viaTypeId: number | null,
    viaOutgoing: boolean,
  ): RelationTreeNode | null => {
    const summary = summaries.get(documentId)
    if (!summary) return null
    return {
      documentId,
      title: summary.title,
      officialNumberText: summary.officialNumberText,
      isArchived: summary.isArchived,
      viaTypeId,
      viaOutgoing,
      children: [],
    }
  }

  const rootNode = toNode(root.documentId, null, true)
  if (!rootNode) return null

  const placed = new Set<string>([root.documentId])
  const queue: RelationTreeNode[] = [rootNode]

  for (let parent = queue.shift(); parent; parent = queue.shift()) {
    for (const edge of graph.edges) {
      const outgoing = edge.sourceDocumentId === parent.documentId
      if (!outgoing && edge.targetDocumentId !== parent.documentId) continue
      const otherId = outgoing ? edge.targetDocumentId : edge.sourceDocumentId
      if (placed.has(otherId)) continue
      const child = toNode(otherId, edge.relationTypeId, outgoing)
      if (!child) continue
      placed.add(otherId)
      parent.children.push(child)
      queue.push(child)
    }
  }

  return rootNode
}
