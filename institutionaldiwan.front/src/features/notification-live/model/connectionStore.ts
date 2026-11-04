import { create } from 'zustand'

/**
 * `idle` — no signed-in user, nothing to connect.
 * `connecting` — first attempt; `connected` — hub open; `reconnecting` — retrying after a drop;
 * `disconnected` — gave up for now and waiting for the next retry timer.
 */
export type NotificationConnectionState =
  | 'idle'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'disconnected'

interface ConnectionStore {
  state: NotificationConnectionState
  setState: (state: NotificationConnectionState) => void
}

export const useConnectionStore = create<ConnectionStore>()((set) => ({
  state: 'idle',
  setState: (state) => set({ state }),
}))

/** Current live-connection state for the bell indicator and the unread-count polling fallback. */
export function useNotificationConnection(): NotificationConnectionState {
  return useConnectionStore((store) => store.state)
}
