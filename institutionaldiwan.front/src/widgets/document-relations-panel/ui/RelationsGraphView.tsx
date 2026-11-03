import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { OfficialNumberTag } from '@/entities/document'
import {
  MAX_GRAPH_DEPTH,
  MIN_GRAPH_DEPTH,
  RelationTypeBadge,
  relationGraphToMermaid,
  relationGraphToTree,
  useRelationGraph,
  type RelationTreeNode,
} from '@/entities/document-relation'
import { toArabicErrorMessage } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { cn } from '@/shared/lib'
import { Button, MermaidView, Select, Skeleton, type SelectOption } from '@/shared/ui'

const DEPTH_OPTIONS: SelectOption[] = Array.from(
  { length: MAX_GRAPH_DEPTH - MIN_GRAPH_DEPTH + 1 },
  (_, index) => {
    const depth = MIN_GRAPH_DEPTH + index
    return { value: String(depth), label: `العمق ${depth}` }
  },
)

interface RelationsGraphViewProps {
  documentId: string
  mode: 'graph' | 'tree'
  depth: number
  onDepthChange: (depth: number) => void
}

function TreeBranch({ node, isRoot }: { node: RelationTreeNode; isRoot?: boolean }) {
  const navigate = useNavigate()

  return (
    <li className="flex flex-col gap-1">
      <div className="flex flex-wrap items-center gap-2">
        {node.viaTypeId !== null && <RelationTypeBadge relationTypeId={node.viaTypeId} />}
        {isRoot ? (
          <span className="font-semibold text-foreground">{node.title}</span>
        ) : (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => navigate(ROUTES.DOCUMENT_DETAIL(node.documentId))}
          >
            {node.title}
          </Button>
        )}
        <OfficialNumberTag officialNumberText={node.officialNumberText} />
        {node.isArchived && <span className="text-xs text-muted">مؤرشفة</span>}
      </div>
      {node.children.length > 0 && (
        <ul className={cn('ms-4 flex flex-col gap-1 border-s border-border ps-4')}>
          {node.children.map((child) => (
            <TreeBranch key={child.documentId} node={child} />
          ))}
        </ul>
      )}
    </li>
  )
}

/** Documents reachable through links: a lazy Mermaid diagram, or the same data as an accessible tree. */
export function RelationsGraphView({
  documentId,
  mode,
  depth,
  onDepthChange,
}: RelationsGraphViewProps) {
  const navigate = useNavigate()
  const graphQuery = useRelationGraph(documentId, depth)
  const graph = graphQuery.data

  const diagram = useMemo(() => (graph ? relationGraphToMermaid(graph) : null), [graph])
  const tree = useMemo(() => (graph ? relationGraphToTree(graph) : null), [graph])

  const hasLinks = (graph?.edges.length ?? 0) > 0

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center gap-3">
        <Select
          aria-label="عمق المخطط"
          value={String(depth)}
          onValueChange={(value) => onDepthChange(Number(value))}
          options={DEPTH_OPTIONS}
          className="w-36"
        />
        {graph?.truncated && (
          <p role="status" className="text-sm text-warning">
            اقتُصر العرض على أول 100 وثيقة؛ قلّل العمق لرؤية صورة أوضح.
          </p>
        )}
      </div>

      {graphQuery.isLoading && <Skeleton className="h-64 w-full" />}

      {graphQuery.isError && (
        <div className="flex flex-col items-center gap-3 rounded-xl border border-border bg-surface px-6 py-10 text-center">
          <p role="alert" className="text-sm font-medium text-danger">
            {toArabicErrorMessage(graphQuery.error)}
          </p>
          <Button type="button" variant="outline" onClick={() => void graphQuery.refetch()}>
            إعادة المحاولة
          </Button>
        </div>
      )}

      {graph && !hasLinks && (
        <div className="rounded-xl border border-dashed border-border bg-surface px-6 py-10 text-center">
          <p className="text-sm font-medium text-foreground">لا روابط لعرضها.</p>
          <p className="mt-1 text-xs text-muted">أضف رابطًا ليظهر المخطط هنا.</p>
        </div>
      )}

      {graph && hasLinks && mode === 'graph' && diagram && (
        <MermaidView
          source={diagram.source}
          ariaLabel={`مخطط روابط الوثيقة بعمق ${depth}`}
          className="overflow-x-auto rounded-xl border border-border bg-surface p-4"
          onNodeClick={(nodeId) => {
            const target = diagram.documentIdByNodeId.get(nodeId)
            if (target && target !== documentId) navigate(ROUTES.DOCUMENT_DETAIL(target))
          }}
        />
      )}

      {graph && hasLinks && mode === 'tree' && tree && (
        <ul
          aria-label="شجرة روابط الوثيقة"
          className="rounded-xl border border-border bg-surface p-4"
        >
          <TreeBranch node={tree} isRoot />
        </ul>
      )}
    </div>
  )
}
