import { useMemo } from 'react'
import type { DepartmentTreeNodeResponse } from '@/entities/department'
import { formatNumber } from '@/shared/lib'

/** A textual distribution with decorative bars, using only the already-loaded visible tree. */
export function DepartmentTypeChart({ nodes, typeLabel }: {
  nodes: readonly DepartmentTreeNodeResponse[]
  typeLabel: (departmentTypeId: number) => string
}) {
  const groups = useMemo(() => {
    const counts = new Map<number, number>()
    const visit = (items: readonly DepartmentTreeNodeResponse[]) => {
      items.forEach((node) => {
        counts.set(node.departmentTypeId, (counts.get(node.departmentTypeId) ?? 0) + 1)
        visit(node.children)
      })
    }
    visit(nodes)
    return [...counts.entries()].sort((a, b) => b[1] - a[1] || a[0] - b[0])
  }, [nodes])
  const total = groups.reduce((sum, [, count]) => sum + count, 0)

  return (
    <figure aria-label="توزيع الأقسام حسب النوع" className="rounded-xl border border-border bg-surface p-4">
      <figcaption className="mb-4 flex flex-col gap-1">
        <h3 className="text-sm font-medium text-foreground">توزيع الأقسام حسب النوع</h3>
        <p className="text-xs text-muted">
          الأقسام المعروضة: <span dir="ltr">{formatNumber(total)}</span>. عند البحث، يشمل التوزيع الأقسام المطابقة وأسلافها الظاهرة في الشجرة، وليس الموظفين.
        </p>
      </figcaption>
      <ul className="flex flex-col gap-4">
        {groups.map(([typeId, count]) => {
          const percent = total === 0 ? 0 : (count / total) * 100
          return (
            <li key={typeId} className="flex flex-col gap-1.5">
              <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-foreground">
                <span>{typeLabel(typeId)}</span>
                <span><span dir="ltr">{formatNumber(count)}</span> قسم · <span dir="ltr">{formatNumber(Math.round(percent))}%</span></span>
              </div>
              <div aria-hidden className="h-3 overflow-hidden rounded-full bg-surface-muted">
                <div className="h-full rounded-full bg-accent" style={{ width: `${percent}%` }} />
              </div>
            </li>
          )
        })}
      </ul>
    </figure>
  )
}
