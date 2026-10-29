import { useMemo, useState } from 'react'
import {
  Copy,
  Eye,
  Hash,
  Pencil,
  Plus,
  RefreshCw,
  ShieldCheck,
  ShieldOff,
} from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import {
  useNumberingRegistries,
  type NumberingRegistryResponse,
} from '@/entities/numbering-registry'
import { useSessionStore } from '@/entities/user'
import {
  RegistryFormDialog,
  useSetNumberingRegistryActiveMutation,
} from '@/features/numbering-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions, NUMBERING_RESET_POLICY } from '@/shared/config'
import { formatDate, formatNumber, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  ConfirmDialog,
  DataTable,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'
import { RegistryDetailsDialog } from './RegistryDetailsDialog'

const resetPolicyLabels: Record<number, string> = {
  [NUMBERING_RESET_POLICY.Yearly]: 'سنوي',
  [NUMBERING_RESET_POLICY.Monthly]: 'شهري',
  [NUMBERING_RESET_POLICY.Never]: 'لا يتصفّر',
}

/** `/numbering` grid: registry scope, template, counters, and status actions. */
export function NumberingRegistryTable() {
  const table = useTableState({
    defaultSortKey: 'registry',
    defaultSortDirection: 'asc',
  })
  const registriesQuery = useNumberingRegistries()
  const registries = registriesQuery.data ?? []

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManage = hasPermission(Permissions.NumberingManage)

  const departmentsQuery = useDepartmentOptions()
  const documentTypesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)
  const directionsQuery = useLookupEntries(LOOKUP_SLUGS.DocumentDirections)

  const departmentNames = useMemo(
    () =>
      new Map(
        (departmentsQuery.data ?? []).map((department) => [
          department.departmentId,
          department.nameAr,
        ]),
      ),
    [departmentsQuery.data],
  )
  const documentTypeLabels = useMemo(
    () =>
      new Map(
        (documentTypesQuery.data ?? []).map((entry) => [entry.id, entry.descAr]),
      ),
    [documentTypesQuery.data],
  )
  const directionLabels = useMemo(
    () =>
      new Map(
        (directionsQuery.data ?? []).map((entry) => [entry.id, entry.descAr]),
      ),
    [directionsQuery.data],
  )

  const [formOpen, setFormOpen] = useState(false)
  const [formRegistry, setFormRegistry] = useState<NumberingRegistryResponse | null>(
    null,
  )
  const [detailsRegistryId, setDetailsRegistryId] = useState<string | null>(null)
  const [statusRegistryId, setStatusRegistryId] = useState<string | null>(null)

  const detailsRegistry =
    registries.find((registry) => registry.registryId === detailsRegistryId) ?? null
  const statusRegistry =
    registries.find((registry) => registry.registryId === statusRegistryId) ?? null

  const statusMutation = useSetNumberingRegistryActiveMutation()

  const openCreate = () => {
    setFormRegistry(null)
    setFormOpen(true)
  }

  const openEdit = (registry: NumberingRegistryResponse) => {
    setFormRegistry(registry)
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
    if (!statusRegistry) return
    try {
      await statusMutation.mutateAsync({
        registryId: statusRegistry.registryId,
        active: !statusRegistry.isActive,
      })
      toast.success(
        statusRegistry.isActive ? 'تم إيقاف سجل الترقيم.' : 'تم تفعيل سجل الترقيم.',
      )
      setStatusRegistryId(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<NumberingRegistryResponse>[]>(
    () => [
      {
        id: 'registry',
        header: 'السجل',
        hideable: false,
        sortable: true,
        sortValue: (registry) => registry.code,
        cell: (registry) => (
          <div className="flex flex-col gap-0.5">
            <code dir="ltr" className="text-xs font-semibold text-foreground">
              {registry.code}
            </code>
            <span className="text-xs text-muted">{registry.nameAr}</span>
          </div>
        ),
      },
      {
        id: 'scope',
        header: 'النطاق',
        cell: (registry) => {
          const chips = [
            registry.departmentId
              ? departmentNames.get(registry.departmentId)
              : null,
            registry.documentTypeId
              ? documentTypeLabels.get(registry.documentTypeId)
              : null,
            registry.directionId ? directionLabels.get(registry.directionId) : null,
          ].filter((label): label is string => Boolean(label))

          return chips.length === 0 ? (
            <Badge variant="outline">شامل</Badge>
          ) : (
            <div className="flex max-w-72 flex-wrap gap-1">
              {chips.map((label) => (
                <Badge key={label} variant="outline">
                  {label}
                </Badge>
              ))}
            </div>
          )
        },
      },
      {
        id: 'format',
        header: 'قالب الرقم',
        cell: (registry) => (
          <code dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {registry.formatPattern}
          </code>
        ),
      },
      {
        id: 'resetPolicy',
        header: 'التصفير',
        cell: (registry) => (
          <span className="text-xs text-foreground">
            {resetPolicyLabels[registry.resetPolicyId] ?? `#${registry.resetPolicyId}`}
          </span>
        ),
      },
      {
        id: 'counters',
        header: 'العدادات',
        sortable: true,
        sortValue: (registry) => registry.counters.length,
        cell: (registry) => {
          const latest = registry.counters[0]

          if (!latest) {
            return <span className="text-xs text-muted">لم يُصدر رقم</span>
          }

          return (
            <div className="flex flex-col gap-0.5 text-xs">
              <span className="text-foreground">
                <span dir="ltr">{latest.numberingYear}</span> ·{' '}
                <span dir="ltr">{formatNumber(latest.lastValue)}</span>
              </span>
              <span className="text-muted">{registry.counters.length} سنوات</span>
            </div>
          )
        },
      },
      {
        id: 'status',
        header: 'الحالة',
        cell: (registry) => (
          <Badge variant={registry.isActive ? 'success' : 'danger'}>
            {registry.isActive ? 'نشط' : 'موقوف'}
          </Badge>
        ),
      },
      {
        id: 'createdAt',
        header: 'تاريخ الإنشاء',
        defaultHidden: true,
        sortable: true,
        sortValue: (registry) => registry.createdAt,
        cell: (registry) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(registry.createdAt)}
          </span>
        ),
      },
    ],
    // Label maps are dependencies of the scope column.
    [departmentNames, documentTypeLabels, directionLabels],
  )

  const buildRowMenu = (registry: NumberingRegistryResponse): MenuEntry[] => {
    const entries: MenuEntry[] = [
      {
        id: 'details',
        label: 'عرض التفاصيل',
        icon: <Eye className="size-4" aria-hidden />,
        onSelect: () => setDetailsRegistryId(registry.registryId),
      },
    ]

    if (canManage) {
      entries.push(
        {
          id: 'edit',
          label: 'تعديل السجل',
          icon: <Pencil className="size-4" aria-hidden />,
          onSelect: () => openEdit(registry),
        },
        {
          id: 'toggle-active',
          label: registry.isActive ? 'إيقاف السجل' : 'تفعيل السجل',
          icon: registry.isActive ? (
            <ShieldOff className="size-4" aria-hidden />
          ) : (
            <ShieldCheck className="size-4" aria-hidden />
          ),
          danger: registry.isActive,
          onSelect: () => setStatusRegistryId(registry.registryId),
        },
      )
    }

    entries.push(
      { type: 'separator' },
      {
        id: 'copy-code',
        label: 'نسخ رمز السجل',
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(registry.code, 'رمز السجل'),
      },
      {
        id: 'copy-id',
        label: 'نسخ المعرّف',
        icon: <Hash className="size-4" aria-hidden />,
        onSelect: () => void copyValue(registry.registryId, 'المعرّف'),
      },
    )

    return entries
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: 'تحديث البيانات',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void registriesQuery.refetch(),
  }
  const createEntry: MenuEntry = {
    id: 'create',
    label: 'سجل ترقيم جديد',
    icon: <Plus className="size-4" aria-hidden />,
    onSelect: openCreate,
  }
  const headerMenu: MenuEntry[] = canManage
    ? [createEntry, refreshEntry]
    : [refreshEntry]

  return (
    <>
      <DataTable
        tableId="numbering-registries"
        mode="client"
        caption="سجلات الترقيم"
        columns={columns}
        rows={registriesQuery.data}
        rowKey={(registry) => registry.registryId}
        rowSearchText={(registry) =>
          [
            registry.code,
            registry.nameAr,
            registry.formatPattern,
            registry.departmentId ? departmentNames.get(registry.departmentId) : null,
            registry.documentTypeId
              ? documentTypeLabels.get(registry.documentTypeId)
              : null,
            registry.directionId ? directionLabels.get(registry.directionId) : null,
          ]
            .filter(Boolean)
            .join(' ')
            .toLocaleLowerCase('ar')
        }
        isLoading={registriesQuery.isLoading}
        error={registriesQuery.error}
        onRetry={() => void registriesQuery.refetch()}
        onRefresh={() => void registriesQuery.refetch()}
        isFetching={registriesQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={registries.length}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        searchPlaceholder="ابحث بالرمز أو الاسم أو النطاق…"
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(registry) => registry.code}
        toolbar={
          canManage ? (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              سجل جديد
            </Button>
          ) : undefined
        }
        emptyTitle={
          table.search ? 'لا توجد سجلات مطابقة.' : 'لا توجد سجلات ترقيم بعد.'
        }
        emptyDescription={
          table.search
            ? 'جرّب تعديل عبارة البحث أو امسحها لعرض كل السجلات.'
            : canManage
              ? 'أنشئ سجلًا للترقيم ثم اربطه بدائرة أو نوع أو اتجاه، أو اتركه سجلًا شاملًا.'
              : 'تُدار سجلات الترقيم من قبل حاملي صلاحية إدارة الترقيم.'
        }
        emptyAction={
          table.search ? (
            <Button
              type="button"
              variant="outline"
              onClick={() => table.setSearch('')}
            >
              مسح البحث
            </Button>
          ) : canManage ? (
            <Button type="button" onClick={openCreate}>
              <Plus className="size-4" aria-hidden />
              إنشاء سجل
            </Button>
          ) : undefined
        }
      />

      {formOpen && (
        <RegistryFormDialog
          key={formRegistry?.registryId ?? 'create'}
          open
          registry={formRegistry}
          onOpenChange={setFormOpen}
        />
      )}

      {detailsRegistry && (
        <RegistryDetailsDialog
          open
          registry={detailsRegistry}
          departmentName={
            detailsRegistry.departmentId
              ? departmentNames.get(detailsRegistry.departmentId)
              : null
          }
          documentTypeLabel={
            detailsRegistry.documentTypeId
              ? documentTypeLabels.get(detailsRegistry.documentTypeId)
              : null
          }
          directionLabel={
            detailsRegistry.directionId
              ? directionLabels.get(detailsRegistry.directionId)
              : null
          }
          onOpenChange={(open) => {
            if (!open) setDetailsRegistryId(null)
          }}
        />
      )}

      <ConfirmDialog
        open={statusRegistry !== null}
        onOpenChange={(open) => {
          if (!open) setStatusRegistryId(null)
        }}
        title={statusRegistry?.isActive ? 'إيقاف سجل الترقيم' : 'تفعيل سجل الترقيم'}
        description={
          statusRegistry
            ? statusRegistry.isActive
              ? `سيُعلَّم السجل «${statusRegistry.code}» كموقوف، ولن يُختار لإصدار أرقام جديدة. تبقى الأرقام الصادرة سابقًا كما هي.`
              : `سيعود السجل «${statusRegistry.code}» نشطًا ومتاحًا لإصدار الأرقام.`
            : undefined
        }
        confirmLabel={statusRegistry?.isActive ? 'إيقاف السجل' : 'تفعيل السجل'}
        destructive={statusRegistry?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
