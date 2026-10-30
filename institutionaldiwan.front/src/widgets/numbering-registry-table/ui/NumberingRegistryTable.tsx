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
  findDuplicateActiveScopes,
  registryScopeRank,
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
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'
import { RegistryDetailsDialog } from './RegistryDetailsDialog'

const resetPolicyLabels: Record<number, string> = {
  [NUMBERING_RESET_POLICY.Yearly]: 'سنوي',
  [NUMBERING_RESET_POLICY.Monthly]: 'شهري',
  [NUMBERING_RESET_POLICY.Never]: 'لا يتصفّر',
}

const statusFilterOptions = [
  { value: 'all', label: 'كل الحالات' },
  { value: 'active', label: 'نشط' },
  { value: 'inactive', label: 'موقوف' },
]

const scopeFilterOptions = [
  { value: 'all', label: 'كل النطاقات' },
  { value: 'global', label: 'شامل (بلا قيود)' },
  { value: 'department', label: 'مقيّد بدائرة' },
  { value: 'documentType', label: 'مقيّد بنوع وثيقة' },
  { value: 'direction', label: 'مقيّد باتجاه' },
]

/** `/numbering` grid: registry scope, template, counters, and status actions. */
export function NumberingRegistryTable() {
  const table = useTableState({
    defaultSortKey: 'registry',
    defaultSortDirection: 'asc',
    filterKeys: ['status', 'resetPolicyId', 'scope'],
  })
  const registriesQuery = useNumberingRegistries()
  const registries = useMemo(() => registriesQuery.data ?? [], [registriesQuery.data])

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canManage = hasPermission(Permissions.NumberingManage)

  const departmentsQuery = useDepartmentOptions()
  const documentTypesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)
  const directionsQuery = useLookupEntries(LOOKUP_SLUGS.DocumentDirections)
  const resetPoliciesQuery = useLookupEntries(LOOKUP_SLUGS.NumberingResetPolicies)

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

  const resetPolicyOptions = useMemo(
    () => [
      { value: 'all', label: 'كل سياسات التصفير' },
      ...(resetPoliciesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [resetPoliciesQuery.data],
  )

  const filteredRegistries = useMemo(
    () =>
      registries.filter((registry) => {
        const { status, resetPolicyId, scope } = table.filters
        if (status === 'active' && !registry.isActive) return false
        if (status === 'inactive' && registry.isActive) return false
        if (resetPolicyId && String(registry.resetPolicyId) !== resetPolicyId) return false
        if (scope === 'global' && registryScopeRank(registry) !== 0) return false
        if (scope === 'department' && registry.departmentId === null) return false
        if (scope === 'documentType' && registry.documentTypeId === null) return false
        if (scope === 'direction' && registry.directionId === null) return false
        return true
      }),
    [registries, table.filters],
  )

  const duplicateGroups = useMemo(
    () => findDuplicateActiveScopes(registries),
    [registries],
  )

  const duplicateCodesFor = (registry: NumberingRegistryResponse): string[] => {
    const group = duplicateGroups.find((candidate) =>
      candidate.registries.some((member) => member.registryId === registry.registryId),
    )
    return (
      group?.registries
        .filter((member) => member.registryId !== registry.registryId)
        .map((member) => member.code) ?? []
    )
  }

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
        id: 'specificity',
        header: 'أبعاد النطاق',
        sortable: true,
        sortValue: (registry) => registryScopeRank(registry),
        cell: (registry) => {
          const rank = registryScopeRank(registry)
          return rank === 0 ? (
            <Badge variant="outline">شامل</Badge>
          ) : (
            <span className="whitespace-nowrap text-xs text-muted">
              {formatNumber(rank)} من 3
            </span>
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

  const hasQuery = Boolean(table.search) || table.hasActiveFilters

  return (
    <>
      {duplicateGroups.length > 0 && (
        <div
          role="status"
          className="flex flex-col gap-1 rounded-lg border border-warning/40 bg-warning/10 px-3 py-2 text-xs text-warning"
        >
          <p className="font-semibold">تنبيه: نطاق مزدوج بين سجلات نشطة</p>
          {duplicateGroups.map((group) => (
            <p key={group.key}>
              {group.registries.map((member) => member.code).join('، ')} — نطاق متطابق تمامًا
            </p>
          ))}
          <p>عند تطابق أكثر من سجل نشط قد يختار الخادم أحدها فقط؛ خصّص نطاق أحدها أو أوقفه.</p>
        </div>
      )}

      <DataTable
        tableId="numbering-registries"
        mode="client"
        caption="سجلات الترقيم"
        columns={columns}
        rows={filteredRegistries}
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
        totalCount={filteredRegistries.length}
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
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <Select
              aria-label="تصفية حسب الحالة"
              value={table.filters.status || 'all'}
              onValueChange={(value) =>
                table.setFilter('status', value === 'all' ? null : value)
              }
              options={statusFilterOptions}
              className="w-40"
            />
            <Select
              aria-label="تصفية حسب سياسة التصفير"
              value={table.filters.resetPolicyId || 'all'}
              onValueChange={(value) =>
                table.setFilter('resetPolicyId', value === 'all' ? null : value)
              }
              options={resetPolicyOptions}
              className="w-44"
            />
            <Select
              aria-label="تصفية حسب النطاق"
              value={table.filters.scope || 'all'}
              onValueChange={(value) =>
                table.setFilter('scope', value === 'all' ? null : value)
              }
              options={scopeFilterOptions}
              className="w-48"
            />
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                مسح المرشحات
              </Button>
            )}
          </div>
        }
        emptyTitle={
          hasQuery ? 'لا توجد سجلات مطابقة.' : 'لا توجد سجلات ترقيم بعد.'
        }
        emptyDescription={
          hasQuery
            ? 'جرّب تعديل البحث أو المرشحات لعرض سجلات أخرى.'
            : canManage
              ? 'أنشئ سجلًا للترقيم ثم اربطه بدائرة أو نوع أو اتجاه، أو اتركه سجلًا شاملًا.'
              : 'تُدار سجلات الترقيم من قبل حاملي صلاحية إدارة الترقيم.'
        }
        emptyAction={
          hasQuery ? (
            <Button
              type="button"
              variant="outline"
              onClick={table.clearQuery}
            >
              مسح البحث والمرشحات
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
          duplicateCodes={duplicateCodesFor(detailsRegistry)}
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
