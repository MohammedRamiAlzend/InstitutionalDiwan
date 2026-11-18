import { useState } from 'react'
import { ListTree, Table2, Workflow, type LucideIcon } from 'lucide-react'
import { EyeOff } from 'lucide-react'
import { useDocumentRelations, DEFAULT_GRAPH_DEPTH, MAX_GRAPH_DEPTH, MIN_GRAPH_DEPTH } from '@/entities/document-relation'
import { CreateRelationDialog, RemoveRelationDialog } from '@/features/document-relation-manage'
import type { LabelKey } from '@/shared/config'
import { t, useTableState } from '@/shared/lib'
import { Button, GuideChip, GuideRegion, Label } from '@/shared/ui'
import { RelationEventsSection } from './RelationEventsSection'
import { RelationsGraphView } from './RelationsGraphView'
import { RelationsGrid } from './RelationsGrid'

type RelationsView = 'table' | 'graph' | 'tree'

const VIEW_OPTIONS: readonly { value: RelationsView; labelKey: LabelKey; icon: LucideIcon }[] = [
  { value: 'table', labelKey: 'relation.panel.view.table', icon: Table2 },
  { value: 'graph', labelKey: 'relation.panel.view.graph', icon: Workflow },
  { value: 'tree', labelKey: 'relation.panel.view.tree', icon: ListTree },
]

function isView(value: string | undefined): value is RelationsView {
  return value === 'table' || value === 'graph' || value === 'tree'
}

function readDepth(raw: string | undefined): number {
  const parsed = Number.parseInt(raw ?? '', 10)
  return Number.isFinite(parsed) && parsed >= MIN_GRAPH_DEPTH && parsed <= MAX_GRAPH_DEPTH
    ? parsed
    : DEFAULT_GRAPH_DEPTH
}

interface DocumentRelationsPanelProps {
  documentId: string
  documentTitle: string
  /** `DOCUMENT_EDIT` on a live (not archived or cancelled) document. */
  canManage: boolean
  /** Arabic reason shown when the caller can edit but the document is frozen. */
  frozenReason?: string
}

/**
 * Module 08 surface of the document page: links as a grid, a diagram, or a tree, with the permanent
 * history underneath. View, depth, filters and paging live in the URL beside `?tab=relations`.
 */
export function DocumentRelationsPanel({
  documentId,
  documentTitle,
  canManage,
  frozenReason,
}: DocumentRelationsPanelProps) {
  const table = useTableState({ filterKeys: ['type', 'direction', 'view', 'depth'] })
  const view = isView(table.filters.view) ? table.filters.view : 'table'
  const depth = readDepth(table.filters.depth)

  const relationsQuery = useDocumentRelations(documentId)
  const [addOpen, setAddOpen] = useState(false)
  const [removing, setRemoving] = useState<{ relationId: string; relatedTitle: string } | null>(null)

  const hidden = relationsQuery.data?.hiddenRelationCount ?? 0

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center gap-2">
        <div role="group" aria-label={t('relation.panel.view-aria')} className="flex items-center gap-1">
          {VIEW_OPTIONS.map(({ value, labelKey, icon: Icon }) => (
            <Button
              key={value}
              type="button"
              size="sm"
              variant={view === value ? 'primary' : 'ghost'}
              aria-pressed={view === value}
              onClick={() => table.setFilter('view', value === 'table' ? null : value)}
            >
              <Icon className="size-4" aria-hidden />
              <Label k={labelKey} />
            </Button>
          ))}
        </div>
        {frozenReason && <p className="text-sm text-muted">{frozenReason}</p>}
      </div>

      {hidden > 0 && (
        <GuideRegion id="document-details.relations-hidden">
          <p
            role="status"
            className="flex items-center gap-2 rounded-lg border border-border bg-surface px-4 py-3 text-sm text-muted"
          >
            <EyeOff className="size-4 shrink-0" aria-hidden />
            {hidden === 1
              ? t('relation.panel.hidden-one')
              : t('relation.panel.hidden-many', { count: hidden })}
          </p>
        </GuideRegion>
      )}

      {view === 'table' ? (
        <GuideRegion id="document-details.relations-grid">
          <RelationsGrid
            relations={relationsQuery.data?.items}
            isLoading={relationsQuery.isLoading}
            error={relationsQuery.error}
            isFetching={relationsQuery.isFetching}
            onRefresh={() => void relationsQuery.refetch()}
            table={table}
            canManage={canManage}
            onAdd={() => setAddOpen(true)}
            onRemove={(relation) =>
              setRemoving({
                relationId: relation.relationId,
                relatedTitle: relation.relatedDocument.title,
              })
            }
          />
        </GuideRegion>
      ) : (
        <GuideRegion id="document-details.relations-graph">
          <RelationsGraphView
            documentId={documentId}
            mode={view}
            depth={depth}
            onDepthChange={(next) =>
              table.setFilter('depth', next === DEFAULT_GRAPH_DEPTH ? null : String(next))
            }
          />
        </GuideRegion>
      )}

      <section aria-labelledby="relation-events-title" className="flex flex-col gap-3">
        <div className="flex items-center gap-2">
          <h2 id="relation-events-title" className="text-base font-semibold">
            <Label k="relation.panel.events-title" />
          </h2>
          <GuideChip id="document-details.relations-events" />
        </div>
        <RelationEventsSection documentId={documentId} />
      </section>

      {canManage && (
        <>
          <CreateRelationDialog
            documentId={documentId}
            documentTitle={documentTitle}
            open={addOpen}
            onOpenChange={setAddOpen}
          />
          <RemoveRelationDialog
            documentId={documentId}
            relation={removing}
            onOpenChange={(open) => {
              if (!open) setRemoving(null)
            }}
          />
        </>
      )}
    </div>
  )
}
