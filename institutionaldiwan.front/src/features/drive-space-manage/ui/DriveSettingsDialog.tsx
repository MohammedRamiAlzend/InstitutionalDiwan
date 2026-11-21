import { useState } from 'react'
import { toast } from 'sonner'
import { useDriveSettings, type DriveSettingsResponse } from '@/entities/drive-space'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { Button, Dialog, FormField, Input, Label } from '@/shared/ui'
import { useUpdateDriveSettingsMutation } from '../api/driveSpaceMutations'

export function DriveSettingsDialog({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const query = useDriveSettings({ enabled: open })
  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('drive.dialog.settings-title')}
      description={t('drive.deletion.approver-hint')}
      footer={
        <>
          <Button variant="outline" type="button" onClick={() => onOpenChange(false)}><Label k="common.action.cancel" /></Button>
        </>
      }
    >
      {query.isLoading ? <p className="py-6 text-center text-sm text-muted"><Label k="drive.state.loading" /></p> : query.error ? (
        <p className="py-6 text-center text-sm text-danger">{toArabicErrorMessage(query.error)}</p>
      ) : (
        query.data ? <DriveSettingsForm settings={query.data} onClose={() => onOpenChange(false)} /> : null
      )}
    </Dialog>
  )
}

function DriveSettingsForm({ settings, onClose }: { settings: DriveSettingsResponse; onClose: () => void }) {
  const mutation = useUpdateDriveSettingsMutation()
  const [quota, setQuota] = useState(() => String(settings.defaultQuotaBytes))
  const [maxFile, setMaxFile] = useState(() => String(settings.maxFileSizeBytes))
  const [extensions, setExtensions] = useState(() => settings.allowedExtensionsCsv)

  const submit = async () => {
    const defaultQuotaBytes = Number(quota)
    const maxFileSizeBytes = Number(maxFile)
    if (!Number.isFinite(defaultQuotaBytes) || defaultQuotaBytes <= 0 || !Number.isFinite(maxFileSizeBytes) || maxFileSizeBytes <= 0) {
      toast.error(t('drive.validation.quota-positive'))
      return
    }
    try {
      await mutation.mutateAsync({ defaultQuotaBytes, maxFileSizeBytes, allowedExtensionsCsv: extensions.trim() })
      toast.success(t('drive.toast.settings-updated'))
      onClose()
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  return (
    <>
      <div className="grid gap-4">
        <FormField labelKey="drive.dialog.settings-default-quota">
          <Input value={quota} onChange={(event) => setQuota(event.target.value)} inputMode="numeric" dir="ltr" />
        </FormField>
        <FormField labelKey="drive.dialog.settings-max-file">
          <Input value={maxFile} onChange={(event) => setMaxFile(event.target.value)} inputMode="numeric" dir="ltr" />
        </FormField>
        <FormField labelKey="drive.dialog.settings-extensions">
          <Input value={extensions} onChange={(event) => setExtensions(event.target.value)} dir="ltr" />
        </FormField>
      </div>
      <div className="mt-4 flex justify-end">
        <Button type="button" onClick={() => void submit()} loading={mutation.isPending}><Label k="common.action.save" /></Button>
      </div>
    </>
  )
}
