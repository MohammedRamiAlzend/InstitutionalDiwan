import { toast } from 'sonner'
import type { SchemaFieldResponse, SchemaVersionResponse } from '@/entities/document-schema'
import { toArabicErrorMessage } from '@/shared/api'
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
      title={`نشر النسخة v${version?.versionNumber ?? ''}`}
      description={
        mutation.error
          ? toArabicErrorMessage(mutation.error)
          : 'ستتجمّد النسخة ولن تُعدَّل، وتتقاعد النسخة المنشورة السابقة. الوثائق القائمة تبقى على نسختها، والوثائق الجديدة تستعمل هذه النسخة.'
      }
      confirmLabel="نشر"
      loading={mutation.isPending}
      onConfirm={() => {
        if (!version) return
        mutation.mutate(version.schemaVersionId, {
          onSuccess: () => {
            toast.success('نُشرت النسخة.')
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
      title="حذف المسودة"
      description={
        mutation.error
          ? toArabicErrorMessage(mutation.error)
          : 'ستُحذف المسودة بإعدادات حقولها. تبقى تعريفات الحقول وكل النسخ المنشورة كما هي.'
      }
      confirmLabel="حذف"
      destructive
      loading={mutation.isPending}
      onConfirm={() => {
        if (!version) return
        mutation.mutate(version.schemaVersionId, {
          onSuccess: () => {
            toast.success('حُذفت المسودة.')
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
      title={`إزالة الحقل «${field?.labelAr ?? ''}»`}
      description={
        mutation.error
          ? toArabicErrorMessage(mutation.error)
          : 'يُزال الحقل من هذه المسودة فقط، ولا تتأثر النسخ المنشورة ولا القيم المخزّنة في الوثائق.'
      }
      confirmLabel="إزالة"
      destructive
      loading={mutation.isPending}
      onConfirm={() => {
        if (!field) return
        mutation.mutate(field.documentFieldId, {
          onSuccess: () => {
            toast.success('أُزيل الحقل من المسودة.')
            onOpenChange(false)
          },
        })
      }}
    />
  )
}
