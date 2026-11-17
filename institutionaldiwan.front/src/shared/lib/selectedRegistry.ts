import { useSyncExternalStore } from 'react'

const STORAGE_KEY = 'diwan.documents.selectedRegistry.v1'
const CHANGE_EVENT = 'diwan:selected-registry-changed'

export function readSelectedRegistryId(): string | null {
  if (typeof localStorage === 'undefined') return null
  try {
    return localStorage.getItem(STORAGE_KEY)
  } catch {
    // Storage can be unavailable; no preference simply means the first register.
    return null
  }
}

export function writeSelectedRegistryId(registryId: string): void {
  if (typeof localStorage !== 'undefined') {
    try {
      localStorage.setItem(STORAGE_KEY, registryId)
    } catch {
      // Quota or privacy mode: the register choice is a convenience, not required state.
    }
  }
  window.dispatchEvent(new Event(CHANGE_EVENT))
}

function subscribe(onChange: () => void): () => void {
  window.addEventListener(CHANGE_EVENT, onChange)
  window.addEventListener('storage', onChange)
  return () => {
    window.removeEventListener(CHANGE_EVENT, onChange)
    window.removeEventListener('storage', onChange)
  }
}

/** The stored register choice, live across this tab's writes and other tabs' storage events. */
export function useSelectedRegistryId(): string | null {
  return useSyncExternalStore(subscribe, readSelectedRegistryId, () => null)
}

/** The stored choice when it is still one of the options, else the first option. */
export function resolveSelectedRegistryId(
  stored: string | null,
  options: readonly { value: string }[],
): string | null {
  if (stored && options.some((option) => option.value === stored)) return stored
  return options[0]?.value ?? null
}