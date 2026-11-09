import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ExternalLink, RefreshCw } from 'lucide-react'
import { DepartmentLabel } from '@/entities/department'
import { DocumentStatusBadge, OfficialNumberTag } from '@/entities/document'
import { LOOKUP_SLUGS, LookupLabel, useLookupEntries } from '@/entities/lookup'
import { usePersonDocuments, type PersonDocumentResponse } from '@/entities/person'
import { UserLabel } from '@/entities/user'
import { ROUTES } from '@/shared/config'
import { formatDateTime, useTableState } from '@/shared/lib'
import { Button, DataTable, Select, type DataTableColumn, type MenuEntry } from '@/shared/ui'

const ALL = '__all__'

interface PersonDocumentsTableProps {
  personId: string
}

function CurrentLocation({ row }: { row: PersonDocumentResponse }) {
  if (row.currentArchiveLocationId !== null) {
    return <LookupLabel lookup={LOOKUP_SLUGS.ArchiveLocations} id={row.currentArchiveLocationId} />
  }
  return (
    <div className="flex flex-col gap-0.5">
      <DepartmentLabel departmentId={row.currentDepartmentId} className="text-sm" />
      {row.activeMovementStatusId !== null && (
        <LookupLabel lookup={LOOKUP_SLUGS.MovementStatuses} id={row.activeMovementStatusId} className="text-xs text-muted" />
      )}
    </div>
  )
}

function LastAction({ row }: { row: PersonDocumentResponse }) {
  return (
    <div className="flex flex-col gap-0.5">
      {row.lastActionKind === 'MOVEMENT' ? (
        <LookupLabel lookup={LOOKUP_SLUGS.MovementEventTypes} id={row.lastMovementEventTypeId} />
      ) : (
        <span className="text-sm text-foreground">تغيير الحالة</span>
      )}
      <span className="flex flex-wrap items-center gap-1 text-xs text-muted">
        <span dir="ltr">{formatDateTime(row.lastActionAt)}</span>
        <span aria-hidden>·</span>
        <UserLabel userId={row.lastActionUserId} />
      </span>
    </div>
  )
}

/** The person's documents the caller may view: role, status, where each one is now, last action. */
export function PersonDocumentsTable({ personId }: PersonDocumentsTableProps) {
  const table = useTableState({ defaultSortKey: 'linkedAt', filterKeys: ['role', 'status'] })
  const navigate = useNavigate()

  const documentsQuery = usePersonDocuments(personId, {
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    sortBy: table.sortKey ?? undefined,
    sortDescending: table.sortDirection === 'desc',
    roleId: table.filters.role ? Number(table.filters.role) : undefined,
    statusId: table.filters.status ? Number(table.filters.status) : undefined,
  })

  const rolesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentPersonRoles)
  const statusesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentStatusTypes)

  const columns = useMemo<DataTableColumn<PersonDocumentResponse>[]>(
    () => [
      {
        id: 'title',
        header: 'الوثيقة',
        hideable: false,
        sortable: true,
        sortKey: 'title',
        cell: (row) => (
          <Link to={ROUTES.DOCUMENT_DETAIL(row.documentId)} className="text-sm font-medium text-accent hover:underline">
            {row.title}
          </Link>
        ),
      },
      {
        id: 'type',
        header: 'النوع',
        defaultHidden: true,
        cell: (row) => <LookupLabel lookup={LOOKUP_SLUGS.DocumentTypes} id={row.documentTypeId} className="text-xs" />,
      },
      {
        id: 'number',
        header: 'الرقم',
        defaultHidden: true,
        cell: (row) => <OfficialNumberTag officialNumberText={row.officialNumberText} />,
      },
      {
        id: 'role',
        header: 'صفته',
        cell: (row) => <LookupLabel lookup={LOOKUP_SLUGS.DocumentPersonRoles} id={row.roleId} />,
      },
      {
        id: 'status',
        header: 'الحالة',
        sortable: true,
        sortKey: 'status',
        cell: (row) => <DocumentStatusBadge statusId={row.statusId} />,
      },
      {
        id: 'location',
        header: 'موجودة الآن في',
        cell: (row) => <CurrentLocation row={row} />,
      },
      {
        id: 'lastAction',
        header: 'آخر إجراء',
        cell: (row) => <LastAction row={row} />,
      },
      {
        id: 'linkedAt',
        header: 'تاريخ الربط',
        defaultHidden: true,
        sortable: true,
        sortKey: 'linkedAt',
        cell: (row) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(row.linkedAt)}
          </span>
        ),
      },
    ],
    [],
  )

  const buildRowMenu = (row: PersonDocumentResponse): MenuEntry[] => [
    {
      id: 'open',
      label: 'فتح الوثيقة',
      icon: <ExternalLink className="size-4" aria-hidden />,
      onSelect: () => navigate(ROUTES.DOCUMENT_DETAIL(row.documentId)),
    },
  ]

  const headerMenu: MenuEntry[] = [
    {
      id: 'refresh',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void documentsQuery.refetch(),
    },
  ]

  const toOptions = (entries: typeof rolesQuery.data, allLabel: string) => [
    { value: ALL, label: allLabel },
    ...(entries ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
  ]

  const hasQuery = Boolean(table.search || table.filters.role || table.filters.status)

  return (
    <DataTable
      tableId="person-documents"
      caption="وثائق الشخص"
      columns={columns}
      rows={documentsQuery.data?.items}
      rowKey={(row) => row.documentPersonId}
      isLoading={documentsQuery.isLoading}
      error={documentsQuery.error}
      onRetry={() => void documentsQuery.refetch()}
      onRefresh={() => void documentsQuery.refetch()}
      isFetching={documentsQuery.isFetching}
      page={table.page}
      pageSize={table.pageSize}
      totalCount={documentsQuery.data?.totalCount ?? 0}
      onPageChange={table.setPage}
      onPageSizeChange={table.setPageSize}
      search={table.search}
      onSearchChange={table.setSearch}
      searchPlaceholder="ابحث باسم الوثيقة أو الرقم…"
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      filters={
        <>
          <Select
            aria-label="تصفية حسب الصفة"
            value={table.filters.role ?? ALL}
            onValueChange={(value) => table.setFilter('role', value === ALL ? null : value)}
            options={toOptions(rolesQuery.data, 'كل الصفات')}
          />
          <Select
            aria-label="تصفية حسب الحالة"
            value={table.filters.status ?? ALL}
            onValueChange={(value) => table.setFilter('status', value === ALL ? null : value)}
            options={toOptions(statusesQuery.data, 'كل الحالات')}
          />
        </>
      }
      rowMenu={buildRowMenu}
      headerMenu={headerMenu}
      emptyMenu={headerMenu}
      rowLabel={(row) => row.title}
      emptyTitle="لا توجد وثائق مطابقة."
      emptyDescription={hasQuery ? 'جرّب تعديل البحث أو المرشحات.' : undefined}
      emptyAction={
        hasQuery ? (
          <Button type="button" variant="outline" onClick={() => table.clearQuery()}>
            مسح البحث والمرشحات
          </Button>
        ) : undefined
      }
    />
  )
}
