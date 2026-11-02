/**
 * One explained part of the interface: a block on a page, a control, or a form field.
 * `body` answers «what do I do here and what changes?», never «what is this button».
 */
export interface GuideEntry {
  /** Stable id, `<page>.<region>` or `table.<tool>` for the shared grid tools. */
  readonly id: string
  readonly title: string
  readonly body: string
  /** A prerequisite, a side effect, or the permission a control needs. */
  readonly note?: string
}

/** A page's walkthrough order. Only entries currently on screen are offered. */
export interface PageGuide {
  readonly pageId: string
  readonly intro: string
  readonly entryIds: readonly string[]
}
