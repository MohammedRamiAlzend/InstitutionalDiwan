import type { ReactNode } from 'react'
import { LabelEditContext } from '@/shared/ui'
import { LabelEditor } from './LabelEditor'

/**
 * Mounts the feature-owned editor into the shared `<Label>` bridge. The app layer wraps the
 * workspace with this so every `<Label>` can open the in-place editor when edit mode is on.
 */
export function UiLabelEditProvider({ children }: { children: ReactNode }) {
  return (
    <LabelEditContext.Provider value={(props) => <LabelEditor {...props} />}>
      {children}
    </LabelEditContext.Provider>
  )
}