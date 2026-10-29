import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Copy, Eye, Hash, Plus, RefreshCw } from 'lucide-react'
import { toast } from 'sonner'
import { DepartmentBadge, useDepartmentOptions } from '@/entities/department'
import {
  DocumentDirectionBadge,
  DocumentStatusBadge,
  OfficialNumberTag,
  useDocuments,
  type DocumentResponse,
} from '@/entities/document'
import {
  DOCUMENT_DIRECTION,
  LOOKUP_SLUGS,
  LookupLabel,
  useLookupEntries,
} from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import { CreateDocumentDialog } from '@/features/document-create'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions, ROUTES } from '@/shared/config'
import { formatDate, formatDateTime, formatShortId, useTableState } from '@/shared/lib'
import {
  Button,
  DataTable,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

function optionalId(value: string | undefined): number | undefined {
  if (!value) return undefined
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined
}

/** Module 03 correspondence registry: server search/paging, filters, row menus. */
export function DocumentRegistryTable() {
  const table = useTableState({
    defaultSortKey: 'createdAt',
    defaultSortDirection: 'desc',
    filterKeys: ['statusId', 'directionId', 'documentTypeId', 'owningDepartmentId'],
  })

  const documentsQuery = useDocuments({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    sortBy: table.sortKey ?? 'createdAt',
    sortDescending: table.sortDirection === 'desc',
    statusId: optionalId(table.filters.statusId),
    directionId: optionalId(table.filters.directionId),
    documentTypeId: optionalId(table.filters.documentTypeId),
    owningDepartmentId: table.filters.owningDepartmentId || undefined,
  })

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canCreate = hasPermission(Permissions.DocumentCreate)
  const navigate = useNavigate()

  const departmentsQuery = useDepartmentOptions()
  const statusQuery = useLookupEntries(LOOKUP_SLUGS.DocumentStatusTypes)
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)
  const directionsQuery = useLookupEntries(LOOKUP_SLUGS.DocumentDirections)

  const departmentById = useMemo(
    () =>
      new Map(
        (departmentsQuery.data ?? []).map((department) => [
          department.departmentId,
          department,
        ]),
      ),
    [departmentsQuery.data],
  )

  const statusOptions = useMemo(
    () => [
      { value: 'all', label: 'كل الحالات' },
      ...(statusQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [statusQuery.data],
  )

  const typeOptions = useMemo(
    () => [
      { value: 'all', label: 'كل الأنواع' },
      ...(typesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [typesQuery.data],
  )

  const directionOptions = useMemo(
    () => [
      { value: 'all', label: 'كل الاتجاهات' },
      ...(directionsQuery.data ?? [])
        .filter((entry) => entry.id !== DOCUMENT_DIRECTION.All)
        .map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    ],
    [directionsQuery.data],
  )

  const departmentOptions = useMemo(
    () => [
      { value: 'all', label: 'كل الأقسام' },
      ...(departmentsQuery.data ?? []).map((department) => ({
        value: department.departmentId,
        label: department.nameAr,
      })),
    ],
    [departmentsQuery.data],
  )

  const [createOpen, setCreateOpen] = useState(false)

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(`تم نسخ ${label}.`)
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const columns = useMemo<DataTableColumn<DocumentResponse>[]>(
    () => [
      {
        id: 'title',
        header: 'عنوان الوثيقة',
        sortable: true,
        sortKey: 'title',
        hideable: false,
        cell: (document) => (
          <div className="flex min-w-52 flex-col gap-0.5">
            <Link
              to={ROUTES.DOCUMENT_DETAIL(document.documentId)}
              className="text-sm font-medium text-foreground hover:text-accent hover:underline"
            >
              {document.title}
            </Link>
            {document.subjectSummary && (
              <span className="line-clamp-2 text-xs text-muted">
                {document.subjectSummary}
              </span>
            )}
          </div>
        ),
      },
      {
        id: 'officialNumber',
        header: 'الرقم الرسمي',
        sortable: true,
        sortKey: 'officialNumber',
        cell: (document) => (
          <OfficialNumberTag officialNumberText={document.officialNumberText} />
        ),
      },
      {
        id: 'direction',
        header: 'الاتجاه',
        cell: (document) => <DocumentDirectionBadge directionId={document.directionId} />,
      },
      {
        id: 'documentType',
        header: 'النوع',
        cell: (document) => (
          <LookupLabel lookup={LOOKUP_SLUGS.DocumentTypes} id={document.documentTypeId} />
        ),
      },
      {
        id: 'status',
        header: 'الحالة',
        cell: (document) => <DocumentStatusBadge statusId={document.statusId} />,
      },
      {
        id: 'department',
        header: 'القسم المالك',
        cell: (document) => {
          const department = departmentById.get(document.owningDepartmentId)
          return department ? (
            <DepartmentBadge
              nameAr={department.nameAr}
              code={department.code}
              isActive={department.isActive}
            />
          ) : (
            <span
              dir="ltr"
              title={document.owningDepartmentId}
              className="font-mono text-xs text-muted"
            >
              {formatShortId(document.owningDepartmentId)}
            </span>
          )
        },
      },
      {
        id: 'documentDate',
        header: 'تاريخ الوثيقة',
        cell: (document) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {formatDate(document.documentDate)}
          </span>
        ),
      },
      {
        id: 'registrationDate',
        header: 'تاريخ التسجيل',
        sortable: true,
        sortKey: 'registrationDate',
        cell: (document) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(document.registrationDate)}
          </span>
        ),
      },
      {
        id: 'confidentiality',
        header: 'السرية',
        defaultHidden: true,
        cell: (document) => (
          <LookupLabel
            lookup={LOOKUP_SLUGS.DocumentConfidentialityLevels}
            id={document.confidentialityLevelId}
          />
        ),
      },
      {
        id: 'priority',
        header: 'الأولوية',
        defaultHidden: true,
        cell: (document) => (
          <LookupLabel
            lookup={LOOKUP_SLUGS.DocumentPriorityLevels}
            id={document.priorityLevelId}
          />
        ),
      },
      {
        id: 'createdAt',
        header: 'تاريخ الإنشاء',
        sortable: true,
        sortKey: 'createdAt',
        defaultHidden: true,
        cell: (document) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(document.createdAt)}
          </span>
        ),
      },
    ],
    [departmentById],
  )

  const buildRowMenu = (document: DocumentResponse): MenuEntry[] => [
    {
      id: 'open',
      label: 'فتح التفاصيل',
      icon: <Eye className="size-4" aria-hidden />,
      onSelect: () => navigate(ROUTES.DOCUMENT_DETAIL(document.documentId)),
    },
    {
      id: 'copy-number',
      label: 'نسخ الرقم الرسمي',
      icon: <Hash className="size-4" aria-hidden />,
      disabled: !document.officialNumberText,
      onSelect: () =>
        void copyValue(document.officialNumberText ?? '', 'الرقم الرسمي'),
    },
    {
      id: 'copy-id',
      label: 'نسخ معرّف الوثيقة',
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyValue(document.documentId, 'معرّف الوثيقة'),
    },
  ]

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: 'تحديث البيانات',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void documentsQuery.refetch(),
  }

  const createEntry: MenuEntry = {
    id: 'create',
    label: 'وثيقة جديدة',
    icon: <Plus className="size-4" aria-hidden />,
    onSelect: () => setCreateOpen(true),
  }

  const headerMenu: MenuEntry[] = canCreate ? [createEntry, refreshEntry] : [refreshEntry]

  const hasQuery = Boolean(table.search) || table.hasActiveFilters

  return (
    <>
      <DataTable
        tableId="documents"
        caption="سجل الوثائق"
        columns={columns}
        rows={documentsQuery.data?.items}
        rowKey={(document) => document.documentId}
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
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        searchPlaceholder="ابحث بالعنوان أو الملخص أو الرقم الرسمي…"
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <Select
              aria-label="تصفية حسب الحالة"
              value={table.filters.statusId || 'all'}
              onValueChange={(value) =>
                table.setFilter('statusId', value === 'all' ? null : value)
              }
              options={statusOptions}
              className="w-40"
            />
            <Select
              aria-label="تصفية حسب الاتجاه"
              value={table.filters.directionId || 'all'}
              onValueChange={(value) =>
                table.setFilter('directionId', value === 'all' ? null : value)
              }
              options={directionOptions}
              className="w-36"
            />
            <Select
              aria-label="تصفية حسب النوع"
              value={table.filters.documentTypeId || 'all'}
              onValueChange={(value) =>
                table.setFilter('documentTypeId', value === 'all' ? null : value)
              }
              options={typeOptions}
              className="w-40"
            />
            <Select
              aria-label="تصفية حسب القسم المالك"
              value={table.filters.owningDepartmentId || 'all'}
              onValueChange={(value) =>
                table.setFilter('owningDepartmentId', value === 'all' ? null : value)
              }
              options={departmentOptions}
              disabled={departmentsQuery.isLoading}
              className="w-52"
            />
            {departmentsQuery.isError && (
              <span className="flex items-center gap-2 text-xs text-danger">
                {toArabicErrorMessage(departmentsQuery.error)}
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => void departmentsQuery.refetch()}
                >
                  إعادة التحميل
                </Button>
              </span>
            )}
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                مسح المرشحات
              </Button>
            )}
          </div>
        }
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(document) => document.title}
        toolbar={
          canCreate ? (
            <Button type="button" onClick={() => setCreateOpen(true)}>
              <Plus className="size-4" aria-hidden />
              وثيقة جديدة
            </Button>
          ) : undefined
        }
        emptyTitle={hasQuery ? 'لا توجد وثائق مطابقة.' : 'لا توجد وثائق بعد.'}
        emptyDescription={
          hasQuery
            ? 'جرّب تعديل البحث أو المرشحات لعرض وثائق أخرى.'
            : canCreate
              ? 'ابدأ بحفظ أول وثيقة كمسودة أو تسجيلها برقم رسمي.'
              : 'ستظهر الوثائق هنا بمجرد تسجيلها في أقسام المؤسسة.'
        }
        emptyAction={
          hasQuery ? (
            <Button type="button" variant="outline" onClick={table.clearQuery}>
              مسح البحث والمرشحات
            </Button>
          ) : canCreate ? (
            <Button type="button" onClick={() => setCreateOpen(true)}>
              <Plus className="size-4" aria-hidden />
              إنشاء وثيقة
            </Button>
          ) : undefined
        }
      />

      {createOpen && (
        <CreateDocumentDialog open onOpenChange={setCreateOpen} />
      )}
    </>
  )
}
