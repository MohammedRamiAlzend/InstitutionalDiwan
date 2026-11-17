import { useState, type KeyboardEvent, type MouseEvent } from 'react'
import { LABEL_DEFAULTS, type LabelKey } from '@/shared/config'
import { cn, useLabel, useLabelEdit, useLabelsStore, type LabelParams } from '@/shared/lib'
import { useLabelEditorRenderer, type LabelEditorProps } from './label-edit-context'

interface LabelProps {
  /** The catalog key whose text to render. */
  k: LabelKey
  params?: LabelParams
  className?: string
}

/**
 * Renders a UI label from the catalog (override → default). When inline edit mode is on and
 * an editor is mounted, the text becomes click-to-edit in place; otherwise it is plain text.
 * Edit mode is forced off for non-superadmin sessions by the boot gate, so editing never leaks.
 */
export function Label({ k, params, className }: LabelProps) {
  const text = useLabel(k, params)
  const editMode = useLabelEdit()
  const overrides = useLabelsStore((state) => state.overrides)
  const renderEditor = useLabelEditorRenderer()
  const [editing, setEditing] = useState(false)

  const editable = editMode === 'on' && renderEditor !== null

  if (editing && renderEditor) {
    const defaultText = LABEL_DEFAULTS[k]
    const editorProps: LabelEditorProps = {
      labelKey: k,
      defaultText,
      initialText: overrides[k] ?? defaultText,
      hasOverride: Object.prototype.hasOwnProperty.call(overrides, k),
      onClose: () => setEditing(false),
    }
    return <>{renderEditor(editorProps)}</>
  }

  const openEditor = (event: MouseEvent<HTMLSpanElement> | KeyboardEvent<HTMLSpanElement>) => {
    event.preventDefault()
    event.stopPropagation()
    setEditing(true)
  }

  return (
    <span
      className={cn(className, editable && 'diwan-label-editable')}
      role={editable ? 'button' : undefined}
      tabIndex={editable ? 0 : undefined}
      title={editable ? 'انقر لتعديل التسمية' : undefined}
      onClick={editable ? openEditor : undefined}
      onKeyDown={
        editable
          ? (event) => {
              if (event.key === 'Enter' || event.key === ' ') openEditor(event)
            }
          : undefined
      }
    >
      {text}
    </span>
  )
}