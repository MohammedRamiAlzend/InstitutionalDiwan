import { create } from 'zustand'
import { LABEL_DEFAULTS, type LabelKey } from '@/shared/config'

const CACHE_KEY = 'diwan.uiLabels.v1'

/** The server snapshot: a version (newest override ticks, 0 when empty) plus the changed keys only. */
export interface UiLabelSnapshot {
  version: number
  overrides: Record<string, string>
}

export type LabelParams = Record<string, string | number>

interface LabelsState {
  overrides: Record<string, string>
  version: number
  /** True once the boot gate may reveal the app (cache applied, fetch settled, or no session). */
  ready: boolean
  applySnapshot: (snapshot: UiLabelSnapshot) => void
  markReady: () => void
}

/** Defensive parse: a corrupt or partial cache is ignored, never thrown. */
function readCache(): UiLabelSnapshot | null {
  try {
    const raw = localStorage.getItem(CACHE_KEY)
    if (!raw) return null
    const parsed: unknown = JSON.parse(raw)
    if (typeof parsed !== 'object' || parsed === null) return null
    const { version, overrides } = parsed as { version?: unknown; overrides?: unknown }
    if (typeof version !== 'number' || typeof overrides !== 'object' || overrides === null) {
      return null
    }
    const clean: Record<string, string> = {}
    for (const [key, value] of Object.entries(overrides)) {
      if (typeof value === 'string') clean[key] = value
    }
    return { version, overrides: clean }
  } catch {
    return null
  }
}

function writeCache(snapshot: UiLabelSnapshot): void {
  try {
    localStorage.setItem(CACHE_KEY, JSON.stringify(snapshot))
  } catch {
    // Storage can be unavailable; labels still work from the in-memory snapshot.
  }
}

export const useLabelsStore = create<LabelsState>()((set) => {
  // Seed from the local cache at creation time so a returning browser paints its stored
  // labels on the very first render — no loading-screen flash. No cache → not ready yet.
  const cached = readCache()
  return {
    overrides: cached?.overrides ?? {},
    version: cached?.version ?? 0,
    ready: cached !== null,

    applySnapshot: (snapshot) => {
      writeCache(snapshot)
      set({ overrides: snapshot.overrides, version: snapshot.version, ready: true })
    },

    markReady: () => set({ ready: true }),
  }
})

function interpolate(text: string, params?: LabelParams): string {
  if (!params) return text
  return text.replace(/\{(\w+)\}/g, (match, name: string) =>
    name in params ? String(params[name]) : match,
  )
}

/** Pure resolution: the override when present, otherwise the built-in default, with placeholders filled. */
export function resolveLabel(
  key: LabelKey,
  overrides: Record<string, string>,
  params?: LabelParams,
): string {
  return interpolate(overrides[key] ?? LABEL_DEFAULTS[key], params)
}

/**
 * Non-reactive lookup for string props (toast, aria-label, placeholder, error messages).
 * Components that must repaint on a rename use `useLabel` or subscribe to the store.
 */
export function t(key: LabelKey, params?: LabelParams): string {
  return resolveLabel(key, useLabelsStore.getState().overrides, params)
}

/** Reactive lookup; the component re-renders whenever the resolved text changes. */
export function useLabel(key: LabelKey, params?: LabelParams): string {
  return useLabelsStore((state) => resolveLabel(key, state.overrides, params))
}