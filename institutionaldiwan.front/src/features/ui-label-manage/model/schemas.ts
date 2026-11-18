import { z } from 'zod'
import { t } from '@/shared/lib'

/** Mirrors the server's `Value` column limit. */
export const UI_LABEL_MAX_LENGTH = 200

/** The `{name}` placeholders embedded in a label template. */
function placeholdersOf(text: string): string[] {
  return Array.from(text.matchAll(/\{(\w+)\}/g), (match) => match[1])
}

/**
 * Builds the pre-save schema for one override against its built-in default. Enforces the
 * client-side guards the server does not: the same placeholders must survive, and no HTML
 * tags are allowed. Length and emptiness mirror the server so the message is Arabic either way.
 */
export function uiLabelValueSchema(defaultText: string) {
  const required = placeholdersOf(defaultText)
  return z
    .string()
    .trim()
    .min(1, t('label-editor.validation.required'))
    .max(UI_LABEL_MAX_LENGTH, t('label-editor.validation.max', { max: UI_LABEL_MAX_LENGTH }))
    .refine((value) => !/[<>]/.test(value), t('label-editor.validation.html'))
    .refine(
      (value) => required.every((name) => value.includes(`{${name}}`)),
      t('label-editor.validation.placeholders', { placeholders: required.map((name) => `{${name}}`).join('، ') }),
    )
}