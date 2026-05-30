# Frontend Troubleshooting

## Intelligence page build still mentions `MapContainer`

The current Intelligence page implementation is intentionally dependency-free for the embedded analytical map. It must not contain any of the following fragments:

- `<MapContainer` or `</MapContainer>` JSX tags
- `from 'react-leaflet'`
- `leaflet/dist/leaflet.css`
- `createElement` map-renderer code

Run this from the repository root before rebuilding Docker:

```bash
cd frontend
npm run check:intelligence-map
cd ..
```

If stale fragments are reported, repair the page automatically with:

```bash
cd frontend
npm run fix:intelligence-map
cd ..
```

`npm run build` also runs the fix command automatically through `prebuild`, so Docker builds repair stale local copies before TypeScript compilation starts.

On Windows Command Prompt, you can also verify the file directly:

```bat
findstr /n "MapContainer" frontend\src\pages\intelligence\IntelligencePage.tsx
findstr /n "react-leaflet" frontend\src\pages\intelligence\IntelligencePage.tsx
findstr /n "createElement" frontend\src\pages\intelligence\IntelligencePage.tsx
```

All three `findstr` commands should return no results. If they return lines, your local branch still has a stale copy of `frontend/src/pages/intelligence/IntelligencePage.tsx`.


## If `npm` cannot parse `package.json`

If `npm run fix:intelligence-map` fails with `EJSONPARSE`, run the standalone repair script with Node instead of npm:

```bash
node frontend/scripts/repair-frontend-build.mjs
```

On Windows Command Prompt from the repository root:

```bat
node frontend\scripts\repair-frontend-build.mjs
```

This script repairs the `scripts` block in `frontend/package.json`, rewrites the checker script if it has duplicate import/SyntaxError damage, restores the dependency-free Intelligence page, and then you can rebuild:
This script repairs the `scripts` block in `frontend/package.json`, restores the dependency-free Intelligence page, and then you can rebuild:

```bash
docker compose build --no-cache frontend
docker compose up -d --force-recreate frontend
```

Docker builds also run the same package repair in `--package-only` mode before `npm install`, so a stale local `package.json` with a missing comma no longer blocks the container build before the full source tree is copied.

## Rebuild frontend after the check passes

```bash
docker compose build --no-cache frontend
docker compose up -d --force-recreate frontend
```

If `docker compose up` starts a container after a failed build, it may be using an older image. Always fix the build failure first, then recreate the frontend container.

## Intelligence map points do not show details on hover

The current projected-grid map uses normal HTML buttons for risk cells. Each risk marker supports:

1. Browser-native `title` text on cursor hover.
2. A visible dark tooltip on hover/focus.
3. Click/tap to open the details card below the map.

If no real heatmap cells are returned by the API, the page shows illustrative fallback points. Those points are for screen validation and training only; operational prioritization must use real server-returned heatmap data.
