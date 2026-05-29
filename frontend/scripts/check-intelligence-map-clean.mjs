import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'

const scriptDir = dirname(fileURLToPath(import.meta.url))
const pagePath = resolve(scriptDir, '../src/pages/intelligence/IntelligencePage.tsx')
const source = readFileSync(pagePath, 'utf8')

const forbiddenPatterns = [
  { name: '<MapContainer JSX tag', pattern: /<\/?MapContainer\b/ },
  { name: 'react-leaflet import', pattern: /from ['"]react-leaflet['"]/ },
  { name: 'leaflet stylesheet import', pattern: /leaflet\/dist\/leaflet\.css/ },
  { name: 'createElement map renderer', pattern: /\bcreateElement\b/ },
]

const failures = forbiddenPatterns.filter(({ pattern }) => pattern.test(source))

if (failures.length > 0) {
  console.error('IntelligencePage.tsx still contains stale map implementation fragments:')
  for (const failure of failures) {
    console.error(`- ${failure.name}`)
  }
  console.error('\nReplace frontend/src/pages/intelligence/IntelligencePage.tsx with the dependency-free projected-grid version before building.')
  process.exit(1)
}

console.log('Intelligence map implementation is dependency-free and free of stale MapContainer fragments.')
