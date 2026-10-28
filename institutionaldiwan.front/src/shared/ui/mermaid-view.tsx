import { useEffect, useId, useRef, useState, type KeyboardEvent, type MouseEvent } from 'react'
import { cn, useTheme } from '@/shared/lib'
import { Skeleton } from './skeleton'

type MermaidApi = (typeof import('mermaid'))['default']

/** Caps the accepted source so a pasted diagram can never freeze the tab. */
const MAX_SOURCE_LENGTH = 20_000
const MAX_ERROR_LENGTH = 1_200

let mermaidPromise: Promise<MermaidApi> | null = null
let renderSequence = 0

/** Mermaid names every flowchart node `flowchart-<nodeId>-<counter>` (optionally diagram-id prefixed). */
const FLOWCHART_NODE_ID = /flowchart-(.+?)-\d+$/
const INTERACTIVE_NODE_ATTR = 'data-diwan-node-id'

function readNodeId(node: Element): string | null {
  const match = FLOWCHART_NODE_ID.exec(node.id)
  return match?.[1] ?? null
}

/**
 * Flowchart nodes render as `<g class="node">`; marking them keeps the diagram
 * source free of Mermaid `click` directives, which `securityLevel: 'strict'`
 * strips. Attributes are (re)applied after every render, since the SVG is
 * replaced on each source or theme change.
 */
function markInteractiveNodes(container: HTMLElement): void {
  for (const node of container.querySelectorAll<SVGGElement>('g.node')) {
    const nodeId = readNodeId(node)
    if (!nodeId) continue
    const label = node.textContent?.replace(/\s+/g, ' ').trim()
    node.setAttribute(INTERACTIVE_NODE_ATTR, nodeId)
    node.setAttribute('role', 'button')
    node.setAttribute('tabindex', '0')
    if (label) node.setAttribute('aria-label', label)
  }
}

function readActivatedNodeId(target: EventTarget | null): string | null {
  if (!(target instanceof Element)) return null
  const node = target.closest(`g.node[${INTERACTIVE_NODE_ATTR}]`)
  return node?.getAttribute(INTERACTIVE_NODE_ATTR) ?? null
}

function loadMermaid(): Promise<MermaidApi> {
  mermaidPromise ??= import('mermaid').then((module) => module.default)
  return mermaidPromise
}

/**
 * `render` names its own throwaway measuring node with the render id — and the
 * returned `<svg>` carries the same id, so only non-SVG leftovers may be removed.
 */
function discardStaleNode(renderId: string): void {
  const node = document.getElementById(renderId)
  if (node && node.tagName.toLowerCase() !== 'svg') node.remove()
}

/** Tokens passed to Mermaid must be plain computed colors, never `color-mix()` values. */
const THEME_TOKENS = {
  background: '--color-surface',
  primaryColor: '--color-accent',
  primaryTextColor: '--color-accent-contrast',
  primaryBorderColor: '--color-highlight',
  secondaryColor: '--color-gold',
  tertiaryColor: '--color-bg',
  lineColor: '--color-text-muted',
  textColor: '--color-text',
} as const

/** Mermaid keeps its geometric layout LTR; only the node labels may be Arabic. */
function buildThemeVariables(theme: 'light' | 'dark'): Record<string, string | boolean> {
  const style = getComputedStyle(document.documentElement)
  const variables: Record<string, string | boolean> = {
    darkMode: theme === 'dark',
    fontFamily: "'Readex Pro', 'Cairo', 'Segoe UI', Tahoma, Arial, sans-serif",
    fontSize: '14px',
  }

  for (const [key, token] of Object.entries(THEME_TOKENS)) {
    const value = style.getPropertyValue(token).trim()
    if (value) variables[key] = value
  }

  return variables
}

interface MermaidViewProps {
  /** Mermaid source: `flowchart TD`, `mindmap`, `timeline`, … */
  source: string
  /** Arabic description announced for the rendered diagram. */
  ariaLabel: string
  className?: string
  /**
   * Makes flowchart nodes activatable (click, `Enter`, `Space`) and reports
   * the Mermaid node id back to the owner, so it can resolve its own record.
   */
  onNodeClick?: (nodeId: string) => void
}

interface RenderResultState {
  /** Identifies the source + theme pair the result belongs to. */
  key: string
  status: 'ready' | 'error'
  message?: string
}

/**
 * The only place that imports `mermaid`. Rendering is lazy, sanitized
 * (`securityLevel: 'strict'`, HTML labels off), theme-aware (re-renders on
 * light/dark change), and every failure shows an Arabic-first state instead of
 * a blank area. The owning widget supplies the diagram / chart / tree switcher.
 */
export function MermaidView({
  source,
  ariaLabel,
  className,
  onNodeClick,
}: MermaidViewProps) {
  const theme = useTheme()
  const containerRef = useRef<HTMLDivElement>(null)
  const onNodeClickRef = useRef(onNodeClick)
  const reactId = useId()
  const [result, setResult] = useState<RenderResultState | null>(null)

  useEffect(() => {
    onNodeClickRef.current = onNodeClick
  }, [onNodeClick])

  const diagram = source.trim()
  const isEmpty = diagram.length === 0
  const isTooLong = diagram.length > MAX_SOURCE_LENGTH
  const renderable = !isEmpty && !isTooLong
  const renderKey = `${theme}:${diagram}`
  const isRendering = renderable && result?.key !== renderKey
  const isReady = !isRendering && result?.status === 'ready'
  const failed = !isRendering && result?.status === 'error'

  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    if (!renderable) {
      container.innerHTML = ''
      return
    }

    let cancelled = false
    renderSequence += 1
    const renderId = `mermaid-${reactId.replace(/[^a-zA-Z0-9]/g, '')}-${renderSequence}`

    const run = async () => {
      try {
        const mermaid = await loadMermaid()
        if (cancelled) return

        mermaid.initialize({
          startOnLoad: false,
          securityLevel: 'strict',
          theme: 'base',
          themeVariables: buildThemeVariables(theme),
          flowchart: { htmlLabels: false, useMaxWidth: true },
        })

        const { svg, bindFunctions } = await mermaid.render(renderId, diagram)
        if (cancelled) {
          discardStaleNode(renderId)
          return
        }

        container.innerHTML = svg
        bindFunctions?.(container)
        if (onNodeClickRef.current) markInteractiveNodes(container)
        setResult({ key: renderKey, status: 'ready' })
      } catch (error) {
        if (cancelled) return
        container.innerHTML = ''
        discardStaleNode(renderId)
        const message = error instanceof Error ? error.message : String(error)
        setResult({ key: renderKey, status: 'error', message: message.slice(0, MAX_ERROR_LENGTH) })
      }
    }

    void run()

    return () => {
      cancelled = true
      discardStaleNode(renderId)
    }
  }, [diagram, renderable, renderKey, theme, reactId])

  const activateNode = (target: EventTarget | null) => {
    const nodeId = readActivatedNodeId(target)
    if (nodeId) onNodeClick?.(nodeId)
  }

  const handleClick = (event: MouseEvent<HTMLDivElement>) => activateNode(event.target)

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key !== 'Enter' && event.key !== ' ') return
    if (!readActivatedNodeId(event.target)) return
    event.preventDefault()
    activateNode(event.target)
  }

  return (
    <div className={cn('rounded-xl border border-border bg-surface p-4', className)}>
      {isEmpty ? (
        <p className="py-10 text-center text-sm text-muted">لا يوجد مخطط لعرضه.</p>
      ) : isTooLong ? (
        <MermaidError
          message={`حجم المصدر يتجاوز الحد المسموح (${MAX_SOURCE_LENGTH.toLocaleString('en-US')} حرفًا).`}
        />
      ) : (
        <div className="flex flex-col gap-3">
          {isRendering && (
            <div role="status" className="flex flex-col gap-2">
              <span className="text-xs text-muted">جارٍ عرض المخطط…</span>
              <Skeleton className="h-28 w-full" />
            </div>
          )}
          {failed && <MermaidError message={result?.message} />}
          <div
            ref={containerRef}
            dir="ltr"
            role={onNodeClick ? 'group' : 'img'}
            aria-label={ariaLabel}
            onClick={handleClick}
            onKeyDown={handleKeyDown}
            className={cn(
              'overflow-x-auto [&_svg]:mx-auto [&_svg]:h-auto [&_svg]:max-w-full',
              onNodeClick &&
                '[&_g.node]:cursor-pointer [&_g.node:focus-visible_rect]:[stroke-width:3px]',
              !isReady && 'hidden',
            )}
          />
        </div>
      )}
    </div>
  )
}

function MermaidError({ message }: { message?: string }) {
  return (
    <div
      role="alert"
      className="flex flex-col gap-2 rounded-lg border border-danger/40 bg-danger/10 p-3"
    >
      <p className="text-sm font-medium text-danger">تعذّر عرض المخطط بصيغة Mermaid.</p>
      {message && (
        <pre
          dir="ltr"
          className="max-h-32 overflow-auto whitespace-pre-wrap break-words rounded bg-surface p-2 font-mono text-[11px] text-muted"
        >
          {message}
        </pre>
      )}
      <p className="text-xs text-muted">تحقّق من صياغة المصدر ثم أعد المحاولة.</p>
    </div>
  )
}
