import { useMemo } from 'react'
import { NameWithId } from '@/shared/ui'
import { useDepartmentOptions } from '../model/options'

interface DepartmentLabelProps {
  departmentId: string | null | undefined
  className?: string
}

/**
 * A department's Arabic name with the short id («ديوان الفرد  1e13069c»). All labels on a page
 * read the one cached department list.
 */
export function DepartmentLabel({ departmentId, className }: DepartmentLabelProps) {
  const { data } = useDepartmentOptions()
  const names = useMemo(
    () => new Map((data ?? []).map((department) => [department.departmentId, department.nameAr])),
    [data],
  )

  if (!departmentId) return <span>—</span>
  return <NameWithId name={names.get(departmentId)} id={departmentId} className={className} />
}
