import { readFileSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'

const scriptDir = dirname(fileURLToPath(import.meta.url))
const pagePath = resolve(scriptDir, '../src/pages/intelligence/IntelligencePage.tsx')
const templatePath = resolve(scriptDir, './templates/IntelligencePage.clean.tsx')
const shouldFix = process.argv.includes('--fix')

const requiredPatterns = [
  { name: 'MapContainer JSX tag', pattern: /<MapContainer\b/ },
  { name: 'TileLayer JSX tag', pattern: /<TileLayer\b/ },
  { name: 'react-leaflet import', pattern: /from ['"]react-leaflet['"]/ },
  { name: 'Leaflet stylesheet import', pattern: /leaflet\/dist\/leaflet\.css/ },
  { name: 'OpenStreetMap tile URL', pattern: /tile\.openstreetmap\.org/ },
]

const forbiddenPatterns = [
  { name: 'manual createElement map renderer', pattern: /\bcreateElement\b/ },
  { name: 'projected-grid MapSurface component', pattern: /function\s+MapSurface\b/ },
]

function findMissingRequirements(source) {
  return requiredPatterns.filter(({ pattern }) => !pattern.test(source))
}

function findFailures(source) {
  return forbiddenPatterns.filter(({ pattern }) => pattern.test(source))
}

let source = readFileSync(pagePath, 'utf8')

if (shouldFix) {
  const cleanSource = readFileSync(templatePath, 'utf8')
  if (source !== cleanSource) {
    writeFileSync(pagePath, cleanSource)
    console.log('Restored IntelligencePage.tsx from the canonical Leaflet/OpenStreetMap template.')
  }
  source = cleanSource
}

const missingRequirements = findMissingRequirements(source)
const failures = findFailures(source)

if (missingRequirements.length > 0 || failures.length > 0) {
  if (missingRequirements.length > 0) {
    console.error('IntelligencePage.tsx is missing required Leaflet/OpenStreetMap fragments:')
    for (const missing of missingRequirements) {
      console.error('- ' + missing.name)
    }
  }

  if (failures.length > 0) {
    console.error('IntelligencePage.tsx still contains stale projected-grid implementation fragments:')
    for (const failure of failures) {
      console.error('- ' + failure.name)
    }
  }

  console.error('\nRun npm run fix:intelligence-map or restore frontend/src/pages/intelligence/IntelligencePage.tsx from the canonical Leaflet/OpenStreetMap template before building.')
  process.exit(1)
}

console.log('Intelligence map implementation uses Leaflet/OpenStreetMap and is free of stale projected-grid fragments.')
