function safeDecode(value: string): string | null {
  try {
    return decodeURIComponent(value.trim())
  } catch {
    return null
  }
}

function plainFileName(header: string): string | null {
  const match = /filename\s*=\s*("?)([^";]+)\1/i.exec(header)
  return match ? match[2].trim() : null
}

/**
 * File name from a `Content-Disposition` header. Prefers the RFC 5987 `filename*=UTF-8''…` form
 * because uploaded names are often Arabic and only that form carries them undamaged, then falls back
 * to plain `filename="…"`. Any path segment is stripped: the value comes from the server and must
 * not steer where the file lands on disk.
 */
export function parseContentDisposition(header: string | null | undefined): string | null {
  if (!header) return null

  const extended = /filename\*\s*=\s*[^']*'[^']*'([^;]+)/i.exec(header)
  const candidate = (extended && safeDecode(extended[1])) || plainFileName(header)
  const baseName = candidate?.replace(/\\/g, '/').split('/').pop()?.trim()

  return baseName || null
}

/** Triggers a browser download of `blob` under `fileName`. */
export function saveBlobAsFile(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()

  // Revoking in the same tick can abort the download before the browser picks the blob up.
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
}
