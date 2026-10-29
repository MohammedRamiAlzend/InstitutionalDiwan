import { useMemo, useState } from 'react'
import { Copy, Hash, Pencil, Plus, RefreshCw, UserCheck, UserX } from 'lucide-react'
import { toast } from 'sonner'
import {
  useExternalOrganizations,
  type ExternalOrganizationResponse,
} from '@/entities/external-organization'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { useSessionStore } from '@/entities/user'
import {
  ExternalOrganizationFormDialog,
  useSetExternalOrganizationActiveMutation,
} from '@/features/external-organization-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions } from '@/shared/config'
import { formatDateTime, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  ConfirmDialog,
  DataTable,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

const INCLUDE_INACTIVE_OPTIONS = [
  { value: 'active', label: 'الفعّالة فقط' },
  { value: 'all', label: 'الفعّالة والموقوفة' },
]

/** External correspondents administration grid (server search + paging). */
export function ExternalOrganizationsTable() {
  const table = useTableState({ filterKeys: ['include'] })
  const includeInactive = table.filters.include === 'all'

  const organizationsQuery = useExternalOrganizations({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    includeInactive,
  })

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canEdit = hasPermission(Permissions.DocumentEdit)

  const [formOpen, setFormOpen] = useState(false)
  const [formOrganization, setFormOrganization] =
    useState<ExternalOrganizationResponse | null>(null)
  const [statusOrganization, setStatusOrganization] =
    useState<ExternalOrganizationResponse | null>(null)

  const statusMutation = useSetExternalOrganizationActiveMutation()

  const openCreate = () => {
    setFormOrganization(null)
    setFormOpen(true)
  }

  const openEdit = (organization: ExternalOrganizationResponse) => {
    setFormOrganization(organization)
    setFormOpen(true)
  }

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(`تم نسخ ${label}.`)
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const handleToggleStatus = async () => {
    if (!statusOrganization) return
    try {
      await statusMutation.mutateAsync({
        externalOrganizationId: statusOrganization.externalOrganizationId,
        active: !statusOrganization.isActive,
      })
      toast.success(
        statusOrganization.isActive
          ? 'تم إيقاف الجهة الخارجية.'
          : 'تم تفعيل الجهة الخارجية.',
      )
      setStatusOrganization(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<ExternalOrganizationResponse>[]>(
    () => [
      {
        id: 'code',
        header: 'الرمز',
        hideable: false,
        cell: (organization) => (
          <span dir="ltr" className="font-mono text-xs text-foreground">
            {organization.code}
          </span>
        ),
      },
      {
        id: 'organization',
        header: 'الجهة',
        hideable: false,
        cell: (organization) => (
          <div className="flex flex-col gap-0.5">
            <span className="text-sm font-medium text-foreground">
              {organization.nameAr}
            </span>
            {organization.nameEn && (
              <span dir="ltr" className="text-xs text-muted">
                {organization.nameEn}
              </span>
            )}
          </div>
        ),
      },
      {
        id: 'type',
        header: 'النوع',
        cell: (organization) => (
          <LookupLabel
            lookup={LOOKUP_SLUGS.OrganizationTypes}
            id={organization.organizationTypeId}
            className="text-xs"
          />
        ),
      },
      {
        id: 'contact',
        header: 'بيانات الاتصال',
        defaultHidden: true,
        cell: (organization) =>
          organization.contactInfo ? (
            <span
              dir="ltr"
              title={organization.contactInfo}
              className="block max-w-64 truncate text-xs text-foreground"
            >
              {organization.contactInfo}
            </span>
          ) : (
            <span className="text-muted">—</span>
          ),
      },
      {
        id: 'status',
        header: 'الحالة',
        cell: (organization) => (
          <Badge variant={organization.isActive ? 'success' : 'danger'}>
            {organization.isActive ? 'نشط' : 'موقوف'}
          </Badge>
        ),
      },
      {
        id: 'updatedAt',
        header: 'آخر تحديث',
        defaultHidden: true,
        cell: (organization) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {organization.updatedAt
              ? formatDateTime(organization.updatedAt)
              : '—'}
          </span>
        ),
      },
      {
        id: 'createdAt',
        header: 'تاريخ الإنشاء',
        defaultHidden: true,
        cell: (organization) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(organization.createdAt)}
          </span>
        ),
      },
    ],
    [],
  )

  const buildRowMenu = (organization: ExternalOrganizationResponse): MenuEntry[] => {
    const menu: MenuEntry[] = []

    if (canEdit) {
      menu.push(
        {
          id: 'edit',
          label: 'تعديل البيانات',
          icon: <Pencil className="size-4" aria-hidden />,
          onSelect: () => openEdit(organization),
        },
        { type: 'separator' },
        {
          id: 'toggle-active',
          label: organization.isActive ? 'إيقاف الجهة' : 'تفعيل الجهة',
          icon: organization.isActive ? (
            <UserX className="size-4" aria-hidden />
          ) : (
            <UserCheck className="size-4" aria-hidden />
          ),
          danger: organization.isActive,
          onSelect: () => setStatusOrganization(organization),
        },
        { type: 'separator' },
      )
    }

    menu.push(
      {
        id: 'copy-code',
        label: 'نسخ الرمز',
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(organization.code, 'الرمز'),
      },
      {
        id: 'copy-id',
        label: 'نسخ المعرّف',
        icon: <Hash className="size-4" aria-hidden />,
        onSelect: () => void copyValue(organization.externalOrganizationId, 'المعرّف'),
      },
    )

    return menu
  }

  const headerMenu: MenuEntry[] = [
    ...(canEdit
      ? [
          {
            id: 'create',
            label: 'جهة خارجية جديدة',
            icon: <Plus className="size-4" aria-hidden />,
            onSelect: openCreate,
          } satisfies MenuEntry,
          { type: 'separator' } satisfies MenuEntry,
        ]
      : []),
    {
      id: 'refresh',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void organizationsQuery.refetch(),
    },
  ]

  return (
    <>
      <DataTable
        tableId="external-organizations-table"
        caption="قائمة الجهات الخارجية"
        columns={columns}
        rows={organizationsQuery.data?.items}
        rowKey={(organization) => organization.externalOrganizationId}
        isLoading={organizationsQuery.isLoading}
        error={organizationsQuery.error}
        onRetry={() => void organizationsQuery.refetch()}
        onRefresh={() => void organizationsQuery.refetch()}
        isFetching={organizationsQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={organizationsQuery.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        filters={
          <Select
            aria-label="تصفية حسب الحالة"
            value={table.filters.include === 'all' ? 'all' : 'active'}
            onValueChange={(value) =>
              table.setFilter('include', value === 'all' ? 'all' : null)
            }
            options={INCLUDE_INACTIVE_OPTIONS}
          />
        }
        searchPlaceholder="ابحث برمز الجهة أو اسمها…"
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(organization) => organization.nameAr}
        toolbar={
          canEdit ? (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              جهة خارجية جديدة
            </Button>
          ) : undefined
        }
        emptyTitle={
          table.search || includeInactive
            ? 'لا توجد نتائج مطابقة.'
            : 'لا توجد جهات خارجية بعد.'
        }
        emptyDescription={
          table.search || includeInactive
            ? 'جرّب تعديل عبارة البحث أو المرشح لعرض سجلات أخرى.'
            : 'ابدأ بإضافة أول جهة خارجية لتظهر في اختيار جهات المشاركين.'
        }
        emptyAction={
          table.search || includeInactive ? (
            <Button
              type="button"
              variant="outline"
              onClick={() => table.clearQuery()}
            >
              مسح البحث والمرشحات
            </Button>
          ) : canEdit ? (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              إضافة جهة
            </Button>
          ) : undefined
        }
      />

      {formOpen && (
        <ExternalOrganizationFormDialog
          key={formOrganization?.externalOrganizationId ?? 'create'}
          open
          onOpenChange={setFormOpen}
          organization={formOrganization}
        />
      )}

      <ConfirmDialog
        open={statusOrganization !== null}
        onOpenChange={(open) => {
          if (!open) setStatusOrganization(null)
        }}
        title={statusOrganization?.isActive ? 'إيقاف الجهة الخارجية' : 'تفعيل الجهة الخارجية'}
        description={
          statusOrganization
            ? statusOrganization.isActive
              ? `ستُعلَّم الجهة «${statusOrganization.nameAr}» كموقوفة، ولن تظهر بعدها في اختيار جهات المشاركين.`
              : `ستعود الجهة «${statusOrganization.nameAr}» فعّالة ومتاحة للاختيار في المشاركين.`
            : undefined
        }
        confirmLabel={statusOrganization?.isActive ? 'إيقاف الجهة' : 'تفعيل الجهة'}
        destructive={statusOrganization?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
