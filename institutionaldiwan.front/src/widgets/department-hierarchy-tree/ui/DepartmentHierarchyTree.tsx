import { useCallback, useMemo, useState, type ReactNode } from 'react'
import {
  BarChart3,
  Building2,
  ChevronDown,
  ChevronsDownUp,
  ChevronsUpDown,
  Copy,
  Eye,
  Hash,
  ListTree,
  Pencil,
  Plus,
  Power,
  PowerOff,
  RefreshCw,
  Table2,
  Workflow,
  type LucideIcon,
} from 'lucide-react'
import { toast } from 'sonner'
import {
  DepartmentFormDialog,
  useSetDepartmentActiveMutation,
  type DepartmentFormTarget,
} from '@/features/department-manage'
import {
  departmentTreeToMermaid,
  useDepartments,
  useDepartmentTree,
  type DepartmentTreeNodeResponse,
} from '@/entities/department'
import { findLookupFallback, LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { cn, formatNumber, useTableState } from '@/shared/lib'
import { Button, Checkbox, ConfirmDialog, MermaidView, Skeleton, type MenuEntry } from '@/shared/ui'
import { copyDepartmentValue } from '../model/clipboard'
import {
  collectDepartmentIds,
  countDepartments,
  filterDepartmentTree,
  isDepartmentView,
  type DepartmentView,
} from '../model/departmentTree'
import { DepartmentDetailDialog } from './DepartmentDetailDialog'
import { DepartmentSearch } from './DepartmentSearch'
import { DepartmentTable } from './DepartmentTable'
import { DepartmentTreeNodeRow } from './DepartmentTreeNodeRow'
import { DepartmentTypeChart } from './DepartmentTypeChart'

const VIEW_OPTIONS: readonly { value: DepartmentView; label: string; icon: LucideIcon }[] = [
  { value: 'tree', label: 'شجرة', icon: ListTree },
  { value: 'mermaid', label: 'مخطط', icon: Workflow },
  { value: 'chart', label: 'إحصاء', icon: BarChart3 },
  { value: 'table', label: 'جدول', icon: Table2 },
]

const EMPTY_IDS: ReadonlySet<string> = new Set()

/**
 * Module 01 department hierarchy surface: a native RTL tree with node context
 * menus, a Mermaid `flowchart TD` rendering of the same data, and the flat
 * grid. Search, view, paging, and sorting live in the URL.
 */
export function DepartmentHierarchyTree() {
  const table = useTableState({ filterKeys: ['view'] })
  const view: DepartmentView = isDepartmentView(table.filters.view)
    ? table.filters.view
    : 'tree'

  const setView = (next: DepartmentView) => {
    table.setFilter('view', next === 'tree' ? null : next)
  }

  const treeQuery = useDepartmentTree()
  const listQuery = useDepartments({ enabled: view === 'table' })
  const { data: departmentTypes } = useLookupEntries(LOOKUP_SLUGS.DepartmentTypes)
  const canViewEmployees = useSessionStore((state) => state.hasPermission(Permissions.EmployeeManage))
  const [employeesVisible, setEmployeesVisible] = useState(false)
  const showEmployees = canViewEmployees && employeesVisible

  const typeLabel = useCallback(
    (departmentTypeId: number) =>
      departmentTypes?.find((entry) => entry.id === departmentTypeId)?.descAr ??
      findLookupFallback(LOOKUP_SLUGS.DepartmentTypes, departmentTypeId)?.descAr ??
      `#${departmentTypeId}`,
    [departmentTypes],
  )

  const nameById = useMemo(() => {
    const map = new Map<string, string>()
    const visit = (nodes: readonly DepartmentTreeNodeResponse[]) => {
      nodes.forEach((node) => {
        map.set(node.departmentId, node.nameAr)
        visit(node.children)
      })
    }
    visit(treeQuery.data ?? [])
    listQuery.data?.forEach((department) =>
      map.set(department.departmentId, department.nameAr),
    )
    return map
  }, [treeQuery.data, listQuery.data])

  const resolveName = useCallback(
    (departmentId: string) => nameById.get(departmentId),
    [nameById],
  )

  const filteredTree = useMemo(
    () => filterDepartmentTree(treeQuery.data ?? [], table.search),
    [treeQuery.data, table.search],
  )
  const totalCount = useMemo(
    () => countDepartments(treeQuery.data ?? []),
    [treeQuery.data],
  )
  const matchCount = useMemo(() => countDepartments(filteredTree), [filteredTree])
  const mermaidDiagram = useMemo(
    () => departmentTreeToMermaid(filteredTree),
    [filteredTree],
  )

  const searching = table.search.trim().length > 0
  const [collapsedIds, setCollapsedIds] = useState<ReadonlySet<string>>(EMPTY_IDS)
  // A filtered tree must reveal every matching path, so collapse state is paused.
  const effectiveCollapsedIds = searching ? EMPTY_IDS : collapsedIds

  const toggleCollapsed = useCallback((departmentId: string) => {
    setCollapsedIds((previous) => {
      const next = new Set(previous)
      if (next.has(departmentId)) next.delete(departmentId)
      else next.add(departmentId)
      return next
    })
  }, [])

  const expandAll = () => setCollapsedIds(EMPTY_IDS)

  const collapseAll = useCallback(() => {
    setCollapsedIds(new Set(collectDepartmentIds(treeQuery.data ?? [])))
  }, [treeQuery.data])

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [formTarget, setFormTarget] = useState<DepartmentFormTarget | null>(null)
  const [activeTarget, setActiveTarget] = useState<{
    departmentId: string
    nameAr: string
    active: boolean
  } | null>(null)
  const setActiveMutation = useSetDepartmentActiveMutation()

  const openEditFormById = useCallback((departmentId: string) => {
    setSelectedId(null)
    setFormTarget({ mode: 'edit', departmentId })
  }, [])

  const confirmActiveChange = async () => {
    if (!activeTarget) return

    try {
      await setActiveMutation.mutateAsync({
        departmentId: activeTarget.departmentId,
        active: activeTarget.active,
      })
      toast.success(activeTarget.active ? 'تم تفعيل القسم.' : 'تم إيقاف القسم.')
      setActiveTarget(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const openDiagramNode = useCallback(
    (nodeId: string) => {
      const departmentId = mermaidDiagram.departmentIdByNodeId.get(nodeId)
      if (departmentId) setSelectedId(departmentId)
    },
    [mermaidDiagram],
  )

  const nodeMenu = useCallback(
    (node: DepartmentTreeNodeResponse): MenuEntry[] => {
      const entries: MenuEntry[] = [
        {
          id: 'details',
          label: 'تفاصيل القسم',
          icon: <Eye className="size-4" aria-hidden />,
          onSelect: () => setSelectedId(node.departmentId),
        },
        { type: 'separator' },
        {
          id: 'add-child',
          label: 'إضافة قسم فرعي',
          icon: <Plus className="size-4" aria-hidden />,
          onSelect: () =>
            setFormTarget({ mode: 'create', parentDepartmentId: node.departmentId }),
        },
        {
          id: 'edit',
          label: 'تعديل القسم',
          icon: <Pencil className="size-4" aria-hidden />,
          onSelect: () => openEditFormById(node.departmentId),
        },
        {
          id: 'toggle-active',
          label: node.isActive ? 'إيقاف القسم' : 'تفعيل القسم',
          icon: node.isActive ? (
            <PowerOff className="size-4" aria-hidden />
          ) : (
            <Power className="size-4" aria-hidden />
          ),
          onSelect: () =>
            setActiveTarget({
              departmentId: node.departmentId,
              nameAr: node.nameAr,
              active: !node.isActive,
            }),
        },
        { type: 'separator' },
        {
          id: 'copy-code',
          label: 'نسخ رمز القسم',
          icon: <Copy className="size-4" aria-hidden />,
          onSelect: () => void copyDepartmentValue(node.code, 'رمز القسم'),
        },
        {
          id: 'copy-id',
          label: 'نسخ المعرّف',
          icon: <Hash className="size-4" aria-hidden />,
          onSelect: () => void copyDepartmentValue(node.departmentId, 'المعرّف'),
        },
      ]

      if (node.children.length > 0 || showEmployees) {
        entries.push(
          { type: 'separator' },
          {
            id: 'toggle-branch',
            label: effectiveCollapsedIds.has(node.departmentId) ? 'توسيع الفرع' : 'طي الفرع',
            icon: <ChevronDown className="size-4" aria-hidden />,
            disabled: searching,
            onSelect: () => toggleCollapsed(node.departmentId),
          },
        )
      }

      return entries
    },
    [effectiveCollapsedIds, openEditFormById, searching, showEmployees, toggleCollapsed],
  )

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center gap-2">
        <Button
          type="button"
          onClick={() => setFormTarget({ mode: 'create', parentDepartmentId: null })}
        >
          <Plus className="size-4" aria-hidden />
          قسم جديد
        </Button>

        <div
          role="group"
          aria-label="طريقة عرض الأقسام"
          className="inline-flex items-center gap-1 rounded-lg border border-border bg-surface p-1"
        >
          {VIEW_OPTIONS.map(({ value, label, icon: Icon }) => (
            <Button
              key={value}
              type="button"
              size="sm"
              variant={view === value ? 'primary' : 'ghost'}
              onClick={() => setView(value)}
              aria-pressed={view === value}
            >
              <Icon className="size-4" aria-hidden />
              {label}
            </Button>
          ))}
        </div>

        {view !== 'table' && (
          <DepartmentSearch
            value={table.search}
            onChange={table.setSearch}
            placeholder="ابحث باسم القسم أو رمزه…"
          />
        )}

        <div className="ms-auto flex flex-wrap items-center gap-2">
          {view === 'tree' && (
            <>
              {canViewEmployees && (
                <Checkbox checked={employeesVisible} onCheckedChange={setEmployeesVisible} label="إظهار فروع الموظفين" />
              )}
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-10"
                onClick={expandAll}
                disabled={searching}
              >
                <ChevronsUpDown className="size-4" aria-hidden />
                توسيع الكل
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-10"
                onClick={collapseAll}
                disabled={searching}
              >
                <ChevronsDownUp className="size-4" aria-hidden />
                طي الكل
              </Button>
            </>
          )}
          {view !== 'table' && (
            <Button
              type="button"
              variant="outline"
              size="icon"
              className="size-10"
              onClick={() => void treeQuery.refetch()}
              aria-label="تحديث البيانات"
            >
              <RefreshCw
                className={cn('size-4', treeQuery.isFetching && 'animate-spin')}
                aria-hidden
              />
            </Button>
          )}
        </div>
      </div>

      {view === 'table' && (
        <DepartmentTable
          table={table}
          query={listQuery}
          typeLabel={typeLabel}
          resolveName={resolveName}
          onOpen={setSelectedId}
          onCreate={() => setFormTarget({ mode: 'create', parentDepartmentId: null })}
          onCreateChild={(parentDepartmentId) =>
            setFormTarget({ mode: 'create', parentDepartmentId })
          }
          onEdit={openEditFormById}
          onToggleActive={(department) =>
            setActiveTarget({
              departmentId: department.departmentId,
              nameAr: department.nameAr,
              active: !department.isActive,
            })
          }
        />
      )}

      {view !== 'table' &&
        (treeQuery.isLoading ? (
          <div role="status" aria-label="جارٍ تحميل الهيكل التنظيمي" className="flex flex-col gap-2 rounded-xl border border-border bg-surface p-4">
            {Array.from({ length: 6 }, (_, index) => (
              <Skeleton key={index} className="h-9 w-full" />
            ))}
          </div>
        ) : treeQuery.error ? (
          <div
            role="alert"
            className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            <span>{toArabicErrorMessage(treeQuery.error)}</span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => void treeQuery.refetch()}
            >
              إعادة المحاولة
            </Button>
          </div>
        ) : totalCount === 0 ? (
          <EmptySurface
            title="لا توجد أقسام بعد."
            description="لم تُسجَّل أقسام تنظيمية في الدليل بعد."
          />
        ) : matchCount === 0 ? (
          <EmptySurface
            title="لا توجد نتائج مطابقة."
            description="جرّب تعديل عبارة البحث أو امسحها لعرض كل الأقسام."
            action={
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => table.setSearch('')}
              >
                مسح البحث
              </Button>
            }
          />
        ) : view === 'mermaid' ? (
          <div className="flex flex-col gap-2">
            <p className="text-xs text-muted">
              اضغط على أي قسم في المخطط لعرض تفاصيله والموظفين التابعين له.
            </p>
            <MermaidView source={mermaidDiagram.source} ariaLabel="المخطط التنظيمي للأقسام" onNodeClick={openDiagramNode} className="min-h-40" />
          </div>
        ) : view === 'chart' ? (
          <DepartmentTypeChart nodes={filteredTree} typeLabel={typeLabel} />
        ) : (
          <div className="rounded-xl border border-border bg-surface">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border px-3 py-2 text-xs text-muted">
              <span>
                إجمالي الأقسام: <span dir="ltr">{formatNumber(totalCount)}</span>
              </span>
              {searching && (
                <span>
                  النتائج المطابقة: <span dir="ltr">{formatNumber(matchCount)}</span>
                </span>
              )}
            </div>
            {showEmployees && (
              <p className="px-3 pt-2 text-xs text-muted">افتح «الموظفون المباشرون» داخل أي قسم لتحميل موظفيه فقط. بحث الأقسام لا يبحث في الموظفين.</p>
            )}
            <ul aria-label="الهيكل التنظيمي للأقسام" className="flex flex-col gap-0.5 p-2">
              {filteredTree.map((node) => (
                <DepartmentTreeNodeRow
                  key={node.departmentId}
                  node={node}
                  collapsedIds={effectiveCollapsedIds}
                  collapseDisabled={searching}
                  showEmployees={showEmployees}
                  onToggle={toggleCollapsed}
                  onOpen={setSelectedId}
                  menu={nodeMenu}
                  typeLabel={typeLabel}
                />
              ))}
            </ul>
          </div>
        ))}

      <DepartmentDetailDialog
        departmentId={selectedId}
        onOpenChange={(open) => {
          if (!open) setSelectedId(null)
        }}
        resolveName={resolveName}
        onEdit={openEditFormById}
      />

      <DepartmentFormDialog
        target={formTarget}
        onOpenChange={(open) => {
          if (!open) setFormTarget(null)
        }}
      />

      <ConfirmDialog
        open={activeTarget !== null}
        onOpenChange={(open) => {
          if (!open) setActiveTarget(null)
        }}
        title={activeTarget?.active ? 'تفعيل القسم' : 'إيقاف القسم'}
        description={
          activeTarget?.active
            ? `سيتم إعادة تفعيل «${activeTarget?.nameAr}» وإتاحته في الهيكل التنظيمي.`
            : `سيتم إيقاف «${activeTarget?.nameAr}». لا يمكن إيقاف قسم لديه أقسام فرعية فعّالة.`
        }
        confirmLabel={activeTarget?.active ? 'تفعيل القسم' : 'إيقاف القسم'}
        destructive={activeTarget ? !activeTarget.active : false}
        loading={setActiveMutation.isPending}
        onConfirm={() => void confirmActiveChange()}
      />
    </div>
  )
}

function EmptySurface({
  title,
  description,
  action,
}: {
  title: string
  description: string
  action?: ReactNode
}) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-xl border border-border bg-surface px-4 py-12 text-center">
      <span className="flex size-12 items-center justify-center rounded-full bg-surface-muted text-muted">
        <Building2 className="size-6" aria-hidden />
      </span>
      <div className="flex flex-col gap-1">
        <p className="text-sm font-medium text-foreground">{title}</p>
        <p className="text-xs text-muted">{description}</p>
      </div>
      {action}
    </div>
  )
}
