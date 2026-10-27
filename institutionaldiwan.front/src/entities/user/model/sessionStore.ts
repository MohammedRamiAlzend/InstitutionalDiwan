import { create } from 'zustand'
import type { PermissionCode } from '@/shared/config'
import type { SessionUser } from './types'

interface SessionState {
  user: SessionUser | null
  mustChangePassword: boolean
  setSession: (user: SessionUser, mustChangePassword: boolean) => void
  clear: () => void
  hasPermission: (code: PermissionCode) => boolean
}

export const useSessionStore = create<SessionState>()((set, get) => ({
  user: null,
  mustChangePassword: false,

  setSession: (user, mustChangePassword) => set({ user, mustChangePassword }),

  clear: () => set({ user: null, mustChangePassword: false }),

  hasPermission: (code) => {
    const { user } = get()
    return user !== null && user.permissions.includes(code)
  },
}))
