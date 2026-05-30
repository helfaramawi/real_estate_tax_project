import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'
import { spawnSync } from 'node:child_process'

const scriptDir = dirname(fileURLToPath(import.meta.url))
const frontendDir = resolve(scriptDir, '..')
const packagePath = resolve(frontendDir, 'package.json')
const checkerPath = resolve(scriptDir, 'check-intelligence-map-clean.mjs')
const intelligencePagePath = resolve(frontendDir, 'src/pages/intelligence/IntelligencePage.tsx')
const packageOnly = process.argv.includes('--package-only')

const desiredScripts = {
  dev: 'vite',
  build: 'tsc -b && vite build',
  lint: 'eslint .',
  preview: 'vite preview',
  'check:intelligence-map': 'node scripts/check-intelligence-map-clean.mjs',
  'fix:intelligence-map': 'node scripts/check-intelligence-map-clean.mjs --fix',
  prebuild: 'npm run fix:intelligence-map',
}

const checkerSource = `import { readFileSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'

const scriptDir = dirname(fileURLToPath(import.meta.url))
const pagePath = resolve(scriptDir, '../src/pages/intelligence/IntelligencePage.tsx')
const templatePath = resolve(scriptDir, './templates/IntelligencePage.clean.tsx')
const shouldFix = process.argv.includes('--fix')

const forbiddenPatterns = [
  { name: '<MapContainer JSX tag', pattern: /<\\/?MapContainer\\b/ },
  { name: 'react-leaflet import', pattern: /from ['"]react-leaflet['"]/ },
  { name: 'leaflet stylesheet import', pattern: /leaflet\\/dist\\/leaflet\\.css/ },
  { name: 'createElement map renderer', pattern: /\\bcreateElement\\b/ },
]

function findFailures(source) {
  return forbiddenPatterns.filter(({ pattern }) => pattern.test(source))
}

let source = readFileSync(pagePath, 'utf8')
let failures = findFailures(source)

if (failures.length > 0 && shouldFix) {
  const cleanSource = readFileSync(templatePath, 'utf8')
  writeFileSync(pagePath, cleanSource)
  source = cleanSource
  failures = findFailures(source)
  console.log('Replaced stale IntelligencePage.tsx with the dependency-free projected-grid implementation.')
}

if (failures.length > 0) {
  console.error('IntelligencePage.tsx still contains stale map implementation fragments:')
  for (const failure of failures) {
    console.error('- ' + failure.name)
  }
  console.error('\\nRun npm run fix:intelligence-map or replace frontend/src/pages/intelligence/IntelligencePage.tsx with the dependency-free projected-grid version before building.')
  process.exit(1)
}

console.log('Intelligence map implementation is dependency-free and free of stale MapContainer fragments.')
`

function readPackageText() {
  return readFileSync(packagePath, 'utf8')
}

function writePackageJson(packageJson) {
  writeFileSync(packagePath, `${JSON.stringify(packageJson, null, 2)}\n`)
}

function writeCheckerScript() {
  writeFileSync(checkerPath, checkerSource)
}

function parseOrRepairPackageJson() {
  const raw = readPackageText()
  try {
    return JSON.parse(raw)
  } catch {
    const scriptsStart = raw.indexOf('"scripts"')
    const dependenciesStart = raw.indexOf('"dependencies"')
    if (scriptsStart === -1 || dependenciesStart === -1) {
      throw new Error('Could not locate scripts/dependencies blocks in frontend/package.json')
    }

    const beforeScripts = raw.slice(0, scriptsStart)
    const afterScripts = raw.slice(dependenciesStart)
    const repairedScripts = `"scripts": ${JSON.stringify(desiredScripts, null, 2)},\n  `
    const repaired = `${beforeScripts}${repairedScripts}${afterScripts}`
    return JSON.parse(repaired)
  }
}

const packageJson = parseOrRepairPackageJson()
packageJson.scripts = { ...packageJson.scripts, ...desiredScripts }
writePackageJson(packageJson)
writeCheckerScript()

if (packageOnly) {
  console.log('Frontend package.json and checker script repaired. Continue with npm install or Docker build.')
  process.exit(0)
}

if (!existsSync(intelligencePagePath)) {
  console.log('Frontend package.json and checker script repaired. Intelligence page was not present, so map repair was skipped.')
  process.exit(0)
}

const fixResult = spawnSync(process.execPath, [checkerPath, '--fix'], {
  cwd: frontendDir,
  stdio: 'inherit',
})

if (fixResult.status !== 0) {
  process.exit(fixResult.status ?? 1)
}

console.log('Frontend package.json, checker script, and Intelligence map page are repaired. You can now run docker compose build --no-cache frontend.')
