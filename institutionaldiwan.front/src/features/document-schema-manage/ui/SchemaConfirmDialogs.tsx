import { toast } from 'sonner'
import type { SchemaFieldResponse, SchemaVersionResponse } from '@/entities/document-schema'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { ConfirmDialog } from '@/shared/ui'
import {
  useDeleteDraftMutation,
  usePublishVersionMutation,
  useRemoveVersionFieldMutation,
} from '../api/schemaMutations'

interface VersionDialogProps {
  version: SchemaVersionResponse | null
  onOpenChange: (open: boolean) => void
}

/** Publishing freezes the draft; documents already created keep the version they were pinned to. */
export function PublishVersionDialog({ version, onOpenChange }: VersionDialogProps) {
  const mutation = usePublishVersionMutation()

  return (
    <ConfirmDialog
      open={version !== null}
      onOpenChange={(open) => !mutation.isPending && onOpenChange(open)}
      title={t('schema.publish.title', { version: version?.versionNumber ?? '' })}
      description={
        mutation.error
          ? toArabicErrorMessage(mutation.error)
          : t('schema.publish.description')
      }
      confirmLabel={t('schema.publish.confirm')}
      loading={mutation.isPending}
      onConfirm={() => {
        if (!version) return
        mutation.mutate(version.schemaVersionId, {
          onSuccess: () => {
            toast.success(t('schema.publish.toast'))
            onOpenChange(false)
          },
        })
      }}
    />
  )
}

/** Deletes the single draft; published and retired versions can never be deleted. */
export function DeleteDraftDialog({ version, onOpenChange }: VersionDialogProps) {
  const mutation = useDeleteDraftMutation()

  return (
    <ConfirmDialog
      open={version !== null}
      onOpenChange={(open) => !mutation.isPending && onOpenChange(open)}
      title={t('schema.delete.title')}
      description={
        mutation.error
          ? toArabicErrorMessage(mutation.error)
          : t('schema.delete.description')
      }
      confirmLabel={t('schema.delete.confirm')}
      destructive
      loading={mutation.isPending}
      onConfirm={() => {
        if (!version) return
        mutation.mutate(version.schemaVersionId, {
          onSuccess: () => {
            toast.success(t('schema.delete.toast'))
            onOpenChange(false)
          },
        })
      }}
    />
  )
}

interface RemoveFieldDialogProps {
  version: SchemaVersionResponse | null
  field: SchemaFieldResponse | null
  onOpenChange: (open: boolean) => void
}

/** Removes a field from the draft only; values documents already stored for it are kept. */
export function RemoveFieldDialog({ version, field, onOpenChange }: RemoveFieldDialogProps) {
  const mutation = useRemoveVersionFieldMutation(version?.schemaVersionId ?? '')

  return (
    <ConfirmDialog
      open={version !== null && field !== null}
      onOpenChange={(open) => !mutation.isPending && onOpenChange(open)}
      title={t('schema.remove.title', { label: field?.labelAr ?? '' })}
      description={
        mutation.error
          ? toArabicErrorMessage(mutation.error)
          : t('schema.remove.description')
      }
      confirmLabel={t('schema.remove.confirm')}
      destructive
      loading={mutation.isPending}
      onConfirm={() => {
        if (!field) return
        mutation.mutate(field.documentFieldId, {
          onSuccess: () => {
            toast.success(t('schema.remove.toast'))
            onOpenChange(false)
          },
        })
      }}
    />
  )
}
