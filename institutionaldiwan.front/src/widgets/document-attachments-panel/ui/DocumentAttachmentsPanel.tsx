import { useCallback, useMemo, useState } from 'react'
import {
  Archive,
  Copy,
  Download,
  Fingerprint,
  Hash,
  History,
  RefreshCw,
  ShieldCheck,
  Upload,
} from 'lucide-react'
import { toast } from 'sonner'
import {
  currentVersionOf,
  downloadFileVersion,
  FileArchivedBadge,
  useDocumentFiles,
  type DocumentFileResponse,
} from '@/entities/document-file'
import {
  findLookupFallback,
  LOOKUP_SLUGS,
  LookupLabel,
  useLookupEntries,
} from '@/entities/lookup'
import {
  ArchiveFileDialog,
  DocumentVersionsDialog,
  UploadAttachmentDialog,
  UploadVersionDialog,
  VerifyChecksumDialog,
} from '@/features/attachment-upload'
import { toArabicErrorMessage } from '@/shared/api'
import {
  formatDateTime,
  formatFileSize,
  formatShortId,
  useTableState,
} from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  Select,
  type DataTableColumn,
  type MenuEntry,
  type SelectOption,
} from '@/shared/ui'

const STATUS_OPTIONS: SelectOption[] = [
  { value: 'all', label: 'كل الحالات' },
  { value: 'active', label: 'نشط' },
  { value: 'archived', label: 'مؤرشف' },
]

/**
 * The one row-level dialog open at a time. Only the file id is kept and the row is looked up again
 * from the live query, so a mutation that refetches the list also updates what the dialog shows.
 */
type RowAction = {
  type: 'versions' | 'upload-version' | 'archive' | 'verify'
  fileId: string
}

interface DocumentAttachmentsPanelProps {
  documentId: string
  /** `DOCUMENT_EDIT` on a live document: the three write actions are hidden entirely without it. */
  canManage?: boolean
}

/**
 * The document's files as a grid: archived files stay listed because archiving stops new versions
 * without hiding anything. The list route is unpaged, so the status and file-type filters run here
 * while the grid keeps search, sort, and paging in the URL.
 */
export function DocumentAttachmentsPanel({
  documentId,
  canManage = false,
}: DocumentAttachmentsPanelProps) {
  const filesQuery = useDocumentFiles(documentId)
  const fileTypesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentFileTypes)
  const table = useTableState({ filterKeys: ['status', 'fileType'] })
  const [uploadOpen, setUploadOpen] = useState(false)
  const [rowAction, setRowAction] = useState<RowAction | null>(null)

  const statusFilter = table.filters.status
  const fileTypeFilter = table.filters.fileType

  const files = useMemo(() => {
    const all = filesQuery.data ?? []

    return all.filter((file) => {
      if (statusFilter === 'active' && file.isArchived) return false
      if (statusFilter === 'archived' && !file.isArchived) return false
      return !fileTypeFilter || String(file.fileTypeId) === fileTypeFilter
    })
  }, [filesQuery.data, statusFilter, fileTypeFilter])

  const fileTypeLabel = useCallback(
    (fileTypeId: number) =>
      fileTypesQuery.data?.find((entry) => entry.id === fileTypeId)?.descAr ??
      findLookupFallback(LOOKUP_SLUGS.DocumentFileTypes, fileTypeId)?.descAr ??
      `#${fileTypeId}`,
    [fileTypesQuery.data],
  )

  const fileTypeOptions = useMemo<SelectOption[]>(
    () => [
      { value: 'all', label: 'كل الأنواع' },
      ...(fileTypesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [fileTypesQuery.data],
  )

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(`تم نسخ ${label}.`)
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const download = async (file: DocumentFileResponse) => {
    try {
      await downloadFileVersion(file, currentVersionOf(file))
      toast.success(`نُزّل الملف «${file.title}».`)
    } catch (error) {
      // Includes the per-document DOWNLOAD denial, which the client cannot predict.
      toast.error(toArabicErrorMessage(error))
    }
  }

  const columns = useMemo<DataTableColumn<DocumentFileResponse>[]>(
    () => [
      {
        id: 'title',
        header: 'العنوان',
        hideable: false,
        sortable: true,
        sortValue: (file) => file.title,
        cell: (file) => (
          <span
            className="block max-w-64 truncate font-medium text-foreground"
            title={file.title}
          >
            {file.title}
          </span>
        ),
      },
      {
        id: 'fileType',
        header: 'النوع',
        sortable: true,
        sortValue: (file) => fileTypeLabel(file.fileTypeId),
        cell: (file) => (
          <LookupLabel
            lookup={LOOKUP_SLUGS.DocumentFileTypes}
            id={file.fileTypeId}
            className="text-xs"
          />
        ),
      },
      {
        id: 'currentVersion',
        header: 'الإصدار',
        sortable: true,
        sortValue: (file) => file.currentVersionNumber,
        cell: (file) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            v{file.currentVersionNumber}
          </span>
        ),
      },
      {
        id: 'size',
        header: 'الحجم',
        sortable: true,
        sortValue: (file) => currentVersionOf(file)?.fileSizeBytes ?? null,
        cell: (file) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatFileSize(currentVersionOf(file)?.fileSizeBytes)}
          </span>
        ),
      },
      {
        id: 'originalFileName',
        header: 'الاسم الأصلي',
        sortable: true,
        sortValue: (file) => currentVersionOf(file)?.originalFileName ?? null,
        cell: (file) => {
          const name = currentVersionOf(file)?.originalFileName ?? '—'

          return (
            <span
              dir="ltr"
              className="block max-w-56 truncate text-xs text-muted"
              title={name}
            >
              {name}
            </span>
          )
        },
      },
      {
        id: 'primaryScan',
        header: 'مسح ضوئي أساسي',
        sortable: true,
        sortValue: (file) => (file.isPrimaryScan ? 1 : 0),
        cell: (file) =>
          file.isPrimaryScan ? (
            <Badge variant="accent">مسح ضوئي أساسي</Badge>
          ) : (
            <span className="text-xs text-muted">—</span>
          ),
      },
      {
        id: 'status',
        header: 'الحالة',
        sortable: true,
        sortValue: (file) => (file.isArchived ? 1 : 0),
        cell: (file) => <FileArchivedBadge isArchived={file.isArchived} />,
      },
      {
        id: 'checksum',
        header: 'البصمة',
        defaultHidden: true,
        cell: (file) => {
          const checksum = currentVersionOf(file)?.checksumSha256

          return (
            <code dir="ltr" className="text-xs text-muted" title={checksum}>
              {formatShortId(checksum)}
            </code>
          )
        },
      },
      {
        id: 'createdAt',
        header: 'تاريخ الإنشاء',
        defaultHidden: true,
        sortable: true,
        sortValue: (file) => file.createdAt,
        cell: (file) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(file.createdAt)}
          </span>
        ),
      },
      {
        id: 'updatedAt',
        header: 'آخر تحديث',
        defaultHidden: true,
        sortable: true,
        sortValue: (file) => file.updatedAt,
        cell: (file) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {file.updatedAt ? formatDateTime(file.updatedAt) : '—'}
          </span>
        ),
      },
    ],
    [fileTypeLabel],
  )

  const buildRowMenu = (file: DocumentFileResponse): MenuEntry[] => {
    const version = currentVersionOf(file)
    // Archiving and a new version are one-way writes, so an archived row offers neither.
    const writeEntries: MenuEntry[] =
      canManage && !file.isArchived
        ? [
            { type: 'separator' },
            {
              id: 'upload-version',
              label: `رفع إصدار بعد v${file.currentVersionNumber}`,
              icon: <Upload className="size-4" aria-hidden />,
              onSelect: () => setRowAction({ type: 'upload-version', fileId: file.fileId }),
            },
            {
              id: 'archive',
              label: 'أرشفة الملف',
              icon: <Archive className="size-4" aria-hidden />,
              danger: true,
              onSelect: () => setRowAction({ type: 'archive', fileId: file.fileId }),
            },
          ]
        : []

    return [
      {
        id: 'download',
        label: 'تنزيل الإصدار الحالي',
        icon: <Download className="size-4" aria-hidden />,
        disabled: !version,
        onSelect: () => void download(file),
      },
      {
        id: 'versions',
        label: 'الإصدارات',
        icon: <History className="size-4" aria-hidden />,
        onSelect: () => setRowAction({ type: 'versions', fileId: file.fileId }),
      },
      {
        id: 'verify',
        label: 'التحقق من البصمة',
        icon: <ShieldCheck className="size-4" aria-hidden />,
        disabled: !version,
        onSelect: () => setRowAction({ type: 'verify', fileId: file.fileId }),
      },
      ...writeEntries,
      { type: 'separator' },
      {
        id: 'copy-id',
        label: 'نسخ معرّف الملف',
        icon: <Hash className="size-4" aria-hidden />,
        onSelect: () => void copyValue(file.fileId, 'معرّف الملف'),
      },
      {
        id: 'copy-checksum',
        label: 'نسخ البصمة',
        icon: <Fingerprint className="size-4" aria-hidden />,
        disabled: !version?.checksumSha256,
        onSelect: () => void copyValue(version?.checksumSha256 ?? '', 'البصمة'),
      },
      {
        id: 'copy-name',
        label: 'نسخ الاسم الأصلي',
        icon: <Copy className="size-4" aria-hidden />,
        disabled: !version?.originalFileName,
        onSelect: () => void copyValue(version?.originalFileName ?? '', 'الاسم الأصلي'),
      },
    ]
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: 'تحديث الملفات',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void filesQuery.refetch(),
  }

  const hasQuery = Boolean(table.search) || table.hasActiveFilters

  const uploadButton = canManage ? (
    <Button type="button" size="sm" onClick={() => setUploadOpen(true)}>
      <Upload className="size-4" aria-hidden />
      رفع ملف
    </Button>
  ) : undefined

  const closeRowAction = (open: boolean) => {
    if (!open) setRowAction(null)
  }

  const actionFile = rowAction
    ? (filesQuery.data ?? []).find((file) => file.fileId === rowAction.fileId)
    : undefined

  const grid = (
    <DataTable
      tableId="document-files"
      mode="client"
      caption="ملفات الوثيقة"
      columns={columns}
      rows={files}
      rowKey={(file) => file.fileId}
      rowLabel={(file) => file.title}
      rowSearchText={(file) =>
        [file.title, currentVersionOf(file)?.originalFileName, fileTypeLabel(file.fileTypeId)]
          .filter(Boolean)
          .join(' ')
      }
      isLoading={filesQuery.isLoading}
      error={filesQuery.error}
      onRetry={() => void filesQuery.refetch()}
      onRefresh={() => void filesQuery.refetch()}
      isFetching={filesQuery.isFetching}
      page={table.page}
      pageSize={table.pageSize}
      totalCount={files.length}
      onPageChange={table.setPage}
      onPageSizeChange={table.setPageSize}
      search={table.search}
      onSearchChange={table.setSearch}
      searchPlaceholder="ابحث بعنوان الملف أو اسمه الأصلي…"
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      toolbar={uploadButton}
      filters={
        <div className="flex flex-wrap items-center gap-2">
          <Select
            aria-label="تصفية حسب حالة الملف"
            value={table.filters.status || 'all'}
            onValueChange={(value) => table.setFilter('status', value === 'all' ? null : value)}
            options={STATUS_OPTIONS}
            className="w-40"
          />
          <Select
            aria-label="تصفية حسب نوع الملف"
            value={table.filters.fileType || 'all'}
            onValueChange={(value) =>
              table.setFilter('fileType', value === 'all' ? null : value)
            }
            options={fileTypeOptions}
            className="w-44"
          />
          {hasQuery && (
            <Button type="button" variant="ghost" size="sm" onClick={table.clearQuery}>
              مسح البحث والمرشحات
            </Button>
          )}
        </div>
      }
      rowMenu={buildRowMenu}
      headerMenu={[refreshEntry]}
      emptyMenu={[refreshEntry]}
      emptyAction={files.length === 0 ? uploadButton : undefined}
      emptyTitle={hasQuery ? 'لا توجد ملفات مطابقة.' : 'لا ملفات مرفقة بعد.'}
      emptyDescription={
        hasQuery
          ? 'جرّب تعديل البحث أو المرشحات لعرض ملفات أخرى.'
          : 'تظهر هنا ملفات الوثيقة كلها، بما فيها المؤرشفة، مع الإصدار الحالي والحجم والحالة.'
      }
    />
  )

  return (
    <>
      {grid}
      {canManage && (
        <UploadAttachmentDialog
          documentId={documentId}
          open={uploadOpen}
          onOpenChange={setUploadOpen}
        />
      )}
      {rowAction && actionFile && (
        <>
          {rowAction.type === 'versions' && (
            <DocumentVersionsDialog file={actionFile} open onOpenChange={closeRowAction} />
          )}
          {rowAction.type === 'upload-version' && (
            <UploadVersionDialog file={actionFile} open onOpenChange={closeRowAction} />
          )}
          {rowAction.type === 'archive' && (
            <ArchiveFileDialog file={actionFile} open onOpenChange={closeRowAction} />
          )}
          {rowAction.type === 'verify' && (
            <VerifyChecksumDialog file={actionFile} open onOpenChange={closeRowAction} />
          )}
        </>
      )}
    </>
  )
}
