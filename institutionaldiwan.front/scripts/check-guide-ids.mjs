#!/usr/bin/env node
// Keeps the tutorial catalog and the JSX honest: every guide id used must exist, and
// every entry written must be used. The front end has no test runner, so this is the gate.
// Only literal ids (`id="users.grid"`) are followed; a computed id is left unchecked.
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join, relative } from 'node:path'

const root = process.cwd()
const sourceDir = join(root, 'src')
const catalogDir = join(sourceDir, 'shared', 'config', 'guides')

const ENTRY_USAGES = [
  { component: 'GuideChip', attr: 'id' },
  { component: 'GuideRegion', attr: 'id' },
  { component: 'FormField', attr: 'guideId' },
]

const PAGE_USAGES = [
  { component: 'GuideProvider', attr: 'pageId' },
  { component: 'PageHeader', attr: 'pageGuide' },
]

function walk(dir) {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name)
    if (statSync(path).isDirectory()) return walk(path)
    return /\.(ts|tsx)$/.test(name) ? [path] : []
  })
}

/** Guide ids the JSX passes to a set of primitives, with the file that passed them. */
function collect(files, specs) {
  const found = []

  for (const file of files) {
    const text = readFileSync(file, 'utf8')
    for (const { component, attr } of specs) {
      const pattern = new RegExp(`<${component}\\b[^]{0,600}?\\b${attr}="([^"]+)"`, 'g')
      for (const match of text.matchAll(pattern)) {
        found.push({ id: match[1], file: relative(root, file) })
      }
    }
  }

  return found
}

function readCatalog() {
  const entryIds = []
  const pageIds = []

  for (const name of readdirSync(catalogDir)) {
    if (!name.endsWith('.ts') || name === 'types.ts' || name === 'index.ts') continue
    const text = readFileSync(join(catalogDir, name), 'utf8')
    for (const match of text.matchAll(/^\s*id: '([^']+)'/gm)) entryIds.push(match[1])
    for (const match of text.matchAll(/pageId: '([^']+)'/g)) pageIds.push(match[1])
  }

  return { entryIds, pageIds }
}

const sourceFiles = walk(sourceDir).filter(
  (file) => !file.startsWith(catalogDir) && !file.endsWith('index.ts'),
)
const { entryIds, pageIds } = readCatalog()
const usedEntries = collect(sourceFiles, ENTRY_USAGES)
const usedPages = collect(sourceFiles, PAGE_USAGES)

const problems = []

const duplicateEntry = entryIds.find((id, index) => entryIds.indexOf(id) !== index)
if (duplicateEntry) problems.push(`duplicate catalog entry id: ${duplicateEntry}`)

for (const { id, file } of usedEntries) {
  if (!entryIds.includes(id)) problems.push(`${file}: unknown guide entry id "${id}"`)
  const namespace = id.split('.')[0]
  if (!pageIds.includes(namespace) && namespace !== 'table') {
    problems.push(`${file}: guide entry id "${id}" is not namespaced to a page or to table.*`)
  }
}

for (const id of entryIds) {
  if (!usedEntries.some((usage) => usage.id === id)) {
    problems.push(`catalog entry "${id}" is never used`)
  }
}

for (const { id, file } of usedPages) {
  if (!pageIds.includes(id)) problems.push(`${file}: unknown page guide id "${id}"`)
}

for (const id of pageIds) {
  if (!usedPages.some((usage) => usage.id === id)) {
    problems.push(`catalog page "${id}" has no <GuideProvider pageId="${id}">`)
  }
}

if (problems.length > 0) {
  console.error('Guide catalogue check failed:\n' + problems.map((p) => `  - ${p}`).join('\n'))
  process.exit(1)
}

console.log(
  `Guide catalogue OK: ${entryIds.length} entries across ${pageIds.length} page guide(s), ` +
    `${usedEntries.length} usage(s).`,
)
