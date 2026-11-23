/**
 * Canvas helpers for the in-app Drive image editor. The bytes come from the authenticated
 * `GET /api/drive/files/{id}/content` route and are read as a data URL, so the canvas stays
 * same-origin and can be re-encoded. Only PNG and JPEG are editable: a save re-encodes to the file's
 * own MIME type, and those two are the formats the server's magic-byte check accepts verbatim.
 */

/** Extension → MIME type for the formats this editor can save back without changing the file. */
const EDITABLE_MIME: Record<string, string> = {
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
}

function extensionOf(fileName: string): string {
  const normalized = fileName.toLowerCase()
  const dot = normalized.lastIndexOf('.')
  return dot < 0 ? '' : normalized.slice(dot)
}

/** The MIME type a save must produce, or null when the file is not editable here. */
export function editableImageMimeType(fileName: string): string | null {
  return EDITABLE_MIME[extensionOf(fileName)] ?? null
}

/** True for `.png`/`.jpg`/`.jpeg`, the images the canvas editor can save as a new version. */
export function isEditableImage(fileName: string): boolean {
  return editableImageMimeType(fileName) !== null
}

/** Reads a blob as a base64 data URL (used instead of an object URL so the canvas never taints). */
export function blobToDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(String(reader.result))
    reader.onerror = () => reject(reader.error ?? new Error('FileReader failed'))
    reader.readAsDataURL(blob)
  })
}

/** Decodes a data URL into a loaded, fully decoded image element. */
export function loadImageElement(url: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image()
    image.onload = () => resolve(image)
    image.onerror = () => reject(new Error('Image decode failed'))
    image.src = url
  })
}

function canvasContext(canvas: HTMLCanvasElement): CanvasRenderingContext2D {
  const context = canvas.getContext('2d')
  if (!context) throw new Error('Canvas is unavailable')
  return context
}

export interface ImageTransform {
  /** Clockwise degrees: 0, 90, 180 or 270. */
  rotate?: 0 | 90 | 180 | 270
  flipHorizontal?: boolean
  flipVertical?: boolean
}

/** Rotates/flips the current image and returns a lossless PNG data URL of the result. */
export async function transformImageDataUrl(
  url: string,
  { rotate = 0, flipHorizontal = false, flipVertical = false }: ImageTransform,
): Promise<string> {
  const image = await loadImageElement(url)
  const sourceWidth = image.naturalWidth
  const sourceHeight = image.naturalHeight
  const swapped = rotate === 90 || rotate === 270

  const canvas = document.createElement('canvas')
  canvas.width = swapped ? sourceHeight : sourceWidth
  canvas.height = swapped ? sourceWidth : sourceHeight

  const context = canvasContext(canvas)
  context.translate(canvas.width / 2, canvas.height / 2)
  context.rotate((rotate * Math.PI) / 180)
  context.scale(flipHorizontal ? -1 : 1, flipVertical ? -1 : 1)
  context.drawImage(image, -sourceWidth / 2, -sourceHeight / 2)

  return canvas.toDataURL('image/png')
}

export interface ImageCropRect {
  x: number
  y: number
  width: number
  height: number
}

/** Crops a pixel rectangle out of the current image and returns a PNG data URL of the result. */
export async function cropImageDataUrl(url: string, rect: ImageCropRect): Promise<string> {
  const image = await loadImageElement(url)
  const x = Math.max(0, Math.round(rect.x))
  const y = Math.max(0, Math.round(rect.y))
  const width = Math.min(Math.round(rect.width), image.naturalWidth - x)
  const height = Math.min(Math.round(rect.height), image.naturalHeight - y)
  if (width < 1 || height < 1) throw new Error('Empty crop')

  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height
  canvasContext(canvas).drawImage(image, x, y, width, height, 0, 0, width, height)

  return canvas.toDataURL('image/png')
}

/** Re-encodes the current image to the given MIME type; null when the browser cannot encode it. */
export async function encodeImage(url: string, mimeType: string): Promise<Blob | null> {
  const image = await loadImageElement(url)
  const canvas = document.createElement('canvas')
  canvas.width = image.naturalWidth
  canvas.height = image.naturalHeight
  canvasContext(canvas).drawImage(image, 0, 0)

  return new Promise((resolve) => {
    canvas.toBlob((blob) => resolve(blob && blob.type === mimeType ? blob : null), mimeType, 0.92)
  })
}
