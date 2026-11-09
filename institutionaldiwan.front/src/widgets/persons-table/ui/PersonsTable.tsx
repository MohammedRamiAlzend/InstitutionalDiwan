import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ExternalLink, RefreshCw } from 'lucide-react'
import { usePersons, type PersonListItemResponse } from '@/entities/person'
import { ROUTES } from '@/shared/config'
import { formatDateTime, formatNumber, useTableState } from '@/shared/lib'
import { Button, DataTable, type DataTableColumn, type MenuEntry } from '@/shared/ui'

/** Everyone named on a document the caller may view; a row leads to that person's file. */
export function PersonsTable() {
  const table = useTableState({ defaultSortKey: 'lastActivity' })
  const navigate = useNavigate()

  const personsQuery = usePersons({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    sortBy: table.sortKey ?? undefined,
    sortDescending: table.sortDirection === 'desc',
  })

  const columns = useMemo<DataTableColumn<PersonListItemResponse>[]>(
    () => [
      {
        id: 'name',
        header: 'الاسم',
        hideable: false,
        sortable: true,
        sortKey: 'name',
        cell: (person) => (
          <Link to={ROUTES.PERSON_DETAIL(person.personId)} className="text-sm font-medium text-accent hover:underline">
            {person.displayName}
          </Link>
        ),
      },
      {
        id: 'documentCount',
        header: 'عدد الوثائق',
        sortable: true,
        sortKey: 'documentCount',
        cell: (person) => <span className="text-sm text-foreground">{formatNumber(person.documentCount)}</span>,
      },
      {
        id: 'lastActivity',
        header: 'آخر تحديث على وثائقه',
        sortable: true,
        sortKey: 'lastActivity',
        cell: (person) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(person.lastActivityAt)}
          </span>
        ),
      },
    ],
    [],
  )

  const buildRowMenu = (person: PersonListItemResponse): MenuEntry[] => [
    {
      id: 'open',
      label: 'فتح ملف الشخص',
      icon: <ExternalLink className="size-4" aria-hidden />,
      onSelect: () => navigate(ROUTES.PERSON_DETAIL(person.personId)),
    },
  ]

  const headerMenu: MenuEntry[] = [
    {
      id: 'refresh',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void personsQuery.refetch(),
    },
  ]

  return (
    <DataTable
      tableId="persons"
      caption="الأشخاص المرتبطون بالوثائق"
      columns={columns}
      rows={personsQuery.data?.items}
      rowKey={(person) => person.personId}
      isLoading={personsQuery.isLoading}
      error={personsQuery.error}
      onRetry={() => void personsQuery.refetch()}
      onRefresh={() => void personsQuery.refetch()}
      isFetching={personsQuery.isFetching}
      page={table.page}
      pageSize={table.pageSize}
      totalCount={personsQuery.data?.totalCount ?? 0}
      onPageChange={table.setPage}
      onPageSizeChange={table.setPageSize}
      search={table.search}
      onSearchChange={table.setSearch}
      searchPlaceholder="ابحث بالاسم…"
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      rowMenu={buildRowMenu}
      headerMenu={headerMenu}
      emptyMenu={headerMenu}
      rowLabel={(person) => person.displayName}
      emptyTitle={table.search ? 'لا توجد نتائج مطابقة.' : 'لا يوجد أشخاص بعد.'}
      emptyDescription={
        table.search
          ? 'جرّب كتابة جزء آخر من الاسم.'
          : 'يظهر هنا كل شخص يُضاف إلى وثيقة يمكنك الاطلاع عليها.'
      }
      emptyAction={
        table.search ? (
          <Button type="button" variant="outline" onClick={() => table.clearQuery()}>
            مسح البحث
          </Button>
        ) : undefined
      }
    />
  )
}
