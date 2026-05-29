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

On Windows Command Prompt, you can also verify the file directly:

```bat
findstr /n "MapContainer" frontend\src\pages\intelligence\IntelligencePage.tsx
findstr /n "react-leaflet" frontend\src\pages\intelligence\IntelligencePage.tsx
findstr /n "createElement" frontend\src\pages\intelligence\IntelligencePage.tsx
```

All three `findstr` commands should return no results. If they return lines, your local branch still has a stale copy of `frontend/src/pages/intelligence/IntelligencePage.tsx`.

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
