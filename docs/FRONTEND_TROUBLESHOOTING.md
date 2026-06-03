# Frontend Troubleshooting

## Intelligence page must use Leaflet/OpenStreetMap

The current Intelligence page implementation intentionally uses Leaflet/OpenStreetMap tiles for the embedded risk map. It must contain the following fragments:

- `<MapContainer` JSX tag
- `<TileLayer` JSX tag
- `from 'react-leaflet'`
- `leaflet/dist/leaflet.css`
- `tile.openstreetmap.org`

Run this from the repository root before rebuilding Docker:

```bash
cd frontend
npm run check:intelligence-map
cd ..
```

If the Leaflet/OpenStreetMap fragments are missing, repair the page automatically with:

```bash
cd frontend
npm run fix:intelligence-map
cd ..
```

`npm run build` also runs the fix command automatically through `prebuild`, so Docker builds restore the canonical Leaflet page before TypeScript compilation starts.

On Windows Command Prompt, you can also verify the file directly:

```bat
findstr /n "MapContainer" frontend\src\pages\intelligence\IntelligencePage.tsx
findstr /n "react-leaflet" frontend\src\pages\intelligence\IntelligencePage.tsx
findstr /n "tile.openstreetmap.org" frontend\src\pages\intelligence\IntelligencePage.tsx
```

All three `findstr` commands should return lines. If any command returns no lines, your local branch does not have the Leaflet/OpenStreetMap Intelligence page.

## If OpenStreetMap tiles do not appear

Leaflet renders risk circles from local/API data, but the street-map tiles come from OpenStreetMap. If the circles appear over a gray map, check:

1. Browser network access to `https://tile.openstreetmap.org`.
2. Browser DevTools → Network for blocked mixed-content, DNS, TLS, or firewall errors.
3. Corporate VPN/proxy rules that block public tile servers.
4. Whether you need an internal tile server for offline/government networks.

The risk data API can still be working even if the tile images are blocked. In that case, the circles and popups render but the background tiles remain blank/gray.

## If the yellow fallback message appears

The fallback message means the page could not display real heatmap cells from `GET /api/v2/geo/risk-heatmap`. Verify:

1. You are logged in as `Admin`, `SuperAdmin`, or `TaxOfficer`.
2. `FeatureManagement.GeoClusteringDashboard` is enabled.
3. `property_locations` contains latitude/longitude rows inside the requested bounds.
4. The API container logs do not show SQL or authorization failures.

Useful checks:

```bash
docker compose logs --tail=120 api
docker exec retax_postgres psql -U retax_user -d retax_db \
  -c "SELECT COUNT(*) FROM property_locations WHERE latitude BETWEEN 29.5 AND 30.5 AND longitude BETWEEN 30.5 AND 31.5;"
```

## If `npm` cannot parse `package.json`

If `npm run fix:intelligence-map` fails with `EJSONPARSE`, run the standalone repair script with Node instead of npm:

```bash
node frontend/scripts/repair-frontend-build.mjs
```

On Windows Command Prompt from the repository root:

```bat
node frontend\scripts\repair-frontend-build.mjs
```

This script repairs the `scripts` block in `frontend/package.json`, rewrites the checker script if it has duplicate import/SyntaxError damage, restores the Leaflet/OpenStreetMap Intelligence page, and then you can rebuild:

```bash
docker compose build --no-cache frontend
docker compose up -d --force-recreate frontend
```

Docker builds also run the same package repair in `--package-only` mode before `npm install`, so a stale local `package.json` with a missing comma no longer blocks the container build before the full source tree is copied.

## If `repair-frontend-build.mjs` has duplicate `checkerSource` after a merge

If PowerShell reports `Identifier 'checkerSource' has already been declared`, your local merge kept two historical versions of the repair script. Restore the canonical template from the repository root before running Node:

```powershell
Copy-Item frontend\scripts\templates\repair-frontend-build.mjs frontend\scripts\repair-frontend-build.mjs -Force
findstr /n "checkerSource" frontend\scripts\repair-frontend-build.mjs
node --check frontend\scripts\repair-frontend-build.mjs
node frontend\scripts\repair-frontend-build.mjs
```

The `findstr` command should return no results. Docker builds also restore this template before executing the repair script so stale merge fragments cannot break the image build.

## Rebuild frontend after the check passes

```bash
docker compose build --no-cache frontend
docker compose up -d --force-recreate frontend
```

If `docker compose up` starts a container after a failed build, it may be using an older image. Always fix the build failure first, then recreate the frontend container.

## Intelligence map points do not show details on hover

The Leaflet map uses `CircleMarker` overlays and Leaflet popups. Each risk marker supports:

1. Hover/click selection in the side details card.
2. Click to open a Leaflet popup on the marker.
3. Click/tap to open the details card below the map.

If no real heatmap cells are returned by the API, the page shows illustrative fallback points. Those points are for screen validation and training only; operational prioritization must use real server-returned heatmap data.
