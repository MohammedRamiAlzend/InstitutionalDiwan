import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ExternalLink, Pencil, Trash2, UserPlus } from 'lucide-react'
import { toast } from 'sonner'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { useDocumentPersons, type DocumentPersonResponse } from '@/entities/person'
import { useSessionStore } from '@/entities/user'
import {
  AddDocumentPersonsDialog,
  ChangeDocumentPersonRoleDialog,
  useRemoveDocumentPersonMutation,
} from '@/features/document-person-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { Permissions, ROUTES } from '@/shared/config'
import { formatDateTime, t, useTableState } from '@/shared/lib'
import { Button, ConfirmDialog, DataTable, Label, type DataTableColumn, type MenuEntry } from '@/shared/ui'

interface DocumentPersonsTabProps {
  documentId: string
  canManage: boolean
  frozenReason?: string
}

/** People named on the document (defendants, plaintiffs, ...), who are not system users. */
export function DocumentPersonsTab({ documentId, canManage, frozenReason }: DocumentPersonsTabProps) {
  const table = useTableState()
  const navigate = useNavigate()
  const canViewPersons = useSessionStore((state) => state.hasPermission(Permissions.PersonView))
  const personsQuery = useDocumentPersons(documentId)
  const removeMutation = useRemoveDocumentPersonMutation()

  const [addOpen, setAddOpen] = useState(false)
  const [roleTarget, setRoleTarget] = useState<DocumentPersonResponse | null>(null)
  const [removeTarget, setRemoveTarget] = useState<DocumentPersonResponse | null>(null)

  const columns = useMemo<DataTableColumn<DocumentPersonResponse>[]>(
    () => [
      {
        id: 'name',
        headerKey: 'docdetails.persons.column.name',
        hideable: false,
        sortable: true,
        sortValue: (person) => person.displayName,
        cell: (person) =>
          canViewPersons ? (
            <Link to={ROUTES.PERSON_DETAIL(person.personId)} className="text-sm font-medium text-accent hover:underline">
              {person.displayName}
            </Link>
          ) : (
            <span className="text-sm font-medium text-foreground">{person.displayName}</span>
          ),
      },
      {
        id: 'role',
        headerKey: 'docdetails.persons.column.role',
        cell: (person) => <LookupLabel lookup={LOOKUP_SLUGS.DocumentPersonRoles} id={person.roleId} />,
      },
      {
        id: 'createdAt',
        headerKey: 'docdetails.persons.column.created-at',
        sortable: true,
        sortValue: (person) => person.createdAt,
        cell: (person) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(person.createdAt)}
          </span>
        ),
      },
    ],
    [canViewPersons],
  )

  const buildRowMenu = (person: DocumentPersonResponse): MenuEntry[] => [
    ...(canViewPersons
      ? [
          {
            id: 'open',
            label: t('docdetails.persons.row.open'),
            icon: <ExternalLink className="size-4" aria-hidden />,
            onSelect: () => navigate(ROUTES.PERSON_DETAIL(person.personId)),
          } satisfies MenuEntry,
        ]
      : []),
    ...(canManage
      ? [
          {
            id: 'role',
            label: t('docdetails.persons.row.change-role'),
            icon: <Pencil className="size-4" aria-hidden />,
            onSelect: () => setRoleTarget(person),
          } satisfies MenuEntry,
          {
            id: 'remove',
            label: t('docdetails.persons.row.remove'),
            icon: <Trash2 className="size-4" aria-hidden />,
            danger: true,
            onSelect: () => setRemoveTarget(person),
          } satisfies MenuEntry,
        ]
      : []),
  ]

  const confirmRemove = async () => {
    if (!removeTarget) return
    try {
      await removeMutation.mutateAsync({ documentId, documentPersonId: removeTarget.documentPersonId })
      toast.success(t('docdetails.persons.toast.removed'))
      setRemoveTarget(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const addButton = canManage ? (
    <Button type="button" onClick={() => setAddOpen(true)}>
      <UserPlus className="size-4" aria-hidden />
      <Label k="docdetails.action.add-persons" />
    </Button>
  ) : undefined

  return (
    <div className="flex flex-col gap-3">
      {frozenReason && <p className="rounded-lg bg-surface-muted px-3 py-2 text-sm text-muted">{frozenReason}</p>}

      <DataTable
        tableId="document-persons"
        mode="client"
        caption={t('docdetails.persons.caption')}
        columns={columns}
        rows={personsQuery.data}
        rowKey={(person) => person.documentPersonId}
        isLoading={personsQuery.isLoading}
        error={personsQuery.error}
        onRetry={() => void personsQuery.refetch()}
        onRefresh={() => void personsQuery.refetch()}
        isFetching={personsQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={personsQuery.data?.length ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        searchPlaceholder={t('docdetails.persons.search')}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        rowSearchText={(person) => person.displayName}
        rowLabel={(person) => person.displayName}
        rowMenu={buildRowMenu}
        toolbar={addButton}
        emptyTitle={t('docdetails.persons.empty-title')}
        emptyDescription={t('docdetails.persons.empty-description')}
        emptyAction={addButton}
      />

      {addOpen && <AddDocumentPersonsDialog documentId={documentId} open onOpenChange={setAddOpen} />}
      {roleTarget && (
        <ChangeDocumentPersonRoleDialog
          key={roleTarget.documentPersonId}
          documentId={documentId}
          person={roleTarget}
          open
          onOpenChange={(open) => {
            if (!open) setRoleTarget(null)
          }}
        />
      )}

      <ConfirmDialog
        open={removeTarget !== null}
        onOpenChange={(open) => {
          if (!open) setRemoveTarget(null)
        }}
        title={t('docdetails.persons.remove-title')}
        description={
          removeTarget ? t('docdetails.persons.remove-description', { name: removeTarget.displayName }) : undefined
        }
        confirmLabel={t('docdetails.persons.remove-confirm')}
        destructive
        loading={removeMutation.isPending}
        onConfirm={() => void confirmRemove()}
      />
    </div>
  )
}
