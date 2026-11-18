#!/usr/bin/env node
// Consistency guard for the editable-labels catalog (plans/ui-labels-edit-mode.md, Phase 5).
// The front end has no test runner, so this runs with `npm run lint`. It checks that:
//   1. every catalog key is declared once (no duplicates across area files);
//   2. every key passed to an explicit label call (t/useLabel/resolveLabel, <Label k>,
//      labelKey/headerKey/hintKey) exists in the catalog;
//   3. every `{token}` in a default is a well-formed interpolation placeholder or a known
//      literal pattern token (numbering {YEAR}/{SEQ}/{SEQ:D6}, driver.js {{current}}/{{total}});
//   4. warns about catalog keys whose literal never appears in source (possible orphans).
// Existence of typed keys is also enforced by `tsc -b`; this adds a clear, early message and
// keeps the catalog honest. Keys referenced only through computed values are left to the compiler.
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join, relative } from 'node:path'

const root = process.cwd()
const sourceDir = join(root, 'src')
const catalogDir = join(sourceDir, 'shared', 'config', 'labels')

/** Tokens allowed to stay literal in a default (never supplied as interpolation params). */
const LITERAL_TOKENS = new Set(['YEAR', 'SEQ', 'SEQ:D6', 'current', 'total'])

const ENTRY_LINE = /^\s*'([^']+)':\s*'((?:[^'\\]|\\.)*)'\s*,?\s*$/gm
const PLACEHOLDER = /\{([^{}]+)\}/g

const CALL_PATTERNS = [
  /\b(?:t|useLabel|resolveLabel)\(\s*'([^']+)'/g,
  /\b(?:t|useLabel|resolveLabel)\(\s*"([^"]+)"/g,
  /<Label\b[^]{0,400}?\bk="([^"]+)"/g,
  /<Label\b[^]{0,400}?\bk=\{'([^']+)'\}/g,
  /\b(?:labelKey|headerKey|hintKey)\s*[:=]\s*'([^']+)'/g,
  /\b(?:labelKey|headerKey|hintKey)\s*[:=]\s*"([^"]+)"/g,
]

function walk(dir) {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name)
    if (statSync(path).isDirectory()) return walk(path)
    return /\.(ts|tsx)$/.test(name) ? [path] : []
  })
}

function readCatalog() {
  const entries = []
  for (const name of readdirSync(catalogDir)) {
    if (!name.endsWith('.ts') || name === 'index.ts') continue
    const file = join(catalogDir, name)
    const text = readFileSync(file, 'utf8')
    for (const match of text.matchAll(ENTRY_LINE)) {
      entries.push({ key: match[1], value: match[2], file: relative(root, file) })
    }
  }
  return entries
}

const entries = readCatalog()
const catalogKeys = new Set(entries.map((entry) => entry.key))
const sourceFiles = walk(sourceDir).filter((file) => !file.startsWith(catalogDir))
const sources = sourceFiles.map((file) => ({
  file: relative(root, file),
  text: readFileSync(file, 'utf8'),
}))

const problems = []
const warnings = []

// 1. Duplicates.
const seen = new Map()
for (const { key, file } of entries) {
  if (seen.has(key)) problems.push(`duplicate catalog key "${key}" (${seen.get(key)} and ${file})`)
  else seen.set(key, file)
}

// 2. Unknown keys in explicit label calls.
const unknown = new Map()
for (const { file, text } of sources) {
  for (const pattern of CALL_PATTERNS) {
    for (const match of text.matchAll(pattern)) {
      const key = match[1]
      if (!catalogKeys.has(key) && !unknown.has(key)) unknown.set(key, file)
    }
  }
}
for (const [key, file] of unknown) problems.push(`${file}: unknown label key "${key}"`)

// 3. Malformed / unexpected placeholder tokens in defaults.
for (const { key, value, file } of entries) {
  for (const match of value.matchAll(PLACEHOLDER)) {
    const token = match[1]
    if (LITERAL_TOKENS.has(token) || /^\w+$/.test(token)) continue
    problems.push(`${file}: label "${key}" has an unexpected placeholder "{${token}}"`)
  }
}

// 4. Orphans (warning only): a key whose exact literal never appears in source.
for (const { key } of entries) {
  const used = sources.some(
    ({ text }) => text.includes(`'${key}'`) || text.includes(`"${key}"`),
  )
  if (!used) warnings.push(`catalog key "${key}" is never referenced in source`)
}

if (warnings.length > 0) {
  console.warn('Label catalogue warnings:\n' + warnings.map((w) => `  - ${w}`).join('\n'))
}

if (problems.length > 0) {
  console.error('Label catalogue check failed:\n' + problems.map((p) => `  - ${p}`).join('\n'))
  process.exit(1)
}

console.log(
  `Label catalogue OK: ${entries.length} key(s) across ${seen.size} unique key(s), ` +
    `${sources.length} source file(s) scanned, ${warnings.length} orphan warning(s).`,
)