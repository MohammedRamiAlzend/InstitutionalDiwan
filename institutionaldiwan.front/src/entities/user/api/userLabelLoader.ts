import { httpClient } from '@/shared/api'
import type { UserOption } from '../model/types'

const MAX_IDS_PER_REQUEST = 100

interface Waiter {
  resolve: (option: UserOption | null) => void
}

const waiting = new Map<string, Waiter[]>()
let timer: ReturnType<typeof setTimeout> | null = null

/**
 * Resolves user ids to labels, coalescing every id asked for in the same tick into one
 * `GET /api/identity-options/users/by-ids` call (a table with fifty rows asks once, not fifty
 * times). An id the server does not know, or a call the caller may not make, resolves to `null`
 * so the cell falls back to the bare identifier instead of failing.
 */
export function loadUserLabel(userId: string): Promise<UserOption | null> {
  return new Promise((resolve) => {
    const waiters = waiting.get(userId)
    if (waiters) waiters.push({ resolve })
    else waiting.set(userId, [{ resolve }])

    timer ??= setTimeout(() => void flush(), 0)
  })
}

async function flush(): Promise<void> {
  timer = null
  const batch = new Map(waiting)
  waiting.clear()

  const ids = [...batch.keys()]
  for (let start = 0; start < ids.length; start += MAX_IDS_PER_REQUEST) {
    const chunk = ids.slice(start, start + MAX_IDS_PER_REQUEST)
    let found = new Map<string, UserOption>()

    try {
      const { data } = await httpClient.get<UserOption[]>('/identity-options/users/by-ids', {
        params: { ids: chunk },
        paramsSerializer: { indexes: null },
      })
      found = new Map(data.map((option) => [option.userId, option]))
    } catch {
      // Not allowed or unavailable: every id in the chunk falls back to its identifier.
    }

    for (const id of chunk) {
      const option = found.get(id) ?? null
      for (const waiter of batch.get(id) ?? []) waiter.resolve(option)
    }
  }
}
