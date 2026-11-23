/** How a Drive file can be previewed in the browser, derived from its extension. */
export type DrivePreviewKind =
  | 'image'
  | 'pdf'
  | 'video'
  | 'audio'
  | 'office'
  | 'text'
  | 'unsupported'

const IMAGE = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.svg']
const VIDEO = ['.mp4', '.webm', '.mkv', '.mov']
const AUDIO = ['.mp3', '.wav']
const OFFICE = ['.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx']
const TEXT = ['.txt', '.md']

function extensionOf(name: string): string {
  const normalized = name.toLowerCase()
  const dot = normalized.lastIndexOf('.')
  return dot < 0 ? '' : normalized.slice(dot)
}

export function drivePreviewKind(fileName: string): DrivePreviewKind {
  const extension = extensionOf(fileName)
  if (IMAGE.includes(extension)) return 'image'
  if (extension === '.pdf') return 'pdf'
  if (VIDEO.includes(extension)) return 'video'
  if (AUDIO.includes(extension)) return 'audio'
  if (OFFICE.includes(extension)) return 'office'
  if (TEXT.includes(extension)) return 'text'
  return 'unsupported'
}

/** Kinds rendered from a presigned GET URL; office/unsupported fall back to a download. */
export function isUrlPreviewable(kind: DrivePreviewKind): boolean {
  return kind === 'image' || kind === 'pdf' || kind === 'video' || kind === 'audio'
}