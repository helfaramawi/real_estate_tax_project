import { useMemo, useState, type ReactNode } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { createElement, useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as ReactLeaflet from 'react-leaflet'
import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { MapContainer as LeafletMapContainer, TileLayer, CircleMarker, Tooltip } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { AlertTriangle, Map, TrendingUp } from 'lucide-react'
import api from '../../lib/api'
import PageHeader from '../../components/PageHeader'
import StatusBadge from '../../components/StatusBadge'

type HeatmapCell = { centerLat: number; centerLon: number; avgRiskScore: number; propertyCount: number }
type Anomaly = { id: string; anomalyType: string; severity: string; status: string; lat?: number; lon?: number; description?: string; detectedAt: string }
type Cluster = { id: string; clusterLabel: number; centroidLat?: number; centroidLon?: number; propertyCount: number; medianValueSqm?: number; governorate?: string; computedAt: string }
type MapPoint = { x: number; y: number }

const SEVERITY_COLOR: Record<string, string> = {
  Critical: '#dc2626',
  High: '#ea580c',
  Medium: '#d97706',
  Low: '#65a30d',
}

const anomalyTypeAr: Record<string, string> = {
  UnregisteredBuilding: 'مبنى غير مسجل',
  ValuationOutlier: 'قيمة شاذة',
  SuspiciousOwnership: 'ملكية مشبوهة',
  BoundaryOverlap: 'تداخل في الحدود',
  DuplicateCoordinates: 'إحداثيات مكررة',
  GhostProperty: 'عقار وهمي',
  SurveyInconsistency: 'تعارض في المعاينة',
}

const severityAr: Record<string, string> = { Critical: 'حرج', High: 'مرتفع', Medium: 'متوسط', Low: 'منخفض' }
const bounds = { minLat: 29.5, minLon: 30.5, maxLat: 30.5, maxLon: 31.5 }
const boundsQuery = `${bounds.minLat},${bounds.minLon},${bounds.maxLat},${bounds.maxLon}`
const riskColor = (score: number) => score >= 0.75 ? '#dc2626' : score >= 0.5 ? '#ea580c' : score >= 0.25 ? '#d97706' : '#65a30d'
const clampPercent = (value: number) => Math.min(94, Math.max(6, value))
const projectPoint = (lat: number, lon: number): MapPoint => ({
  x: clampPercent(((lon - bounds.minLon) / (bounds.maxLon - bounds.minLon)) * 100),
  y: clampPercent(((bounds.maxLat - lat) / (bounds.maxLat - bounds.minLat)) * 100),
})
const riskColor = (score: number) => score >= 0.75 ? '#dc2626' : score >= 0.5 ? '#ea580c' : score >= 0.25 ? '#d97706' : '#65a30d'
const defaultMapCenter: [number, number] = [30.0444, 31.2357]
const mapContainerStyle = { height: '100%', width: '100%' }
const osmAttribution = '© OpenStreetMap'
const MapView = ReactLeaflet.MapContainer
const MapTiles = ReactLeaflet.TileLayer
const MapCircle = ReactLeaflet.CircleMarker
const MapTooltip = ReactLeaflet.Tooltip
const defaultMapCenter: [number, number] = [30.0444, 31.2357]
const mapContainerStyle = { height: '100%', width: '100%' }
const osmAttribution = '© <a href="https://openstreetmap.org">OpenStreetMap</a>'

const fallbackHeatmap: HeatmapCell[] = [
  { centerLat: 30.0475, centerLon: 31.2124, avgRiskScore: 0.82, propertyCount: 18 },
  { centerLat: 30.0586, centerLon: 31.2357, avgRiskScore: 0.64, propertyCount: 31 },
  { centerLat: 30.0328, centerLon: 31.2442, avgRiskScore: 0.41, propertyCount: 24 },
  { centerLat: 30.0219, centerLon: 31.2015, avgRiskScore: 0.18, propertyCount: 12 },
]

function HeatmapMarker({ cell, onSelect }: { cell: HeatmapCell; onSelect: (cell: HeatmapCell) => void }) {
  const point = projectPoint(cell.centerLat, cell.centerLon)

  return (
    <button
      type="button"
      onClick={() => onSelect(cell)}
      className="group absolute -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-white shadow-lg outline-none focus:ring-4 focus:ring-blue-200"
      style={{ left: `${point.x}%`, top: `${point.y}%`, width: 30, height: 30, backgroundColor: riskColor(cell.avgRiskScore) }}
      title={`متوسط الخطر ${(cell.avgRiskScore * 100).toFixed(0)}% - العقارات ${cell.propertyCount}`}
    >
      <span className="sr-only">تفاصيل منطقة مخاطر</span>
      <span className="pointer-events-none absolute bottom-9 left-1/2 z-[1000] w-52 -translate-x-1/2 rounded-lg bg-slate-900 px-3 py-2 text-xs text-white opacity-0 shadow-lg transition-opacity group-hover:opacity-100 group-focus:opacity-100">
        <span className="block font-semibold">متوسط الخطر: {(cell.avgRiskScore * 100).toFixed(0)}%</span>
        <span className="block">العقارات: {cell.propertyCount.toLocaleString()}</span>
        <span className="block text-slate-300">انقر لعرض التفاصيل</span>
      </span>
    </button>
  )
}

function MapSurface({ children }: { children: ReactNode }) {
  return (
    <div className="relative h-full w-full overflow-hidden bg-slate-100">
      <div className="absolute inset-0 bg-[linear-gradient(90deg,rgba(148,163,184,.22)_1px,transparent_1px),linear-gradient(0deg,rgba(148,163,184,.22)_1px,transparent_1px)] bg-[size:64px_64px]" />
      <div className="absolute inset-0 bg-gradient-to-br from-blue-50 via-white to-emerald-50" />
      <div className="absolute left-6 top-6 rounded-lg border border-white/70 bg-white/85 px-3 py-2 text-xs text-slate-600 shadow-sm">
        نطاق القاهرة التجريبي: {boundsQuery}
      </div>
      <div className="absolute bottom-4 left-4 rounded bg-white/90 px-2 py-1 text-[11px] text-slate-500 shadow-sm">
        خريطة تحليلية داخلية - اضغط/مرّر على النقاط لعرض البيانات
      </div>
      {children}
    </div>
  )
}
function HeatmapCellMarker({ cell, index, onSelect }: { cell: HeatmapCell; index: number; onSelect: (cell: HeatmapCell) => void }) {
  const markerCenter: [number, number] = [cell.centerLat, cell.centerLon]

  return createElement(
    MapCircle,
function HeatmapCellMarker({
  cell,
  index,
  onSelect,
}: {
  cell: HeatmapCell
  index: number
  onSelect: (cell: HeatmapCell) => void
}) {
  const markerCenter: [number, number] = [cell.centerLat, cell.centerLon]

  return createElement(
    CircleMarker,
    {
      key: index,
      center: markerCenter,
      radius: 14,
      fillOpacity: 0.55,
      weight: 1,
      color: '#fff',
      fillColor: riskColor(cell.avgRiskScore),
      eventHandlers: { click: () => onSelect(cell) },
    },
    createElement(
      MapTooltip,
      fillColor: RISK_COLOR(cell.avgRiskScore),
      eventHandlers: { click: () => onSelect(cell) },
    },
    createElement(
      Tooltip,
      { sticky: true, direction: 'top', opacity: 1 },
      createElement(
        'div',
        { className: 'text-xs' },
        createElement('div', null, 'متوسط الخطر: ', createElement('strong', null, `${(cell.avgRiskScore * 100).toFixed(0)}%`)),
        createElement('div', null, `العقارات: ${cell.propertyCount}`),
      ),
    ),
  )
}
  return (
    <CircleMarker
      key={index}
      center={markerCenter}
      radius={14}
      fillOpacity={0.55}
      weight={1}
      color="#fff"
      fillColor={RISK_COLOR(cell.avgRiskScore)}
      eventHandlers={{ click: () => onSelect(cell) }}
    >
      <Tooltip sticky direction="top" opacity={1}>
        <div className="text-xs">
          <div>متوسط الخطر: <strong>{(cell.avgRiskScore * 100).toFixed(0)}%</strong></div>
          <div>العقارات: {cell.propertyCount}</div>
        </div>
      </Tooltip>
    </CircleMarker>
  )
}
const fallbackHeatmap: HeatmapCell[] = [
  { centerLat: 30.0475, centerLon: 31.2124, avgRiskScore: 0.82, propertyCount: 18 },
  { centerLat: 30.0586, centerLon: 31.2357, avgRiskScore: 0.64, propertyCount: 31 },
  { centerLat: 30.0328, centerLon: 31.2442, avgRiskScore: 0.41, propertyCount: 24 },
  { centerLat: 30.0219, centerLon: 31.2015, avgRiskScore: 0.18, propertyCount: 12 },
]

export default function IntelligencePage() {
  const qc = useQueryClient()
  const [tab, setTab] = useState<'heatmap' | 'anomalies' | 'clusters'>('heatmap')
  const [selectedAnomaly, setSelectedAnomaly] = useState<Anomaly | null>(null)
  const [selectedHeatmapCell, setSelectedHeatmapCell] = useState<HeatmapCell | null>(null)

  const { data: heatmap, isLoading: heatmapLoading, isError: heatmapError } = useQuery<HeatmapCell[]>({
    queryKey: ['heatmap', boundsQuery],
    queryFn: () => api.get(`/v2/geo/risk-heatmap?minLat=${bounds.minLat}&minLon=${bounds.minLon}&maxLat=${bounds.maxLat}&maxLon=${bounds.maxLon}`).then(r => r.data.data?.cells ?? []),
  const bounds = '29.5,30.5,30.5,31.5'
  const [minLat, minLon, maxLat, maxLon] = bounds.split(',').map(Number)

  const { data: heatmap, isLoading: heatmapLoading, isError: heatmapError } = useQuery<HeatmapCell[]>({
    queryKey: ['heatmap', bounds],
    queryFn: () => api.get(`/v2/geo/risk-heatmap?minLat=${minLat}&minLon=${minLon}&maxLat=${maxLat}&maxLon=${maxLon}`).then(r => r.data.data?.cells ?? []),
    enabled: tab === 'heatmap',
  })

  const { data: anomalies } = useQuery<Anomaly[]>({
    queryKey: ['anomalies', 'Open'],
    queryFn: () => api.get('/v2/geo/anomalies?status=Open').then(r => r.data.data ?? []),
    enabled: tab === 'anomalies',
  })

  const { data: clusters } = useQuery<Cluster[]>({
    queryKey: ['geo-clusters'],
    queryFn: () => api.get('/v2/geo/clusters').then(r => r.data.data ?? []),
    enabled: tab === 'clusters',
  })

  const { data: summary } = useQuery({
    queryKey: ['intel-summary'],
    queryFn: () => api.get('/v2/intelligence/dashboard/summary').then(r => r.data.data),
  })

  const displayedHeatmap = useMemo(() => {
    if (tab !== 'heatmap' || heatmapLoading) return []
    return heatmap && heatmap.length > 0 ? heatmap : fallbackHeatmap
  }, [heatmap, heatmapLoading, tab])

  const isFallbackHeatmap = tab === 'heatmap' && !heatmapLoading && (!heatmap || heatmap.length === 0)
  const fallbackHeatmapMessage = heatmapError
    ? 'تعذر تحميل بيانات خريطة المخاطر من الخادم. تظهر الآن نقاط توضيحية قابلة للنقر؛ تأكد من إعادة تشغيل API ومن توافر بيانات المواقع والمخاطر.'
    : 'لا توجد خلايا مخاطر مرجعة من الخادم لهذه المنطقة. تظهر الآن نقاط توضيحية قابلة للنقر؛ أضف مواقع عقارات أو فعّل بيانات المخاطر لإظهار البيانات الفعلية.'

  const heatmapMarkers = tab === 'heatmap'
    ? displayedHeatmap.map((cell, index) => createElement(HeatmapCellMarker, { key: index, cell, index, onSelect: setSelectedHeatmapCell }))
    : null

  const anomalyMarkers = tab === 'anomalies'
    ? anomalies?.filter(item => item.lat && item.lon).map(item => createElement(
      MapCircle,
      {
        key: item.id,
        center: [item.lat!, item.lon!] as [number, number],
    ? displayedHeatmap.map((cell, i) => createElement(HeatmapCellMarker, {
      key: i,
      cell,
      index: i,
      onSelect: setSelectedHeatmapCell,
    }))
    : null

  const anomalyMarkers = tab === 'anomalies'
    ? anomalies?.filter(a => a.lat && a.lon).map(a => createElement(
      CircleMarker,
      {
        key: a.id,
        center: [a.lat!, a.lon!] as [number, number],
        radius: 9,
        fillOpacity: 0.9,
        weight: 2,
        color: '#fff',
        fillColor: SEVERITY_COLOR[item.severity] ?? '#94a3b8',
        eventHandlers: { click: () => setSelectedAnomaly(item) },
      },
      createElement(
        MapTooltip,
        fillColor: SEVERITY_COLOR[a.severity] ?? '#94a3b8',
        eventHandlers: { click: () => setSelectedAnomaly(a) },
      },
      createElement(
        Tooltip,
        null,
        createElement(
          'div',
          { className: 'text-xs' },
          createElement('div', { className: 'font-semibold' }, anomalyTypeAr[item.anomalyType] ?? item.anomalyType),
          createElement('div', null, severityAr[item.severity] ?? item.severity),
          createElement('div', { className: 'font-semibold' }, anomalyTypeAr[a.anomalyType] ?? a.anomalyType),
          createElement('div', null, severityAr[a.severity] ?? a.severity),
        ),
      ),
    ))
    : null

  const clusterMarkers = tab === 'clusters'
    ? clusters?.filter(item => item.centroidLat && item.centroidLon).map(item => createElement(
      MapCircle,
      {
        key: item.id,
        center: [item.centroidLat!, item.centroidLon!] as [number, number],
        radius: Math.min(6 + Math.log(item.propertyCount + 1) * 3, 24),
    ? clusters?.filter(c => c.centroidLat && c.centroidLon).map(c => createElement(
      CircleMarker,
      {
        key: c.id,
        center: [c.centroidLat!, c.centroidLon!] as [number, number],
        radius: Math.min(6 + Math.log(c.propertyCount + 1) * 3, 24),
        fillOpacity: 0.7,
        weight: 1,
        color: '#3b82f6',
        fillColor: '#93c5fd',
      },
      createElement(
        MapTooltip,
        Tooltip,
        null,
        createElement(
          'div',
          { className: 'text-xs' },
          createElement('div', { className: 'font-semibold' }, item.governorate ?? `تجمع رقم ${item.clusterLabel}`),
          createElement('div', null, `العقارات: ${item.propertyCount}`),
          item.medianValueSqm
            ? createElement('div', null, `متوسط القيمة: ${Number(item.medianValueSqm).toLocaleString()} ج.م/م²`)
          createElement('div', { className: 'font-semibold' }, c.governorate ?? `تجمع رقم ${c.clusterLabel}`),
          createElement('div', null, `العقارات: ${c.propertyCount}`),
          c.medianValueSqm
            ? createElement('div', null, `متوسط القيمة: ${Number(c.medianValueSqm).toLocaleString()} ج.م/م²`)
            : null,
        ),
      ),
    ))
    : null

  const mapElement = createElement(
    MapView,
    { center: defaultMapCenter, zoom: 11, style: mapContainerStyle },
    createElement(MapTiles, {
    LeafletMapContainer,
    MapContainer,
    { center: defaultMapCenter, zoom: 11, style: mapContainerStyle },
    createElement(TileLayer, {
      url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
      attribution: osmAttribution,
    }),
    heatmapMarkers,
    anomalyMarkers,
    clusterMarkers,
  )

  const updateAnomaly = useMutation({
    mutationFn: ({ id, status, notes }: { id: string; status: string; notes?: string }) =>
      api.patch(`/v2/geo/anomalies/${id}/status`, { status, notes }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['anomalies'] })
      setSelectedAnomaly(null)
    },
  })

  const tabs = [
    { key: 'heatmap', label: 'خريطة المخاطر', icon: Map },
    { key: 'anomalies', label: 'الشذوذات', icon: AlertTriangle },
    { key: 'clusters', label: 'التجمعات', icon: TrendingUp },
  ] as const

  return (
    <div>
      <PageHeader title="الذكاء الاصطناعي" subtitle="التحليل الجغرافي ونماذج التعلم الآلي" />

      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
          {[
            { label: 'تنبؤات عالية الخطر', value: summary.highRiskCount, color: 'text-red-600' },
            { label: 'مشبوه بالاحتيال', value: summary.fraudSuspectCount, color: 'text-orange-600' },
            { label: 'شذوذات مفتوحة', value: summary.openAnomalies, color: 'text-yellow-600' },
            { label: 'بانتظار المراجعة', value: summary.pendingReview, color: 'text-blue-600' },
          ].map(kpi => (
            <div key={kpi.label} className="bg-white rounded-xl border border-slate-200 p-4">
              <div className="text-xs text-slate-500 mb-1">{kpi.label}</div>
              <div className={`text-2xl font-bold ${kpi.color}`}>{kpi.value?.toLocaleString()}</div>
            </div>
          ))}
        </div>
      )}

      <div className="flex gap-2 mb-4">
        {tabs.map(tabItem => {
          const Icon = tabItem.icon
          return (
            <button key={tabItem.key} onClick={() => setTab(tabItem.key)}
              className={`flex items-center gap-2 px-4 py-2 text-sm rounded-lg transition-colors ${
                tab === tabItem.key ? 'bg-blue-600 text-white' : 'bg-white border border-slate-200 text-slate-600 hover:bg-slate-50'
              }`}>
              <Icon size={15} /> {tabItem.label}
            </button>
          )
        })}
      </div>

      <div className="relative rounded-xl overflow-hidden border border-slate-200 shadow-sm" style={{ height: 520 }}>
      {/* Map */}
      <div className="relative rounded-xl overflow-hidden border border-slate-200 shadow-sm" style={{ height: 520 }}>

        {tab === 'heatmap' && heatmapLoading && (
          <div className="absolute inset-x-4 top-4 z-[1000] rounded-lg border border-blue-100 bg-white/95 p-3 text-sm text-slate-700 shadow-sm">
            جارٍ تحميل بيانات خريطة المخاطر...
          </div>
        )}

        {tab === 'heatmap' && isFallbackHeatmap && (
          <div className="absolute inset-x-4 top-4 z-[1000] rounded-lg border border-amber-200 bg-amber-50/95 p-3 text-sm text-amber-900 shadow-sm">
            {fallbackHeatmapMessage}
          </div>
        )}

        <MapSurface>
          {tab === 'heatmap' && displayedHeatmap.map((cell, index) => (
            <HeatmapMarker key={index} cell={cell} onSelect={setSelectedHeatmapCell} />
          ))}

          {tab === 'anomalies' && anomalies?.filter(item => item.lat && item.lon).map(item => {
            const point = projectPoint(item.lat!, item.lon!)
            return (
              <button key={item.id} type="button" onClick={() => setSelectedAnomaly(item)}
                className="absolute -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-white shadow-lg"
                style={{ left: `${point.x}%`, top: `${point.y}%`, width: 22, height: 22, backgroundColor: SEVERITY_COLOR[item.severity] ?? '#94a3b8' }}
                title={`${anomalyTypeAr[item.anomalyType] ?? item.anomalyType} - ${severityAr[item.severity] ?? item.severity}`} />
            )
          })}

          {tab === 'clusters' && clusters?.filter(item => item.centroidLat && item.centroidLon).map(item => {
            const point = projectPoint(item.centroidLat!, item.centroidLon!)
            return (
              <div key={item.id}
                className="absolute -translate-x-1/2 -translate-y-1/2 rounded-full border border-blue-500 bg-blue-200/80 text-[10px] font-bold text-blue-900 shadow"
                style={{ left: `${point.x}%`, top: `${point.y}%`, width: Math.min(26 + Math.log(item.propertyCount + 1) * 5, 54), height: Math.min(26 + Math.log(item.propertyCount + 1) * 5, 54), display: 'grid', placeItems: 'center' }}
                title={`العقارات: ${item.propertyCount}`}>
                {item.propertyCount}
              </div>
            )
          })}
        </MapSurface>
      </div>


        {tab === 'heatmap' && isFallbackHeatmap && (
          <div className="absolute inset-x-4 top-4 z-[1000] rounded-lg border border-amber-200 bg-amber-50/95 p-3 text-sm text-amber-900 shadow-sm">
            {fallbackHeatmapMessage}
          </div>
        )}

        {mapElement}
      </div>


        {tab === 'heatmap' && isFallbackHeatmap && (
          <div className="absolute inset-x-4 top-4 z-[1000] rounded-lg border border-amber-200 bg-amber-50/95 p-3 text-sm text-amber-900 shadow-sm">
            {fallbackHeatmapMessage}
          </div>
        )}

        <MapContainer
          center={defaultMapCenter}
          zoom={11}
          style={mapContainerStyle}
        >
            {heatmapError
              ? 'تعذر تحميل بيانات خريطة المخاطر من الخادم. تظهر الآن نقاط توضيحية قابلة للنقر؛ تأكد من إعادة تشغيل API ومن توافر بيانات المواقع والمخاطر.'
              ? 'تعذر تحميل بيانات خريطة المخاطر من الخادم. تظهر الآن نقاط توضيحية قابلة للنقر حتى يتم تفعيل GeoClusteringDashboard وتوفير بيانات المواقع.'
              : 'لا توجد خلايا مخاطر مرجعة من الخادم لهذه المنطقة. تظهر الآن نقاط توضيحية قابلة للنقر؛ أضف مواقع عقارات أو فعّل بيانات المخاطر لإظهار البيانات الفعلية.'}
          </div>
        )}

        <MapContainer center={[30.0444, 31.2357]} zoom={11} style={{ height: '100%', width: '100%' }}>
          <TileLayer
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            attribution={osmAttribution}
          />

          {tab === 'heatmap' && displayedHeatmap.map((cell, i) => (
            <HeatmapCellMarker
              key={i}
              cell={cell}
              index={i}
              onSelect={setSelectedHeatmapCell}
            />
            <CircleMarker key={i} center={[cell.centerLat, cell.centerLon]}
              radius={14} fillOpacity={0.55} weight={1} color="#fff"
              fillColor={RISK_COLOR(cell.avgRiskScore)}
              eventHandlers={{ click: () => setSelectedHeatmapCell(cell) }}>
              <Tooltip sticky direction="top" opacity={1}>
                <div className="text-xs">
                  <div>متوسط الخطر: <strong>{(cell.avgRiskScore * 100).toFixed(0)}%</strong></div>
                  <div>العقارات: {cell.propertyCount}</div>
                </div>
              </Tooltip>
            </CircleMarker>
          ))}

        {tab === 'heatmap' && heatmapLoading && (
          <div className="absolute inset-x-4 top-4 z-[1000] rounded-lg border border-blue-100 bg-white/95 p-3 text-sm text-slate-700 shadow-sm">
            جارٍ تحميل بيانات خريطة المخاطر...
          </div>
        )}

        {tab === 'heatmap' && isFallbackHeatmap && (
          <div className="absolute inset-x-4 top-4 z-[1000] rounded-lg border border-amber-200 bg-amber-50/95 p-3 text-sm text-amber-900 shadow-sm">
            {fallbackHeatmapMessage}
          </div>
        )}

        {mapElement}
      </div>


      {/* Heatmap detail panel */}
      {tab === 'heatmap' && selectedHeatmapCell && (
        <div className="mt-4 bg-white rounded-xl border border-slate-200 p-5">
          <div className="flex items-start justify-between mb-3">
            <div>
              <h3 className="font-semibold text-slate-800">تفاصيل منطقة المخاطر</h3>
              <p className="text-xs text-slate-500 mt-1">
                تظهر هذه البطاقة عند النقر على أي دائرة في خريطة المخاطر، وتعمل أيضاً كبديل موثوق لأجهزة اللمس عند عدم ظهور التلميح بالتحويم.
              </p>
            </div>
            <button onClick={() => setSelectedHeatmapCell(null)} className="text-slate-400 hover:text-slate-600 text-lg">✕</button>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 text-sm">
            <div className="rounded-lg bg-slate-50 p-3">
              <div className="text-slate-500 text-xs mb-1">متوسط درجة الخطر</div>
              <div className="font-bold text-slate-800">{(selectedHeatmapCell.avgRiskScore * 100).toFixed(0)}%</div>
            </div>
            <div className="rounded-lg bg-slate-50 p-3">
              <div className="text-slate-500 text-xs mb-1">عدد العقارات في المنطقة</div>
              <div className="font-bold text-slate-800">{selectedHeatmapCell.propertyCount.toLocaleString()}</div>
            </div>
            <div className="rounded-lg bg-slate-50 p-3">
              <div className="text-slate-500 text-xs mb-1">مركز الخلية الجغرافية</div>
              <div className="font-bold text-slate-800">
                {selectedHeatmapCell.centerLat.toFixed(4)}, {selectedHeatmapCell.centerLon.toFixed(4)}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Severity legend for anomalies */}
      {tab === 'anomalies' && (
        <div className="mt-4 flex gap-4 flex-wrap">
          {Object.entries(SEVERITY_COLOR).map(([sev, color]) => (
            <div key={sev} className="flex items-center gap-2">
              <div className="w-3 h-3 rounded-full" style={{ backgroundColor: color }} />
              <span className="text-sm text-slate-600">{severityAr[sev]}</span>
              <span className="text-xs text-slate-400">
                ({anomalies?.filter(item => item.severity === sev).length ?? 0})
              </span>
            </div>
          ))}
        </div>
      )}

      {selectedAnomaly && (
        <div className="mt-4 bg-white rounded-xl border border-slate-200 p-5">
          <div className="flex items-start justify-between mb-3">
            <div>
              <h3 className="font-semibold text-slate-800">
                {anomalyTypeAr[selectedAnomaly.anomalyType] ?? selectedAnomaly.anomalyType}
              </h3>
              <div className="flex gap-2 mt-1">
                <StatusBadge label={selectedAnomaly.severity} />
                <StatusBadge label={selectedAnomaly.status} />
              </div>
            </div>
            <button onClick={() => setSelectedAnomaly(null)} className="text-slate-400 hover:text-slate-600 text-lg">✕</button>
          </div>
          {selectedAnomaly.description && (
            <p className="text-sm text-slate-600 mb-4">{selectedAnomaly.description}</p>
          )}
          <div className="flex gap-2">
            {['Investigating', 'Resolved', 'FalsePositive'].map(status => (
              <button key={status}
                onClick={() => updateAnomaly.mutate({ id: selectedAnomaly.id, status })}
                disabled={updateAnomaly.isPending}
                className="px-3 py-1.5 text-xs border border-slate-200 rounded-lg hover:bg-slate-50 disabled:opacity-60">
                {status === 'Investigating' ? 'قيد التحقيق' : status === 'Resolved' ? 'تم الحل' : 'غير صحيح'}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
