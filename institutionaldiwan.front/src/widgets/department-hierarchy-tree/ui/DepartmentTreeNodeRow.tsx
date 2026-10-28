import { useId } from 'react'
import { Building2, ChevronDown, Landmark, MoreHorizontal } from 'lucide-react'
import {
  DepartmentBadge,
  type DepartmentTreeNodeResponse,
} from '@/entities/department'
import { DEPARTMENT_TYPE } from '@/shared/config'
import { cn, formatNumber } from '@/shared/lib'
import {
  Badge,
  Button,
  ContextMenu,
  DropdownMenu,
  dispatchContextMenu,
  isContextMenuShortcut,
  type MenuEntry,
} from '@/shared/ui'
import { DepartmentEmployeesSection } from './DepartmentEmployeesSection'

interface DepartmentTreeNodeRowProps {
  node: DepartmentTreeNodeResponse
  /** Already search-aware: searches keep every matching path expanded. */
  collapsedIds: ReadonlySet<string>
  collapseDisabled: boolean
  showEmployees: boolean
  onToggle: (departmentId: string) => void
  onOpen: (departmentId: string) => void
  menu: (node: DepartmentTreeNodeResponse) => MenuEntry[]
  typeLabel: (departmentTypeId: number) => string
}

/** Recursive native list hierarchy, with identical context and overflow menu actions. */
export function DepartmentTreeNodeRow({
  node,
  collapsedIds,
  collapseDisabled,
  showEmployees,
  onToggle,
  onOpen,
  menu,
  typeLabel,
}: DepartmentTreeNodeRowProps) {
  const branchId = useId()
  const hasChildren = node.children.length > 0
  const hasBranch = hasChildren || showEmployees
  const collapsed = collapsedIds.has(node.departmentId)
  const isCentralDiwan = node.departmentTypeId === DEPARTMENT_TYPE.CentralDiwan
  const Icon = isCentralDiwan ? Landmark : Building2
  const entries = menu(node)

  return (
    <li className="min-w-0">
      <div className="flex items-center gap-1.5 rounded-lg py-0.5 transition-colors hover:bg-surface-muted/60">
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="size-7 shrink-0"
          onClick={() => onToggle(node.departmentId)}
          disabled={!hasBranch || collapseDisabled}
          aria-label={hasBranch ? `${collapsed ? 'توسيع' : 'طي'} فرع ${node.nameAr}` : 'لا فروع'}
          aria-expanded={hasBranch ? !collapsed : undefined}
          aria-controls={hasBranch && !collapsed ? branchId : undefined}
        >
          {hasBranch ? (
            <ChevronDown
              className={cn('size-4 transition-transform', collapsed && '-rotate-90 rtl:rotate-90')}
              aria-hidden
            />
          ) : <span className="size-4" aria-hidden />}
        </Button>

        <ContextMenu entries={entries}>
          <button
            type="button"
            onClick={() => onOpen(node.departmentId)}
            onKeyDown={(event) => {
              if (!isContextMenuShortcut(event)) return
              event.preventDefault()
              dispatchContextMenu(event.currentTarget)
            }}
            aria-label={`تفاصيل ${node.nameAr}`}
            className="flex min-w-0 flex-1 cursor-pointer items-center gap-2 rounded-md px-1.5 py-1.5 text-start focus-visible:outline-2 focus-visible:outline-accent"
          >
            <Icon className="size-4 shrink-0 text-muted" aria-hidden />
            <DepartmentBadge nameAr={node.nameAr} code={node.code} isActive={node.isActive} />
            <span className="hidden shrink-0 text-xs text-muted sm:inline">{typeLabel(node.departmentTypeId)}</span>
            {isCentralDiwan && <Badge variant="accent" className="hidden shrink-0 sm:inline-flex">الديوان المركزي</Badge>}
            {hasChildren && (
              <span className="shrink-0 rounded-full bg-surface-muted px-2 py-0.5 text-[11px] leading-none text-muted" title="عدد الأقسام الفرعية المباشرة">
                {formatNumber(node.children.length)}
              </span>
            )}
          </button>
        </ContextMenu>
        <DropdownMenu entries={entries} label={node.nameAr} trigger={
          <Button type="button" variant="ghost" size="icon" className="size-8 shrink-0" aria-label={`إجراءات القسم ${node.nameAr}`}>
            <MoreHorizontal className="size-4" aria-hidden />
          </Button>
        } />
      </div>

      {hasBranch && !collapsed && (
        <ul id={branchId} className="ms-5 flex flex-col gap-0.5 border-s border-border ps-2">
          {node.children.map((child) => (
            <DepartmentTreeNodeRow
              key={child.departmentId}
              node={child}
              collapsedIds={collapsedIds}
              collapseDisabled={collapseDisabled}
              showEmployees={showEmployees}
              onToggle={onToggle}
              onOpen={onOpen}
              menu={menu}
              typeLabel={typeLabel}
            />
          ))}
          {showEmployees && (
            <li className="min-w-0 py-1">
              <DepartmentEmployeesSection departmentId={node.departmentId} variant="tree" />
            </li>
          )}
        </ul>
      )}
    </li>
  )
}
