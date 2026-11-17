import { useSyncExternalStore } from 'react'

export type LabelEditMode = 'on' | 'off'

const LABEL_EDIT_KEY = 'diwan.labelEdit'

/**
 * Per-browser inline label-edit mode, applied as `<html data-label-edit>`. It is
 * opt-in and only meaningful for the superadmin account, which alone may write
 * overrides; other accounts never see the toggle that turns it on.
 */
export function getLabelEditMode(): LabelEditMode {
  return localStorage.getItem(LABEL_EDIT_KEY) === 'on' ? 'on' : 'off'
}

/** Applies the choice to `<html data-label-edit>`; mirrors the boot script in index.html. */
export function applyLabelEditMode(mode: LabelEditMode): void {
  document.documentElement.dataset.labelEdit = mode
}

const listeners = new Set<() => void>()

export function setLabelEditMode(mode: LabelEditMode): void {
  try {
    // Only «on» is stored: an absent key is the off default, so switching back
    // leaves nothing behind in storage.
    if (mode === 'on') localStorage.setItem(LABEL_EDIT_KEY, mode)
    else localStorage.removeItem(LABEL_EDIT_KEY)
  } catch {
    // Storage can be unavailable; edit mode is a convenience preference.
  }
  applyLabelEditMode(mode)
  listeners.forEach((listener) => listener())
}

function subscribe(onStoreChange: () => void): () => void {
  listeners.add(onStoreChange)
  return () => {
    listeners.delete(onStoreChange)
  }
}

/**
 * Effective label-edit mode. It lives in `shared/lib`, not in the app shell, because
 * the `<Label>` primitive and the grid/form primitives below `widgets` read it too.
 */
export function useLabelEdit(): LabelEditMode {
  return useSyncExternalStore(subscribe, getLabelEditMode)
}