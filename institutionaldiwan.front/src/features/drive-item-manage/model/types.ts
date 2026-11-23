/** `POST /api/drive/items/folders` — a folder inside a space (root when `parentId` is null). */
export interface CreateDriveFolderRequest {
  spaceId: string
  parentId?: string | null
  name: string
}

/** `POST /api/drive/files` — an empty Word/Excel/text/Markdown file with a committed version 1. */
export interface CreateDriveFileRequest {
  spaceId: string
  parentId?: string | null
  fileName: string
}

/** `PUT /api/drive/items/{itemId}/rename` — Editor access; sibling-unique name. */
export interface RenameDriveItemRequest {
  name: string
}

/** `PUT /api/drive/items/{itemId}/move` — null destination moves to the space root. */
export interface MoveDriveItemRequest {
  newParentId?: string | null
}