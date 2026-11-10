import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Pencil, Plus, RefreshCw, Rocket, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import {
  FIELD_OPTION_SOURCE,
  FieldTypeBadge,
  SCHEMA_STATUS,
  SchemaStatusBadge,
  useDocumentTypeFields,
  useSchemaVersion,
  useSchemaVersions,
  type SchemaFieldResponse,
  type SchemaVersionSummaryResponse,
} from '@/entities/document-schema'
import { useDepartmentOptions } from '@/entities/department'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import {
  DeleteDraftDialog,
  FieldSettingsDialog,
  PublishVersionDialog,
  RemoveFieldDialog,
  useCreateDraftMutation,
} from '@/features/document-schema-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { cn, formatDate, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  GuideRegion,
  Select,
  type DataTableColumn,
  type MenuEntry,
  type SelectOption,
} from '@/shared/ui'

/** The form selector's value for the creation form (no department). */
const CREATION_FORM = 'creation'

/** Latest version first; the server already sorts, so the first row is the default selection. */
function pickVersionId(
  versions: SchemaVersionSummaryResponse[] | undefined,
  requested: string | null,
): string | undefined {
  if (!versions?.length) return undefined
  return versions.find((v) => v.schemaVersionId === requested)?.schemaVersionId ??
    versions[0].schemaVersionId
}

function constraintsOf(field: SchemaFieldResponse): string {
  const parts: string[] = []
  if (field.maxLength !== null) parts.push(`الطول ≤ ${field.maxLength}`)
  if (field.minNumber !== null) parts.push(`من ${field.minNumber}`)
  if (field.maxNumber !== null) parts.push(`إلى ${field.maxNumber}`)
  if (field.options.length > 0) parts.push(`${field.options.length} خيارًا`)
  if (field.regexPattern) parts.push('نمط Regex')
  const { behavior } = field
  if (behavior.minLength !== null) parts.push(`الطول ≥ ${behavior.minLength}`)
  if (behavior.minDate || behavior.maxDate) parts.push('حدود تاريخ')
  if (behavior.isReadOnly) parts.push('للقراءة فقط')
  else if (behavior.defaultValue) parts.push('قيمة افتراضية')
  if (behavior.visibleWhenFieldKey) parts.push('مشروط')
  if (behavior.isFilterable) parts.push('قابل للتصفية')
  if (behavior.showInTable) parts.push('عمود')
  if (behavior.optionSource !== FIELD_OPTION_SOURCE.Static) parts.push('خيارات مرتبطة')
  return parts.join(' · ') || '—'
}

/**
 * Administration view of one form of a document type: the creation form, or a department's record
 * form (`?dept=`) that the department fills when it receives a document of the type. The version
 * history sits on the side and the selected version's fields form a grid. The document type
 * (`?type=`), form (`?dept=`) and version (`?version=`) live in the URL so a link reopens the view.
 */
export function DocumentSchemaEditor() {
  const [searchParams, setSearchParams] = useSearchParams()
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)
  const table = useTableState({ filterKeys: [] })

  const typeParam = searchParams.get('type')
  const documentTypeId = typeParam && Number.isInteger(Number(typeParam)) ? Number(typeParam) : undefined
  // `?dept=` picks a department's record form; without it the page edits the creation form.
  const departmentId = searchParams.get('dept')
  const departmentsQuery = useDepartmentOptions()

  const versionsQuery = useSchemaVersions(documentTypeId, { departmentId })
  const selectedVersionId = pickVersionId(versionsQuery.data, searchParams.get('version'))
  const versionQuery = useSchemaVersion(selectedVersionId)
  const definedFieldsQuery = useDocumentTypeFields(documentTypeId, { departmentId })
  const createDraft = useCreateDraftMutation(documentTypeId ?? 0, departmentId)

  const [dialog, setDialog] = useState<
    | { type: 'add' }
    | { type: 'edit' | 'remove'; field: SchemaFieldResponse }
    | { type: 'publish' | 'delete' }
    | null
  >(null)

  const typeOptions = useMemo<SelectOption[]>(
    () => (typesQuery.data ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    [typesQuery.data],
  )

  const formOptions = useMemo<SelectOption[]>(
    () => [
      { value: CREATION_FORM, label: 'نموذج الإنشاء' },
      ...(departmentsQuery.data ?? [])
        .filter((department) => department.isActive || department.departmentId === departmentId)
        .map((department) => ({
          value: department.departmentId,
          label: `سجل ديوان: ${department.nameAr}`,
        })),
    ],
    [departmentsQuery.data, departmentId],
  )

  const update = (next: Record<string, string | null>) => {
    const params = new URLSearchParams(searchParams)
    for (const [key, value] of Object.entries(next)) {
      if (value === null) params.delete(key)
      else params.set(key, value)
    }
    setSearchParams(params, { replace: true })
  }

  const columns = useMemo<DataTableColumn<SchemaFieldResponse>[]>(
    () => [
      {
        id: 'displayOrder',
        header: 'الترتيب',
        sortable: true,
        sortValue: (field) => field.displayOrder,
        cell: (field) => <span className="text-xs text-muted">{field.displayOrder}</span>,
      },
      {
        id: 'labelAr',
        header: 'العنوان',
        hideable: false,
        sortable: true,
        sortValue: (field) => field.labelAr,
        cell: (field) => <span className="font-medium text-foreground">{field.labelAr}</span>,
      },
      {
        id: 'key',
        header: 'المفتاح',
        sortable: true,
        sortValue: (field) => field.key,
        cell: (field) => (
          <code dir="ltr" className="text-xs text-muted">
            {field.key}
          </code>
        ),
      },
      {
        id: 'dataType',
        header: 'النوع',
        sortable: true,
        sortValue: (field) => field.dataType,
        cell: (field) => <FieldTypeBadge dataType={field.dataType} />,
      },
      {
        id: 'required',
        header: 'إجباري',
        sortable: true,
        sortValue: (field) => (field.isRequired ? 1 : 0),
        cell: (field) =>
          field.isRequired ? (
            <Badge variant="accent">إجباري</Badge>
          ) : (
            <span className="text-xs text-muted">—</span>
          ),
      },
      {
        id: 'constraints',
        header: 'القيود',
        cell: (field) => <span className="text-xs text-muted">{constraintsOf(field)}</span>,
      },
    ],
    [],
  )

  const refresh = () => {
    void versionsQuery.refetch()
    void versionQuery.refetch()
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: 'تحديث المخطط',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: refresh,
  }

  const fields = versionQuery.data?.fields ?? []
  const version = versionQuery.data
  const isDraft = version?.status === SCHEMA_STATUS.Draft
  const hasDraft = versionsQuery.data?.some((v) => v.status === SCHEMA_STATUS.Draft) ?? false
  const closeDialog = (open: boolean) => {
    if (!open) setDialog(null)
  }

  const startDraft = () =>
    createDraft.mutate(undefined, {
      onSuccess: (draft) => {
        toast.success('بدأت مسودة جديدة.')
        update({ version: draft.schemaVersionId })
      },
      onError: (error) => toast.error(toArabicErrorMessage(error)),
    })

  const buildRowMenu = (field: SchemaFieldResponse): MenuEntry[] =>
    isDraft
      ? [
          {
            id: 'edit',
            label: 'تعديل الإعدادات',
            icon: <Pencil className="size-4" aria-hidden />,
            onSelect: () => setDialog({ type: 'edit', field }),
          },
          {
            id: 'remove',
            label: 'إزالة من المسودة',
            icon: <Trash2 className="size-4" aria-hidden />,
            danger: true,
            onSelect: () => setDialog({ type: 'remove', field }),
          },
        ]
      : []

  const toolbar = (
    <div className="flex flex-wrap items-center gap-2">
      <Button
        type="button"
        size="sm"
        variant="outline"
        disabled={hasDraft}
        loading={createDraft.isPending}
        title={hasDraft ? 'للنوع مسودة قائمة؛ انشرها أو احذفها أولًا.' : undefined}
        onClick={startDraft}
      >
        <Plus className="size-4" aria-hidden />
        مسودة جديدة
      </Button>
      {isDraft && (
        <>
          <Button type="button" size="sm" onClick={() => setDialog({ type: 'add' })}>
            <Plus className="size-4" aria-hidden />
            إضافة حقل
          </Button>
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={fields.length === 0}
            onClick={() => setDialog({ type: 'publish' })}
          >
            <Rocket className="size-4" aria-hidden />
            نشر النسخة
          </Button>
          <Button
            type="button"
            size="sm"
            variant="ghost"
            onClick={() => setDialog({ type: 'delete' })}
          >
            <Trash2 className="size-4" aria-hidden />
            حذف المسودة
          </Button>
        </>
      )}
    </div>
  )

  return (
    <div className="flex flex-col gap-4">
      <GuideRegion id="document-schemas.type">
        <div className="flex flex-wrap items-center gap-3">
          <span className="text-sm font-medium text-foreground">نوع الوثيقة</span>
          <Select
            aria-label="اختيار نوع الوثيقة"
            value={documentTypeId === undefined ? undefined : String(documentTypeId)}
            onValueChange={(value) => update({ type: value, version: null })}
            options={typeOptions}
            placeholder="اختر نوع الوثيقة…"
            className="w-64"
          />
          <span className="text-sm font-medium text-foreground">النموذج</span>
          <Select
            aria-label="اختيار النموذج: نموذج الإنشاء أو سجل ديوان"
            value={departmentId ?? CREATION_FORM}
            onValueChange={(value) =>
              update({ dept: value === CREATION_FORM ? null : value, version: null })
            }
            options={formOptions}
            className="w-72"
          />
        </div>
        {departmentId && (
          <p className="mt-2 text-xs text-muted">
            يملأ الديوان هذا النموذج عند استلام وثيقة من هذا النوع، ويُقفل سجله عند خروج الوثيقة منه.
          </p>
        )}
      </GuideRegion>

      {documentTypeId === undefined ? (
        <p className="rounded-xl border border-dashed border-border p-6 text-center text-sm text-muted">
          اختر نوع وثيقة لعرض حقوله ونسخ مخططه.
        </p>
      ) : (
        <>
          <GuideRegion id="document-schemas.versions">
            <div
              role="tablist"
              aria-label="نسخ المخطط"
              className="flex flex-wrap items-center gap-2"
            >
              {versionsQuery.isError && (
                <span role="alert" className="text-sm text-danger">
                  {toArabicErrorMessage(versionsQuery.error)}
                </span>
              )}
              {versionsQuery.data?.length === 0 && (
                <span className="text-sm text-muted">لا توجد نسخ مخطط لهذا النوع بعد.</span>
              )}
              {versionsQuery.data?.map((version) => {
                const isActive = version.schemaVersionId === selectedVersionId
                return (
                  <button
                    key={version.schemaVersionId}
                    type="button"
                    role="tab"
                    aria-selected={isActive}
                    onClick={() => update({ version: version.schemaVersionId })}
                    className={cn(
                      'flex flex-col items-start gap-1 rounded-xl border px-4 py-2.5 text-start transition-colors',
                      isActive
                        ? 'border-accent bg-accent/10'
                        : 'border-border bg-surface hover:bg-surface-muted/60',
                    )}
                  >
                    <span className="flex items-center gap-2 text-sm font-semibold text-foreground">
                      <span dir="ltr">v{version.versionNumber}</span>
                      <SchemaStatusBadge status={version.status} />
                    </span>
                    <span className="text-xs text-muted">{version.fieldCount} حقلًا</span>
                    <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
                      {formatDate(version.publishedAt ?? version.createdAt)}
                    </span>
                  </button>
                )
              })}
            </div>
          </GuideRegion>

          <GuideRegion id="document-schemas.fields">
            <DataTable
              tableId="document-schema-fields"
              mode="client"
              caption="حقول نسخة المخطط"
              columns={columns}
              rows={fields}
              rowKey={(field) => field.documentFieldId}
              rowLabel={(field) => field.labelAr}
              rowSearchText={(field) => `${field.labelAr} ${field.key}`}
              isLoading={versionQuery.isLoading}
              error={versionQuery.error}
              onRetry={refresh}
              onRefresh={refresh}
              isFetching={versionQuery.isFetching}
              page={table.page}
              pageSize={table.pageSize}
              totalCount={fields.length}
              onPageChange={table.setPage}
              onPageSizeChange={table.setPageSize}
              search={table.search}
              onSearchChange={table.setSearch}
              searchPlaceholder="ابحث بعنوان الحقل أو مفتاحه…"
              sortKey={table.sortKey}
              sortDirection={table.sortDirection}
              onSortChange={table.setSort}
              toolbar={toolbar}
              rowMenu={buildRowMenu}
              headerMenu={[refreshEntry]}
              emptyMenu={[refreshEntry]}
              emptyTitle={table.search ? 'لا حقول مطابقة.' : 'لا حقول في هذه النسخة.'}
              emptyDescription={
                table.search
                  ? 'جرّب تعديل البحث لعرض حقول أخرى.'
                  : 'تظهر هنا حقول النسخة المحددة بترتيب عرضها.'
              }
            />
          </GuideRegion>
        </>
      )}
      {(dialog?.type === 'add' || dialog?.type === 'edit') && version && (
        // Mounted per target so the form starts from that field's settings.
        <FieldSettingsDialog
          key={dialog.type === 'edit' ? dialog.field.documentFieldId : 'add'}
          version={version}
          field={dialog.type === 'edit' ? dialog.field : undefined}
          definedFields={definedFieldsQuery.data ?? []}
          onOpenChange={closeDialog}
        />
      )}
      <RemoveFieldDialog
        version={version ?? null}
        field={dialog?.type === 'remove' ? dialog.field : null}
        onOpenChange={closeDialog}
      />
      <PublishVersionDialog
        version={dialog?.type === 'publish' ? (version ?? null) : null}
        onOpenChange={closeDialog}
      />
      <DeleteDraftDialog
        version={dialog?.type === 'delete' ? (version ?? null) : null}
        onOpenChange={closeDialog}
      />
    </div>
  )
}
