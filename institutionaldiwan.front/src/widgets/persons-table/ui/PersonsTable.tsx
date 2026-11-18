import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ExternalLink, RefreshCw } from 'lucide-react'
import { usePersons, type PersonListItemResponse } from '@/entities/person'
import { ROUTES } from '@/shared/config'
import { formatDateTime, formatNumber, t, useTableState } from '@/shared/lib'
import { Button, DataTable, Label, type DataTableColumn, type MenuEntry } from '@/shared/ui'

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
        headerKey: 'persons.column.name',
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
        headerKey: 'persons.column.document-count',
        sortable: true,
        sortKey: 'documentCount',
        cell: (person) => <span className="text-sm text-foreground">{formatNumber(person.documentCount)}</span>,
      },
      {
        id: 'lastActivity',
        headerKey: 'persons.column.last-activity',
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
      label: t('persons.row.open'),
      icon: <ExternalLink className="size-4" aria-hidden />,
      onSelect: () => navigate(ROUTES.PERSON_DETAIL(person.personId)),
    },
  ]

  const headerMenu: MenuEntry[] = [
    {
      id: 'refresh',
      label: t('table.refresh'),
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void personsQuery.refetch(),
    },
  ]

  return (
    <DataTable
      tableId="persons"
      caption={t('persons.caption')}
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
      searchPlaceholder={t('persons.search-placeholder')}
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      rowMenu={buildRowMenu}
      headerMenu={headerMenu}
      emptyMenu={headerMenu}
      rowLabel={(person) => person.displayName}
      emptyTitle={table.search ? t('persons.empty.no-match-title') : t('persons.empty.no-persons-title')}
      emptyDescription={
        table.search
          ? t('persons.empty.no-match-description')
          : t('persons.empty.no-persons-description')
      }
      emptyAction={
        table.search ? (
          <Button type="button" variant="outline" onClick={() => table.clearQuery()}>
            <Label k="persons.empty.clear-search" />
          </Button>
        ) : undefined
      }
    />
  )
}
