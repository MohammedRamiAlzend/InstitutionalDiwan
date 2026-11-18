import { useState } from 'react'
import { Loader2, RefreshCw } from 'lucide-react'
import {
  ChecksumBadge,
  currentVersionOf,
  useVerifyChecksum,
  type DocumentFileResponse,
} from '@/entities/document-file'
import { toArabicErrorMessage } from '@/shared/api'
import { formatShortId, t } from '@/shared/lib'
import { Button, Dialog, Input, Label } from '@/shared/ui'
import { useLocalChecksum } from '../model/useLocalChecksum'

interface VerifyChecksumDialogProps {
  file: DocumentFileResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

function DigestLine({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <p className="flex flex-wrap items-baseline gap-1.5 text-xs">
      <span className="text-muted">{label}:</span>
      <code dir="ltr" className="break-all text-foreground" title={value}>
        {formatShortId(value)}
      </code>
      {hint && <span className="text-muted">{hint}</span>}
    </p>
  )
}

function ComparisonLine({ label, isMatch }: { label: string; isMatch: boolean | null }) {
  return (
    <p className="flex flex-wrap items-center gap-2 text-xs">
      <span className="text-muted">{label}</span>
      {isMatch === null ? (
        <span className="text-muted"><Label k="attachment.verify.unavailable" /></span>
      ) : (
        <ChecksumBadge isMatch={isMatch} />
      )}
    </p>
  )
}

/**
 * Verifies the current version's integrity: the server re-reads the stored bytes and recomputes
 * SHA-256, and the dialog lines that up against the checksum recorded at upload time. Picking the
 * same file from disk adds a third digest, so a transfer error can be told apart from a storage one.
 */
export function VerifyChecksumDialog({
  file,
  open,
  onOpenChange,
}: VerifyChecksumDialogProps) {
  const version = currentVersionOf(file)
  const verifyQuery = useVerifyChecksum({ fileId: file.fileId }, { enabled: open })
  const checksum = useLocalChecksum()
  const [pickedName, setPickedName] = useState<string | null>(null)

  const result = verifyQuery.data
  const recorded = result?.recordedChecksumSha256 ?? version?.checksumSha256 ?? ''
  const computed = result?.computedChecksumSha256 ?? ''
  const local = checksum.digest

  const lower = (value: string) => value.toLowerCase()
  const matchesLocal = (value: string) => (local ? lower(value) === lower(local) : null)

  const pickFile = async (next: File | null) => {
    setPickedName(next?.name ?? null)
    await checksum.compute(next)
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('attachment.verify.title')}
      description={t('attachment.verify.description', { version: result?.versionNumber ?? version?.versionNumber ?? t('attachment.versions.current'), title: file.title })}
      size="md"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => void verifyQuery.refetch()}
            loading={verifyQuery.isFetching}
          >
            <RefreshCw className="size-4" aria-hidden />
            <Label k="attachment.verify.reverify" />
          </Button>
          <Button type="button" onClick={() => onOpenChange(false)}>
            <Label k="common.action.close" />
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-3 rounded-lg border border-border bg-surface-muted/30 p-3">
        {verifyQuery.isPending ? (
          <p className="flex items-center gap-2 text-sm text-muted">
            <Loader2 className="size-4 animate-spin" aria-hidden />
            <Label k="attachment.verify.pending" />
          </p>
        ) : verifyQuery.error ? (
          <p role="alert" className="text-sm text-danger">
            {toArabicErrorMessage(verifyQuery.error)}
          </p>
        ) : result ? (
          <>
            <DigestLine label={t('attachment.verify.recorded')} value={recorded} hint={t('attachment.verify.recorded-hint')} />
            <DigestLine label={t('attachment.verify.computed')} value={computed} hint={t('attachment.verify.computed-hint')} />
            {local && (
              <DigestLine
                label={t('attachment.verify.local')}
                value={local}
                hint={t('attachment.verify.local-hint', { name: pickedName ?? t('attachment.verify.picked-fallback') })}
              />
            )}
          </>
        ) : null}
      </div>

      {result && (
        <div className="flex flex-col gap-1.5 rounded-lg border border-border bg-surface p-3">
          <ComparisonLine label={t('attachment.verify.compare-recorded-computed')} isMatch={result.isMatch} />
          <ComparisonLine label={t('attachment.verify.compare-recorded-local')} isMatch={matchesLocal(recorded)} />
          <ComparisonLine label={t('attachment.verify.compare-computed-local')} isMatch={matchesLocal(computed)} />
          {!local && (
            <p className="text-xs text-muted">
              <Label k="attachment.verify.pick-hint" />
            </p>
          )}
        </div>
      )}

      {!verifyQuery.isPending && !verifyQuery.error && (
        <div className="flex flex-col gap-1">
          <Input
            id="attachment-verify-file"
            type="file"
            dir="ltr"
            onChange={(event) => void pickFile(event.target.files?.[0] ?? null)}
            disabled={checksum.isHashing}
            className="text-xs"
          />
          {checksum.isHashing && (
            <p className="flex items-center gap-1.5 text-xs text-muted">
              <Loader2 className="size-3.5 animate-spin" aria-hidden />
              <Label k="attachment.verify.local-hashing" />
            </p>
          )}
          {checksum.error && <p className="text-xs text-danger">{checksum.error}</p>}
        </div>
      )}

      <p className="text-xs text-muted">
        <Label k="attachment.verify.footnote" />
      </p>
    </Dialog>
  )
}
