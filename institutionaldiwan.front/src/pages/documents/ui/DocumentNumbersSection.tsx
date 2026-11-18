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
  type LabelKey,
} from '@/shared/config'
import {
  formatDate,
  formatDateTime,
  formatShortId,
  resolveLabel,
  t,
  useLabelsStore,
  useTableState,
} from '@/shared/lib'
import {
  Badge,
  Button,
  CollapsibleSection,
  DataTable,
  Label,
  Skeleton,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'
import { UserLabel } from '@/entities/user'

const NUMBER_EVENT_KEYS: Record<number, LabelKey> = {
  [DOCUMENT_NUMBER_EVENT_TYPE.Issued]: 'docdetails.number-event.issued',
  [DOCUMENT_NUMBER_EVENT_TYPE.Cancelled]: 'docdetails.number-event.cancelled',
  [DOCUMENT_NUMBER_EVENT_TYPE.Replaced]: 'docdetails.number-event.replaced',
  [DOCUMENT_NUMBER_EVENT_TYPE.RegisteredInDepartment]: 'docdetails.number-event.registered-dept',
  [DOCUMENT_NUMBER_EVENT_TYPE.ReRegisteredInDepartment]: 'docdetails.number-event.re-registered-dept',
}

const NUMBER_STATUS_KEYS: Record<number, LabelKey> = {
  [DOCUMENT_NUMBER_STATUS.Active]: 'documents.number-status.active',
  [DOCUMENT_NUMBER_STATUS.Cancelled]: 'documents.number-status.cancelled',
  [DOCUMENT_NUMBER_STATUS.Replaced]: 'documents.number-status.replaced',
}

/** The API stamps replacement numbers with an English `Replaces …` note; keep it Arabic here. */
function formatNumberNote(note: string): string {
  const replaced = /^Replaces\s+(.+)$/.exec(note)
  if (replaced) return t('docdetails.number-note.replaces', { number: replaced[1] })

  const renumbered = /^Replaced by department number\s+(.+)$/.exec(note)
  return renumbered ? t('docdetails.number-note.renumbered', { number: renumbered[1] }) : note
}

function latestNumberNote(number: DocumentNumberResponse): string | null {
  const note = number.history.find((event) => event.notes)?.notes
  return note ? formatNumberNote(note) : null
}

interface DocumentNumbersSectionProps {
  documentId: string
  documentStatusId: number
  canManage: boolean
  departmentNames: Map<string, string>
  /** Controlled collapse state, driven by the details tab's section navigation. */
  open?: boolean
  onOpenChange?: (open: boolean) => void
}

/** Numbers section of the details tab: central number, department register lines, and the full number history. */
export function DocumentNumbersSection({
  documentId,
  documentStatusId,
  canManage,
  departmentNames,
  open,
  onOpenChange,
}: DocumentNumbersSectionProps) {
  const historyQuery = useDocumentNumberHistory(documentId)
  const entriesQuery = useDocumentRegistryNumbers(documentId)
  const overrides = useLabelsStore((state) => state.overrides)
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

  const eventLabel = (id: number) =>
    resolveLabel(NUMBER_EVENT_KEYS[id] ?? 'docdetails.number-event.unknown', overrides, { id })
  const statusLabel = (id: number) =>
    resolveLabel(NUMBER_STATUS_KEYS[id] ?? 'documents.number-status.unknown', overrides, { id })

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
        ? resolveLabel('docdetails.scope.central', overrides)
        : number.departmentId
          ? departmentNames.get(number.departmentId) ?? formatShortId(number.departmentId)
          : '—'

    return [
      {
        id: 'number',
        headerKey: 'docdetails.column.number',
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
        headerKey: 'docdetails.column.scope',
        sortable: true,
        sortValue: scopeLabel,
        cell: (number) => (
          <span className="text-xs text-muted">{scopeLabel(number)}</span>
        ),
      },
      {
        id: 'year',
        headerKey: 'docdetails.column.year',
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
        headerKey: 'docdetails.column.status',
        cell: (number) => <NumberStatusBadge statusId={number.statusId} />,
      },
      {
        id: 'issuedAt',
        headerKey: 'docdetails.column.issued-at',
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
        headerKey: 'docdetails.column.notes',
        sortable: true,
        sortValue: (number) => number.cancellationReason ?? latestNumberNote(number),
        cell: (number) => (
          <span className="line-clamp-2 max-w-64 text-xs text-muted">
            {number.cancellationReason ?? latestNumberNote(number) ?? '—'}
          </span>
        ),
      },
    ]
  }, [departmentNames, overrides])

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('documents.toast.copied', { label }))
    } catch {
      toast.error(t('documents.toast.copy-failed'))
    }
  }

  const buildRowMenu = (number: DocumentNumberResponse): MenuEntry[] => [
    {
      id: 'copy-number',
      label: t('docdetails.row.copy-number'),
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyValue(number.officialNumberText, t('docdetails.copy.number-label')),
    },
    {
      id: 'copy-id',
      label: t('docdetails.row.copy-id'),
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyValue(number.documentNumberId, t('docdetails.copy.id-label')),
    },
  ]

  const isLoading = historyQuery.isLoading || entriesQuery.isLoading
  const error = historyQuery.error ?? entriesQuery.error
  const refetch = () => {
    void historyQuery.refetch()
    void entriesQuery.refetch()
  }

  return (
    <CollapsibleSection
      title={t('docdetails.section.numbers')}
      icon={<Hash className="size-4" aria-hidden />}
      hint={t('docdetails.numbers-hint')}
      open={open}
      onOpenChange={onOpenChange}
    >
      <div className="flex flex-col gap-5">
      <section className="flex flex-col gap-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="flex flex-col gap-1">
            <h3 className="text-sm font-semibold text-foreground"><Label k="docdetails.central-title" /></h3>
            <p className="max-w-2xl text-xs text-muted">
              <Label k="docdetails.central-hint" />
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            {canManage && isDraft && (
              <Button type="button" size="sm" onClick={() => setIssueOpen(true)}>
                <Hash className="size-4" aria-hidden />
                <Label k="docdetails.action.issue-number" />
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
                <Label k="docdetails.action.register-dept-number" />
              </Button>
            )}
            <Button
              type="button"
              size="icon"
              variant="outline"
              onClick={refetch}
              aria-label={t('docdetails.refresh-numbers-aria')}
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
              <Label k="common.action.retry" />
            </Button>
          </div>
        ) : !hasCentralNumber ? (
          <p className="rounded-lg border border-dashed border-border px-4 py-6 text-center text-sm text-muted">
            {isDraft
              ? <Label k="docdetails.numbers.empty-draft" />
              : <Label k="docdetails.numbers.empty-none" />}
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
                  <Label k="docdetails.registered-at" /> <span dir="ltr">{formatDate(entries.centralRegistrationDate)}</span>
                </span>
              )}
              {central && (
                <span className="text-xs text-muted">
                  <Label k="docdetails.issued-by" /> <UserLabel userId={central.issuedByUserId} /> ·{' '}
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
                    <Label k="docdetails.action.replace" />
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    className="text-danger"
                    onClick={() => setCancelNumber(central)}
                  >
                    <Ban className="size-4" aria-hidden />
                    <Label k="common.action.cancel" />
                  </Button>
                </div>
              )}
            </div>

            {central?.cancellationReason && (
              <p className="rounded-lg bg-danger/10 px-3 py-2 text-xs text-danger">
                <Label k="docdetails.cancel-reason" /> {central.cancellationReason}
              </p>
            )}

            {central?.replacedByNumberId && (
              <p className="rounded-lg bg-surface-muted px-3 py-2 text-xs text-foreground">
                <Label k="docdetails.replaced-by" />{' '}
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
                    className="flex flex-wrap items-center gap-2 py-1 text-xs"
                  >
                    <Badge variant="outline">
                      {eventLabel(event.eventTypeId)}
                    </Badge>
                    <span dir="ltr" className="text-muted">
                      {formatDateTime(event.occurredAt)}
                    </span>
                    {event.effectiveDate && (
                      <span className="text-muted">
                        <Label k="docdetails.on-date" /> <span dir="ltr">{formatDate(event.effectiveDate)}</span>
                      </span>
                    )}
                    <span className="text-muted">
                      <Label k="docdetails.by" /> <UserLabel userId={event.actorUserId} />
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

      <section className="flex flex-col gap-4 border-t border-border pt-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="flex flex-col gap-1">
            <h3 className="text-sm font-semibold text-foreground"><Label k="docdetails.dept-numbers-title" /></h3>
            <p className="max-w-2xl text-xs text-muted">
              <Label k="docdetails.dept-numbers-hint" />
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
              <Label k="docdetails.action.register-number" />
            </Button>
          )}
        </div>

        {isLoading ? (
          <Skeleton className="h-20 w-full" />
        ) : error ? null : (entries?.departmentNumbers.length ?? 0) === 0 ? (
          <p className="rounded-lg border border-dashed border-border px-4 py-6 text-center text-sm text-muted">
            <Label k="docdetails.dept-numbers.empty" />
          </p>
        ) : (
          <ul className="flex flex-col gap-4">
            {entries?.departmentNumbers.map((line: DepartmentRegistryNumberResponse) => (
              <li
                key={line.documentNumberId}
                className="flex flex-col gap-2 border-t border-border pt-4 first:border-t-0 first:pt-0"
              >
                <div className="flex flex-wrap items-center gap-2">
                  <code dir="ltr" className="font-mono text-sm font-semibold text-foreground">
                    {line.registryNumberText}
                  </code>
                  <Badge variant="outline">{departmentNameOf(line.departmentId)}</Badge>
                  <NumberStatusBadge statusId={line.statusId} />
                  {line.registryDate && (
                    <span className="text-xs text-muted">
                      <Label k="docdetails.on-date" /> <span dir="ltr">{formatDate(line.registryDate)}</span>
                    </span>
                  )}
                  <span className="text-xs text-muted">
                    {t('docdetails.visit-count', { count: line.visits.length })}
                  </span>
                </div>

                {(line.replacedByNumberText || line.replacesNumberText) && (
                  <p className="flex flex-wrap items-center gap-x-4 gap-y-1 rounded-lg bg-surface-muted px-3 py-2 text-xs text-foreground">
                    {line.replacesNumberText && (
                      <span>
                        <Label k="docdetails.replaces-previous" />{' '}
                        <span dir="ltr" className="font-mono font-semibold">
                          {line.replacesNumberText}
                        </span>
                      </span>
                    )}
                    {line.replacedByNumberText && (
                      <span>
                        <Label k="docdetails.replaced-by" />{' '}
                        <span dir="ltr" className="font-mono font-semibold">
                          {line.replacedByNumberText}
                        </span>
                      </span>
                    )}
                  </p>
                )}

                {line.visits.length > 0 && (
                  <div className="flex flex-wrap items-center gap-1.5 text-xs">
                    <span className="text-muted"><Label k="docdetails.visit-dates" /></span>
                    {line.visits.map((visit) => (
                      <Badge key={visit.visitId} variant="outline">
                        <span dir="ltr">{formatDate(visit.registryDate)}</span>
                      </Badge>
                    ))}
                  </div>
                )}

                {line.registrations.length > 0 && (
                  <ul className="flex flex-col gap-1.5 border-s-2 border-border ps-3">
                    {line.registrations.map((event) => (
                      <li
                        key={event.historyId}
                        className="flex flex-wrap items-center gap-2 text-xs"
                      >
                        <Badge variant="outline">
                          {eventLabel(event.eventTypeId)}
                        </Badge>
                        <span dir="ltr" className="text-muted">
                          {formatDateTime(event.occurredAt)}
                        </span>
                        <span className="text-muted">
                          <Label k="docdetails.by" /> <UserLabel userId={event.actorUserId} />
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

      <section className="flex flex-col gap-4 border-t border-border pt-5">
        <div className="flex flex-col gap-1">
          <h3 className="text-sm font-semibold text-foreground"><Label k="docdetails.number-log-title" /></h3>
          <p className="max-w-2xl text-xs text-muted">
            <Label k="docdetails.number-log-hint" />
          </p>
        </div>

        <DataTable
          tableId="document-numbers"
          mode="client"
          caption={t('docdetails.number-log-caption')}
          columns={columns}
          rows={history}
          rowKey={(number) => number.documentNumberId}
          rowLabel={(number) => number.officialNumberText}
          rowSearchText={(number) =>
            [
              number.officialNumberText,
              number.scopeId === 1
                ? resolveLabel('docdetails.scope.central', overrides)
                : departmentNameOf(number.departmentId),
              String(number.numberingYear),
              statusLabel(number.statusId),
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
          searchPlaceholder={t('docdetails.number-log-search')}
          sortKey={table.sortKey}
          sortDirection={table.sortDirection}
          onSortChange={table.setSort}
          rowMenu={buildRowMenu}
          emptyTitle={t('docdetails.number-log.empty-title')}
          emptyDescription={
            isDraft
              ? t('docdetails.number-log.empty-draft')
              : t('docdetails.number-log.empty-none')
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
    </CollapsibleSection>
  )
}