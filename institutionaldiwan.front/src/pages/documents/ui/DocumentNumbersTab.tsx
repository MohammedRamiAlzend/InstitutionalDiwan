import { useMemo, useState } from 'react'
import {
  Ban,
  Building2,
  Copy,
  Hash,
  RefreshCw,
  Replace,
} from 'lucide-react'
import { toast } from 'sonner'
import {
  NumberStatusBadge,
  useDocumentNumberHistory,
  useDocumentRegistryNumbers,
  type DocumentNumberResponse,
  type DepartmentRegistryNumberResponse,
} from '@/entities/document'
import {
  CancelNumberDialog,
  DepartmentRegistrationDialog,
  IssueNumberDialog,
  ReplaceNumberDialog,
} from '@/features/numbering-manage'
import { toArabicErrorMessage } from '@/shared/api'
import {
  DOCUMENT_NUMBER_EVENT_TYPE,
  DOCUMENT_NUMBER_STATUS,
  DOCUMENT_STATUS,
} from '@/shared/config'
import {
  formatDate,
  formatDateTime,
  formatShortId,
  useTableState,
} from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  Skeleton,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

const numberEventLabels: Record<number, string> = {
  [DOCUMENT_NUMBER_EVENT_TYPE.Issued]: 'صدر',
  [DOCUMENT_NUMBER_EVENT_TYPE.Cancelled]: 'أُلغي',
  [DOCUMENT_NUMBER_EVENT_TYPE.Replaced]: 'استُبدل',
  [DOCUMENT_NUMBER_EVENT_TYPE.RegisteredInDepartment]: 'سُجّل في الدائرة',
  [DOCUMENT_NUMBER_EVENT_TYPE.ReRegisteredInDepartment]: 'أُعيد تسجيله في الدائرة',
}

const numberStatusLabels: Record<number, string> = {
  [DOCUMENT_NUMBER_STATUS.Active]: 'نشط',
  [DOCUMENT_NUMBER_STATUS.Cancelled]: 'ملغى',
  [DOCUMENT_NUMBER_STATUS.Replaced]: 'مُستبدل',
}

/** The API stamps replacement numbers with an English `Replaces …` note; keep it Arabic here. */
function formatNumberNote(note: string): string {
  const replaced = /^Replaces\s+(.+)$/.exec(note)
  return replaced ? `يستبدل الرقم ${replaced[1]}` : note
}

function latestNumberNote(number: DocumentNumberResponse): string | null {
  const note = number.history.find((event) => event.notes)?.notes
  return note ? formatNumberNote(note) : null
}

interface DocumentNumbersTabProps {
  documentId: string
  documentStatusId: number
  canManage: boolean
  departmentNames: Map<string, string>
}

/** Numbers tab: central number, department register lines, and the full number history. */
export function DocumentNumbersTab({
  documentId,
  documentStatusId,
  canManage,
  departmentNames,
}: DocumentNumbersTabProps) {
  const historyQuery = useDocumentNumberHistory(documentId)
  const entriesQuery = useDocumentRegistryNumbers(documentId)
  const table = useTableState({
    defaultSortKey: 'issuedAt',
    defaultSortDirection: 'desc',
  })

  const [issueOpen, setIssueOpen] = useState(false)
  const [departmentOpen, setDepartmentOpen] = useState(false)
  const [cancelNumber, setCancelNumber] = useState<DocumentNumberResponse | null>(null)
  const [replaceNumber, setReplaceNumber] = useState<DocumentNumberResponse | null>(null)

  const history = historyQuery.data ?? []
  const entries = entriesQuery.data
  const isDraft = documentStatusId === DOCUMENT_STATUS.Draft

  const historyById = useMemo(
    () =>
      new Map(
        (historyQuery.data ?? []).map((number) => [number.documentNumberId, number]),
      ),
    [historyQuery.data],
  )

  const central = entries?.centralNumberId
    ? historyById.get(entries.centralNumberId)
    : undefined
  const centralActive = central?.statusId === DOCUMENT_NUMBER_STATUS.Active
  const hasCentralNumber = Boolean(entries?.centralNumberId)

  const departmentNameOf = (departmentId: string | null) =>
    departmentId ? departmentNames.get(departmentId) ?? formatShortId(departmentId) : '—'

  const columns = useMemo<DataTableColumn<DocumentNumberResponse>[]>(() => {
    const scopeLabel = (number: DocumentNumberResponse) =>
      number.scopeId === 1
        ? 'الديوان المركزي'
        : number.departmentId
          ? departmentNames.get(number.departmentId) ?? formatShortId(number.departmentId)
          : '—'

    return [
      {
        id: 'number',
        header: 'الرقم',
        hideable: false,
        sortable: true,
        sortValue: (number) => number.officialNumberText,
        cell: (number) => (
          <code dir="ltr" className="text-xs font-semibold text-foreground">
            {number.officialNumberText}
          </code>
        ),
      },
      {
        id: 'scope',
        header: 'النطاق',
        sortable: true,
        sortValue: scopeLabel,
        cell: (number) => (
          <span className="text-xs text-muted">{scopeLabel(number)}</span>
        ),
      },
      {
        id: 'year',
        header: 'السنة',
        sortable: true,
        sortValue: (number) => number.numberingYear,
        cell: (number) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {number.numberingYear}
          </span>
        ),
      },
      {
        id: 'status',
        header: 'الحالة',
        cell: (number) => <NumberStatusBadge statusId={number.statusId} />,
      },
      {
        id: 'issuedAt',
        header: 'تاريخ الإصدار',
        sortable: true,
        sortValue: (number) => number.issuedAt,
        cell: (number) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(number.issuedAt)}
          </span>
        ),
      },
      {
        id: 'notes',
        header: 'ملاحظات',
        sortable: true,
        sortValue: (number) => number.cancellationReason ?? latestNumberNote(number),
        cell: (number) => (
          <span className="line-clamp-2 max-w-64 text-xs text-muted">
            {number.cancellationReason ?? latestNumberNote(number) ?? '—'}
          </span>
        ),
      },
    ]
  }, [departmentNames])

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(`تم نسخ ${label}.`)
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const buildRowMenu = (number: DocumentNumberResponse): MenuEntry[] => [
    {
      id: 'copy-number',
      label: 'نسخ الرقم',
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyValue(number.officialNumberText, 'الرقم'),
    },
    {
      id: 'copy-id',
      label: 'نسخ المعرّف',
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyValue(number.documentNumberId, 'المعرّف'),
    },
  ]

  const isLoading = historyQuery.isLoading || entriesQuery.isLoading
  const error = historyQuery.error ?? entriesQuery.error
  const refetch = () => {
    void historyQuery.refetch()
    void entriesQuery.refetch()
  }

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-4 rounded-xl border border-border bg-surface p-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="flex flex-col gap-1">
            <h2 className="text-base font-semibold text-foreground">الرقم المركزي</h2>
            <p className="max-w-2xl text-xs text-muted">
              رقم الديوان الرسمي للوثيقة، ويبقى ثابتًا عند تسجيل أرقام الدوائر في سجلاتها.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            {canManage && isDraft && (
              <Button type="button" size="sm" onClick={() => setIssueOpen(true)}>
                <Hash className="size-4" aria-hidden />
                إصدار رقم
              </Button>
            )}
            {canManage && hasCentralNumber && (
              <Button
                type="button"
                size="sm"
                variant="outline"
                onClick={() => setDepartmentOpen(true)}
              >
                <Building2 className="size-4" aria-hidden />
                تسجيل رقم الدائرة
              </Button>
            )}
            <Button
              type="button"
              size="icon"
              variant="outline"
              onClick={refetch}
              aria-label="تحديث الأرقام"
              className="size-9"
            >
              <RefreshCw
                className={historyQuery.isFetching || entriesQuery.isFetching ? 'size-4 animate-spin' : 'size-4'}
                aria-hidden
              />
            </Button>
          </div>
        </div>

        {isLoading ? (
          <>
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-10 w-2/3" />
          </>
        ) : error ? (
          <div
            role="alert"
            className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger"
          >
            <span>{toArabicErrorMessage(error)}</span>
            <Button type="button" variant="outline" size="sm" onClick={refetch}>
              إعادة المحاولة
            </Button>
          </div>
        ) : !hasCentralNumber ? (
          <p className="rounded-lg border border-dashed border-border px-4 py-6 text-center text-sm text-muted">
            {isDraft
              ? 'الوثيقة مسودة بلا رقم رسمي. أصدر رقمًا ليُسجَّل في الديوان وترتبط به أرقام الدوائر.'
              : 'لا يوجد رقم مركزي لهذه الوثيقة.'}
          </p>
        ) : (
          <div className="flex flex-col gap-4">
            <div className="flex flex-wrap items-center gap-3">
              <span
                dir="ltr"
                className="rounded-lg bg-surface-muted px-3 py-1.5 font-mono text-lg font-semibold text-foreground"
              >
                {entries?.centralNumberText ?? central?.officialNumberText ?? '—'}
              </span>
              <NumberStatusBadge statusId={central?.statusId ?? 1} />
              {entries?.centralRegistrationDate && (
                <span className="text-xs text-muted">
                  مسجّل في: <span dir="ltr">{formatDate(entries.centralRegistrationDate)}</span>
                </span>
              )}
              {central && (
                <span className="text-xs text-muted">
                  أصدره: {formatShortId(central.issuedByUserId)} ·{' '}
                  <span dir="ltr">{formatDateTime(central.issuedAt)}</span>
                </span>
              )}

              {canManage && centralActive && central && (
                <div className="ms-auto flex flex-wrap items-center gap-2">
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    onClick={() => setReplaceNumber(central)}
                  >
                    <Replace className="size-4" aria-hidden />
                    استبدال
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    className="text-danger"
                    onClick={() => setCancelNumber(central)}
                  >
                    <Ban className="size-4" aria-hidden />
                    إلغاء
                  </Button>
                </div>
              )}
            </div>

            {central?.cancellationReason && (
              <p className="rounded-lg bg-danger/10 px-3 py-2 text-xs text-danger">
                سبب الإلغاء: {central.cancellationReason}
              </p>
            )}

            {central?.replacedByNumberId && (
              <p className="rounded-lg bg-surface-muted px-3 py-2 text-xs text-foreground">
                استُبدل بالرقم:{' '}
                <span dir="ltr" className="font-mono">
                  {historyById.get(central.replacedByNumberId)?.officialNumberText ??
                    formatShortId(central.replacedByNumberId)}
                </span>
              </p>
            )}

            {central && central.history.length > 0 && (
              <ul className="flex flex-col gap-2">
                {central.history.map((event) => (
                  <li
                    key={event.historyId}
                    className="flex flex-wrap items-center gap-2 rounded-lg border border-border px-3 py-2 text-xs"
                  >
                    <Badge variant="outline">
                      {numberEventLabels[event.eventTypeId] ?? `حدث #${event.eventTypeId}`}
                    </Badge>
                    <span dir="ltr" className="text-muted">
                      {formatDateTime(event.occurredAt)}
                    </span>
                    {event.effectiveDate && (
                      <span className="text-muted">
                        بتاريخ <span dir="ltr">{formatDate(event.effectiveDate)}</span>
                      </span>
                    )}
                    <span className="text-muted">
                      بواسطة {formatShortId(event.actorUserId)}
                    </span>
                    {event.notes && (
                      <span className="text-foreground">{formatNumberNote(event.notes)}</span>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </section>

      <section className="flex flex-col gap-4 rounded-xl border border-border bg-surface p-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="flex flex-col gap-1">
            <h2 className="text-base font-semibold text-foreground">أرقام سجلات الدوائر</h2>
            <p className="max-w-2xl text-xs text-muted">
              الأرقام التي كتبتها الدوائر في سجلاتها الخاصة للوثيقة، مع كل تاريخ تسجيل أو إعادة
              تسجيل.
            </p>
          </div>
          {canManage && hasCentralNumber && (
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={() => setDepartmentOpen(true)}
            >
              <Building2 className="size-4" aria-hidden />
              تسجيل رقم
            </Button>
          )}
        </div>

        {isLoading ? (
          <Skeleton className="h-20 w-full" />
        ) : error ? null : (entries?.departmentNumbers.length ?? 0) === 0 ? (
          <p className="rounded-lg border border-dashed border-border px-4 py-6 text-center text-sm text-muted">
            لم تسجّل أي دائرة رقمًا خاصًا بها لهذه الوثيقة بعد.
          </p>
        ) : (
          <ul className="flex flex-col gap-3">
            {entries?.departmentNumbers.map((line: DepartmentRegistryNumberResponse) => (
              <li
                key={line.documentNumberId}
                className="flex flex-col gap-2 rounded-lg border border-border bg-background px-4 py-3"
              >
                <div className="flex flex-wrap items-center gap-2">
                  <code dir="ltr" className="font-mono text-sm font-semibold text-foreground">
                    {line.registryNumberText}
                  </code>
                  <Badge variant="outline">{departmentNameOf(line.departmentId)}</Badge>
                  <NumberStatusBadge statusId={line.statusId} />
                  {line.registryDate && (
                    <span className="text-xs text-muted">
                      بتاريخ <span dir="ltr">{formatDate(line.registryDate)}</span>
                    </span>
                  )}
                  <span className="text-xs text-muted">
                    {line.registrations.length} تسجيل
                  </span>
                </div>

                {line.registrations.length > 0 && (
                  <ul className="flex flex-col gap-1.5 border-s-2 border-border ps-3">
                    {line.registrations.map((event) => (
                      <li
                        key={event.historyId}
                        className="flex flex-wrap items-center gap-2 text-xs"
                      >
                        <Badge variant="outline">
                          {numberEventLabels[event.eventTypeId] ?? `حدث #${event.eventTypeId}`}
                        </Badge>
                        <span dir="ltr" className="text-muted">
                          {formatDateTime(event.occurredAt)}
                        </span>
                        <span className="text-muted">
                          بواسطة {formatShortId(event.actorUserId)}
                        </span>
                        {event.notes && (
                          <span className="text-foreground">{formatNumberNote(event.notes)}</span>
                        )}
                      </li>
                    ))}
                  </ul>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="flex flex-col gap-4 rounded-xl border border-border bg-surface p-5">
        <div className="flex flex-col gap-1">
          <h2 className="text-base font-semibold text-foreground">سجل الأرقام</h2>
          <p className="max-w-2xl text-xs text-muted">
            كل رقم صدر للوثيقة، بما فيه الأرقام الملغاة والمستبدلة؛ لا يُحذف أي سجل.
          </p>
        </div>

        <DataTable
          tableId="document-numbers"
          mode="client"
          caption="سجل الأرقام الرسمية"
          columns={columns}
          rows={history}
          rowKey={(number) => number.documentNumberId}
          rowLabel={(number) => number.officialNumberText}
          rowSearchText={(number) =>
            [
              number.officialNumberText,
              number.scopeId === 1
                ? 'الديوان المركزي'
                : departmentNameOf(number.departmentId),
              String(number.numberingYear),
              numberStatusLabels[number.statusId],
              number.cancellationReason,
              latestNumberNote(number),
            ]
              .filter(Boolean)
              .join(' ')
              .toLocaleLowerCase('ar')
          }
          isLoading={historyQuery.isLoading}
          error={historyQuery.error}
          onRetry={() => void historyQuery.refetch()}
          onRefresh={refetch}
          isFetching={historyQuery.isFetching || entriesQuery.isFetching}
          page={table.page}
          pageSize={table.pageSize}
          totalCount={history.length}
          onPageChange={table.setPage}
          onPageSizeChange={table.setPageSize}
          search={table.search}
          onSearchChange={table.setSearch}
          searchPlaceholder="ابحث بالرقم أو النطاق أو السنة…"
          sortKey={table.sortKey}
          sortDirection={table.sortDirection}
          onSortChange={table.setSort}
          rowMenu={buildRowMenu}
          emptyTitle="لم يصدر أي رقم رسمي لهذه الوثيقة بعد."
          emptyDescription={
            isDraft
              ? 'أصدر رقمًا من بطاقة الرقم المركزي ليظهر في هذا السجل.'
              : 'تظهر هنا كل الأرقام الرسمية الصادرة لهذه الوثيقة.'
          }
        />
      </section>

      {issueOpen && (
        <IssueNumberDialog
          documentId={documentId}
          open
          onOpenChange={setIssueOpen}
        />
      )}
      {departmentOpen && (
        <DepartmentRegistrationDialog
          documentId={documentId}
          open
          onOpenChange={setDepartmentOpen}
        />
      )}
      {cancelNumber && (
        <CancelNumberDialog
          documentId={documentId}
          number={cancelNumber}
          open
          onOpenChange={(open) => {
            if (!open) setCancelNumber(null)
          }}
        />
      )}
      {replaceNumber && (
        <ReplaceNumberDialog
          documentId={documentId}
          number={replaceNumber}
          open
          onOpenChange={(open) => {
            if (!open) setReplaceNumber(null)
          }}
        />
      )}
    </div>
  )
}
