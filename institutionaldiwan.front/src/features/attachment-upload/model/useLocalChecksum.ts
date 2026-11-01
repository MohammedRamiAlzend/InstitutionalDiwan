import { useCallback, useRef, useState } from 'react'
import { ChecksumUnsupportedError, sha256Hex } from '@/shared/lib'

export interface LocalChecksum {
  /** Hex digest of the picked file, or `null` while none is known. */
  digest: string | null
  error: string | null
  isHashing: boolean
  /** Hashes `file`; a pick that lands after a newer one is discarded. */
  compute: (file: File | null) => Promise<void>
  /** Drops the digest and invalidates a hash still running — a cleared pick must not report back. */
  clear: () => void
}

/**
 * The SHA-256 of the file sitting on the user's disk, computed before upload so the stored checksum
 * can be compared with it afterwards. That comparison covers the transfer as well as the storage.
 */
export function useLocalChecksum(): LocalChecksum {
  const [digest, setDigest] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isHashing, setIsHashing] = useState(false)

  // A fast file swap must not let an older digest overwrite the newer one.
  const token = useRef(0)

  const clear = useCallback(() => {
    token.current += 1
    setDigest(null)
    setError(null)
    setIsHashing(false)
  }, [])

  const compute = useCallback(async (file: File | null) => {
    const current = ++token.current
    setDigest(null)
    setError(null)

    if (!file || file.size === 0) {
      setIsHashing(false)
      return
    }

    setIsHashing(true)
    try {
      const value = await sha256Hex(file)
      if (current === token.current) setDigest(value)
    } catch (cause) {
      if (current === token.current) {
        setError(
          cause instanceof ChecksumUnsupportedError
            ? cause.message
            : 'تعذّر حساب البصمة المحلية لهذا الملف.',
        )
      }
    } finally {
      if (current === token.current) setIsHashing(false)
    }
  }, [])

  return { digest, error, isHashing, compute, clear }
}
