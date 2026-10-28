import { useCallback, useEffect, useId, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { ChevronDown, ChevronLeft, ChevronRight, Copy, Eye, Hash, MoreHorizontal, RefreshCw, UserRound, Users } from 'lucide-react'
import { useEmployees, type EmployeeResponse } from '@/entities/employee'
import { useSessionStore } from '@/entities/user'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { cn, formatNumber } from '@/shared/lib'
import { Badge, Button, Checkbox, ContextMenu, DropdownMenu, Skeleton, dispatchContextMenu, isContextMenuShortcut, type MenuEntry } from '@/shared/ui'
import { copyDepartmentValue } from '../model/clipboard'
import { DepartmentEmployeeDetailDialog } from './DepartmentEmployeeDetailDialog'
import { DepartmentSearch } from './DepartmentSearch'

const PAGE_SIZE = 20

interface DepartmentEmployeesSectionProps {
  departmentId: string
  /** Tree branches are opt-in and always read only the department's direct employees. */
  variant?: 'roster' | 'tree'
}

/** No employee query is mounted without permission or before a tree employee branch is opened. */
export function DepartmentEmployeesSection({ departmentId, variant = 'roster' }: DepartmentEmployeesSectionProps) {
  const canViewEmployees = useSessionStore((state) => state.hasPermission(Permissions.EmployeeManage))
  const [expanded, setExpanded] = useState(false)
  const regionId = useId()
  if (!canViewEmployees) return null

  const open = variant === 'roster' || expanded
  return (
    <section className="flex min-w-0 flex-col rounded-xl border border-border">
      <header className="flex flex-wrap items-center gap-2 px-3 py-2">
        <Users className="size-4 text-muted" aria-hidden />
        {variant === 'tree' ? (
          <Button type="button" variant="ghost" size="sm" aria-expanded={expanded} aria-controls={regionId} onClick={() => setExpanded(!expanded)}>
            <ChevronDown className={cn('size-4', !expanded && '-rotate-90 rtl:rotate-90')} aria-hidden />
            الموظفون المباشرون
          </Button>
        ) : <h3 className="text-sm font-medium text-foreground">الموظفون</h3>}
      </header>
      <div id={regionId} hidden={!open}>
        {open && <EmployeeRoster key={`${variant}:${departmentId}`} departmentId={departmentId} variant={variant} />}
      </div>
    </section>
  )
}

/** Namespaced URL state keeps independent department branches separate from the main grid. */
function EmployeeRoster({ departmentId, variant }: Required<DepartmentEmployeesSectionProps>) {
  const [params, setParams] = useSearchParams()
  const prefix = `employees.${variant}.${departmentId}`
  const rawPage = Number(params.get(`${prefix}.page`) ?? 1)
  const page = Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1
  const search = params.get(`${prefix}.search`) ?? ''
  const includeSubDepartments = variant === 'roster' && params.get(`${prefix}.descendants`) !== 'false'
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const update = useCallback((key: string, value: string | null, resetPage = false) => {
    setParams((previous) => {
      const next = new URLSearchParams(previous)
      if (resetPage) next.delete(`${prefix}.page`)
      if (value === null) next.delete(`${prefix}.${key}`)
      else next.set(`${prefix}.${key}`, value)
      return next
    }, { replace: true })
  }, [prefix, setParams])
  const setPage = useCallback((next: number) => update('page', next <= 1 ? null : String(next)), [update])
  const setSearch = useCallback((next: string) => update('search', next.trim() || null, true), [update])

  const query = useEmployees({ page, pageSize: PAGE_SIZE, departmentId, includeSubDepartments, search: search || undefined })
  // useEmployees retains the previous page while fetching: never label stale rows as the new page/filter.
  const busy = query.isLoading || query.isPlaceholderData
  const data = !query.isPlaceholderData && !query.error ? query.data : undefined
  const totalPages = Math.max(1, data?.totalPages ?? 1)
  useEffect(() => {
    if (data && page > totalPages) setPage(totalPages)
  }, [data, page, setPage, totalPages])

  const employeeMenu = (employee: EmployeeResponse): MenuEntry[] => [
    { id: 'details', label: 'تفاصيل الموظف', icon: <Eye className="size-4" aria-hidden />, onSelect: () => setSelectedId(employee.employeeId) },
    { type: 'separator' },
    { id: 'copy-name', label: 'نسخ اسم الموظف', icon: <Copy className="size-4" aria-hidden />, onSelect: () => void copyDepartmentValue(employee.fullNameAr, 'اسم الموظف') },
    { id: 'copy-number', label: 'نسخ الرقم الوظيفي', icon: <Copy className="size-4" aria-hidden />, disabled: !employee.employeeNumber, onSelect: () => void copyDepartmentValue(employee.employeeNumber ?? '', 'الرقم الوظيفي') },
    { id: 'copy-id', label: 'نسخ المعرّف', icon: <Hash className="size-4" aria-hidden />, onSelect: () => void copyDepartmentValue(employee.employeeId, 'المعرّف') },
  ]

  return (
    <div className="border-t border-border" aria-busy={query.isFetching}>
      <div className="flex flex-wrap items-center gap-2 p-3">
        <DepartmentSearch value={search} onChange={setSearch} placeholder="ابحث باسم الموظف أو رقمه الوظيفي…" />
        {variant === 'roster' && (
          <Checkbox checked={includeSubDepartments} onCheckedChange={(checked) => update('descendants', checked ? null : 'false', true)} label="تضمين الأقسام الفرعية" />
        )}
        <Button type="button" variant="ghost" size="icon" className="ms-auto shrink-0" aria-label="تحديث الموظفين" disabled={query.isFetching} onClick={() => void query.refetch()}>
          <RefreshCw className={cn('size-4', query.isFetching && 'animate-spin')} aria-hidden />
        </Button>
      </div>
      {busy ? (
        <div role="status" aria-label="جارٍ تحميل الموظفين" className="flex flex-col gap-2 p-3">
          {Array.from({ length: 3 }, (_, index) => <Skeleton key={index} className="h-10 w-full" />)}
        </div>
      ) : query.error ? (
        <div role="alert" className="m-3 flex flex-wrap items-center gap-3 rounded-lg border border-danger/40 bg-danger/10 p-3 text-sm text-danger">
          <span>{toArabicErrorMessage(query.error)}</span>
          <Button type="button" variant="outline" size="sm" onClick={() => void query.refetch()}>إعادة المحاولة</Button>
          {page > 1 && <Button type="button" variant="outline" size="sm" onClick={() => setPage(1)}>الصفحة الأولى</Button>}
        </div>
      ) : !data?.items.length ? (
        <div className="flex flex-col items-center gap-2 px-3 py-6 text-center">
          <p className="text-sm text-foreground">{search ? 'لا توجد نتائج مطابقة.' : 'لا يوجد موظفون مسجّلون هنا.'}</p>
          <p className="text-xs text-muted">{search ? 'جرّب تعديل عبارة البحث أو مسحها.' : includeSubDepartments ? 'لم يُسجَّل أي موظف في هذا القسم أو الأقسام التابعة له.' : 'لا يوجد موظفون في هذا القسم مباشرةً.'}</p>
          {search && <Button type="button" variant="outline" size="sm" onClick={() => setSearch('')}>مسح بحث الموظفين</Button>}
        </div>
      ) : (
        <ul aria-label="موظفو القسم" className="divide-y divide-border">
          {data.items.map((employee) => (
            <EmployeeRow key={employee.employeeId} employee={employee} entries={employeeMenu(employee)} onOpen={() => setSelectedId(employee.employeeId)} />
          ))}
        </ul>
      )}
      {data && (
        <nav aria-label="صفحات موظفي القسم" className="flex flex-wrap items-center justify-between gap-2 border-t border-border px-3 py-2 text-xs text-muted">
          <span role="status">
            الإجمالي: <span dir="ltr">{formatNumber(data.totalCount)}</span> · الصفحة <span dir="ltr">{formatNumber(page)}</span> من <span dir="ltr">{formatNumber(totalPages)}</span>
          </span>
          <div className="flex items-center gap-2">
            <Button type="button" variant="outline" size="icon" className="size-8" disabled={page <= 1 || query.isFetching} onClick={() => setPage(page - 1)} aria-label="الصفحة السابقة للموظفين">
              <ChevronLeft className="size-4 rtl:rotate-180" aria-hidden />
            </Button>
            <Button type="button" variant="outline" size="icon" className="size-8" disabled={page >= totalPages || query.isFetching} onClick={() => setPage(page + 1)} aria-label="الصفحة التالية للموظفين">
              <ChevronRight className="size-4 rtl:rotate-180" aria-hidden />
            </Button>
          </div>
        </nav>
      )}
      <DepartmentEmployeeDetailDialog employeeId={selectedId} onOpenChange={(open) => { if (!open) setSelectedId(null) }} />
    </div>
  )
}

function EmployeeRow({ employee, entries, onOpen }: { employee: EmployeeResponse; entries: MenuEntry[]; onOpen: () => void }) {
  return (
    <li className="flex min-w-0 items-center gap-2 px-3 py-2">
      <ContextMenu entries={entries}>
        <button type="button" onClick={onOpen} onKeyDown={(event) => {
          if (!isContextMenuShortcut(event)) return
          event.preventDefault()
          dispatchContextMenu(event.currentTarget)
        }} aria-label={`تفاصيل الموظف ${employee.fullNameAr}`} className="flex min-w-0 flex-1 cursor-pointer flex-wrap items-center gap-2 rounded-md p-1 text-start hover:bg-surface-muted focus-visible:outline-2 focus-visible:outline-accent">
          <UserRound className="size-4 shrink-0 text-muted" aria-hidden />
          <span className="min-w-0 flex-1">
            <span className="block truncate text-sm font-medium text-foreground">{employee.fullNameAr}</span>
            <span className="block truncate text-xs text-muted">{employee.jobTitleAr ?? 'بدون مسمى وظيفي'}</span>
          </span>
          {employee.employeeNumber && <span dir="ltr" className="font-mono text-xs text-muted">{employee.employeeNumber}</span>}
          <Badge variant={employee.isActive ? 'success' : 'danger'}>{employee.isActive ? 'نشط' : 'موقوف'}</Badge>
        </button>
      </ContextMenu>
      <DropdownMenu entries={entries} label={employee.fullNameAr} trigger={
        <Button type="button" variant="ghost" size="icon" className="size-8 shrink-0" aria-label={`إجراءات الموظف ${employee.fullNameAr}`}>
          <MoreHorizontal className="size-4" aria-hidden />
        </Button>
      } />
    </li>
  )
}
