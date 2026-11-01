import type { DocumentFileResponse } from '@/entities/document-file'
import { uploadForm } from '@/shared/api'

export interface UploadInitialInput {
  documentId: string
  file: File
  fileTypeId: number
  title: string
  /** Always sent: the server parameter is non-nullable, so an absent field is not an option. */
  isPrimaryScan: boolean
  changeNotes: string
  /** 0–100 while the body is in flight; the dialog shows the last value until the response lands. */
  onProgress?: (percent: number) => void
}

/**
 * `POST /api/documents/{documentId}/files` — creates the file and stores version 1 atomically, so a
 * rejected extension or size never leaves a metadata-only file behind. Returns the file with its
 * first version, whose `checksumSha256` the caller compares with the locally computed one.
 */
export function uploadInitial(input: UploadInitialInput): Promise<DocumentFileResponse> {
  const form = new FormData()
  form.append('file', input.file)
  form.append('fileTypeId', String(input.fileTypeId))
  form.append('title', input.title)
  form.append('isPrimaryScan', String(input.isPrimaryScan))
  if (input.changeNotes) form.append('changeNotes', input.changeNotes)

  return uploadForm<DocumentFileResponse>(
    `/documents/${input.documentId}/files`,
    form,
    { onProgress: input.onProgress },
  )
}
