import type { DocumentFileResponse } from '@/entities/document-file'
import { uploadForm } from '@/shared/api'

export interface UploadVersionInput {
  fileId: string
  file: File
  changeNotes: string
  /** 0–100 while the body is in flight; the dialog shows the last value until the response lands. */
  onProgress?: (percent: number) => void
}

/**
 * `POST /api/document-files/{fileId}/versions` — stores the next immutable version of an existing
 * file. The type, title, and primary-scan flag belong to the file and are not resent; the version
 * number is assigned by the server. An archived file and an inactive file type are both rejected
 * before anything is stored.
 */
export function uploadVersion(input: UploadVersionInput): Promise<DocumentFileResponse> {
  const form = new FormData()
  form.append('file', input.file)
  // Omitted rather than sent empty: the server parameter is nullable, and an empty note would be
  // stored as a blank string instead of the absent value the field represents.
  if (input.changeNotes) form.append('changeNotes', input.changeNotes)

  return uploadForm<DocumentFileResponse>(
    `/document-files/${input.fileId}/versions`,
    form,
    { onProgress: input.onProgress },
  )
}
