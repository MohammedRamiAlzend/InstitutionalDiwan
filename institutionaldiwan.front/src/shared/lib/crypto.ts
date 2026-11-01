/** Raised when the context offers no WebCrypto, so no checksum can be computed at all. */
export class ChecksumUnsupportedError extends Error {
  constructor() {
    super('تعذّر حساب البصمة في هذا السياق؛ يلزم اتصال آمن (HTTPS) أو localhost.')
    this.name = 'ChecksumUnsupportedError'
  }
}

function toHex(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer)
  let hex = ''
  for (const byte of bytes) {
    hex += byte.toString(16).padStart(2, '0')
  }
  return hex
}

/**
 * Lowercase 64-character hex SHA-256, directly comparable with the checksum the server records
 * (`LocalFileStorageService.ComputeSha256Async`). The whole input is read into memory once, so this
 * is sized for the uploads the API allows (up to ~50 MB), not for arbitrary streams.
 */
export async function sha256Hex(input: Blob | ArrayBuffer): Promise<string> {
  const subtle = globalThis.crypto?.subtle
  if (!subtle) throw new ChecksumUnsupportedError()

  const buffer = input instanceof Blob ? await input.arrayBuffer() : input
  return toHex(await subtle.digest('SHA-256', buffer))
}
