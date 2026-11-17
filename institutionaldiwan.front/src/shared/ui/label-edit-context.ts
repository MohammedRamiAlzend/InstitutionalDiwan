import { createContext, useContext, type ReactNode } from 'react'
import type { LabelKey } from '@/shared/config'

/** Everything the in-place editor needs to edit one label and report back. */
export interface LabelEditorProps {
  labelKey: LabelKey
  /** The built-in default template (placeholders intact), used for validation and reset. */
  defaultText: string
  /** The raw text to seed the input with: the current override, or the default. */
  initialText: string
  /** Whether a server override exists, so "restore default" is meaningful. */
  hasOverride: boolean
  /** Called after a successful save/reset, or on cancel, to leave edit state. */
  onClose: () => void
}

export type LabelEditorRenderer = (props: LabelEditorProps) => ReactNode

/**
 * Bridges the shared `<Label>` primitive to the feature-owned editor without an upward
 * import: the app layer provides a renderer (the `ui-label-manage` editor), and any
 * `<Label>` below consumes it. With no provider (or edit mode off) labels stay plain text.
 */
export const LabelEditContext = createContext<LabelEditorRenderer | null>(null)

export function useLabelEditorRenderer(): LabelEditorRenderer | null {
  return useContext(LabelEditContext)
}