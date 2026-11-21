/**
 * The Drive workspace's browse modes. Each maps to a different read: `my-files`/`shared` browse a
 * space root, `starred` browses with `starred=true`, `trash` reads the trash list, and `deletion`
 * reads deletion requests. The active mode lives in the URL (`?view=`) so it is shareable.
 */
export type DriveView = 'my-files' | 'shared' | 'starred' | 'trash' | 'deletion'

export const DRIVE_VIEWS: readonly DriveView[] = [
  'my-files',
  'shared',
  'starred',
  'trash',
  'deletion',
]

export function isDriveView(value: string | null): value is DriveView {
  return value !== null && (DRIVE_VIEWS as readonly string[]).includes(value)
}