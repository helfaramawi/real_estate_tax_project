import { useEffect, useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { CircleMarker, MapContainer, Popup, TileLayer, useMap } from 'react-leaflet'
import type { LatLngBoundsExpression, LatLngExpression, PathOptions } from 'leaflet'
import type { AxiosError } from 'axios'
import 'leaflet/dist/leaflet.css'
import { AlertTriangle, Map, TrendingUp } from 'lucide-react'
import api from '../../lib/api'
import PageHeader from '../../components/PageHeader'
import StatusBadge from '../../components/StatusBadge'

type HeatmapCell = { centerLat: number; centerLon: number; avgRiskScore: number; propertyCount: number }
type RawHeatmapCell = Partial<HeatmapCell> & { CenterLat?: number; CenterLon?: number; AvgRiskScore?: number; PropertyCount?: number }
type Anomaly = { id: string; anomalyType: string; severity: string; status: string; lat?: number; lon?: number; description?: string; detectedAt: string }
type Cluster = { id: string; clusterLabel: number; centroidLat?: number; centroidLon?: number; propertyCount: number; medianValueSqm?: number; governorate?: string; computedAt: string }
type GeoBounds = { minLat: number; minLon: number; maxLat: number; maxLon: number }

type MapPoint = { lat: number; lon: number }
type ApiErrorPayload = { error?: string; message?: string; title?: string }

const severityColor: Record<string, string> = {
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
const queryBounds: GeoBounds = { minLat: 29.5, minLon: 30.5, maxLat: 30.5, maxLon: 31.5 }
const queryBoundsLabel = `${queryBounds.minLat},${queryBounds.minLon},${queryBounds.maxLat},${queryBounds.maxLon}`
const defaultCenter: LatLngExpression = [30.0444, 31.2357]
const fallbackHeatmap: HeatmapCell[] = [
  { centerLat: 30.0475, centerLon: 31.2124, avgRiskScore: 0.82, propertyCount: 18 },
  { centerLat: 30.0586, centerLon: 31.2357, avgRiskScore: 0.64, propertyCount: 31 },
  { centerLat: 30.0328, centerLon: 31.2442, avgRiskScore: 0.41, propertyCount: 24 },
  { centerLat: 30.0219, centerLon: 31.2015, avgRiskScore: 0.18, propertyCount: 12 },
]


function getErrorPayload(error: unknown) {
  return (error as AxiosError<ApiErrorPayload> | undefined)?.response?.data
}

function getHttpStatus(error: unknown) {
  return (error as AxiosError | undefined)?.response?.status
}

function getHeatmapErrorDetails(error: unknown) {
  const status = getHttpStatus(error)
  const payload = getErrorPayload(error)
  const serverMessage = payload?.error ?? payload?.message ?? payload?.title

  if (status === 401) return { status, title: 'جلسة الدخول غير صالحة', action: 'سجّل الدخول مرة أخرى بحساب Admin أو SuperAdmin أو TaxOfficer.', serverMessage }
  if (status === 403) return { status, title: 'صلاحيات غير كافية', action: 'الحساب الحالي لا يملك دور Admin/SuperAdmin/TaxOfficer المطلوب لخريطة المخاطر.', serverMessage }
  if (status === 404) return { status, title: 'مسار الخدمة غير موجود', action: 'تأكد أن API container يعمل وأن nginx يمرر /api/v2/geo/risk-heatmap إلى الخادم.', serverMessage }
  if (status && status >= 500) return { status, title: 'خطأ في الخادم أو قاعدة البيانات', action: 'راجع docker compose logs --tail=150 api وابحث عن أخطاء property_locations أو risk_scores أو migrations.', serverMessage }
  if (!status) return { status: null, title: 'تعذر الاتصال بالخادم', action: 'تأكد أن API يعمل وأن المتصفح يستطيع الوصول إلى /api/v2/geo/risk-heatmap.', serverMessage }

  return { status, title: 'تعذر تحميل بيانات المخاطر', action: 'افتح DevTools > Network وافحص طلب risk-heatmap لمعرفة سبب الفشل.', serverMessage }
}

function toNumber(value: unknown) {
  const numberValue = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(numberValue) ? numberValue : null
}

function normalizeHeatmapCells(cells: RawHeatmapCell[] | undefined) {
  return (cells ?? [])
    .map(cell => {
      const centerLat = toNumber(cell.centerLat ?? cell.CenterLat)
      const centerLon = toNumber(cell.centerLon ?? cell.CenterLon)
      const avgRiskScore = toNumber(cell.avgRiskScore ?? cell.AvgRiskScore)
      const propertyCount = toNumber(cell.propertyCount ?? cell.PropertyCount)

      if (centerLat === null || centerLon === null || avgRiskScore === null || propertyCount === null) return null

      return {
        centerLat,
        centerLon,
        avgRiskScore: Math.min(1, Math.max(0, avgRiskScore)),
        propertyCount: Math.max(0, Math.round(propertyCount)),
      } satisfies HeatmapCell
    })
    .filter((cell): cell is HeatmapCell => cell !== null)
}

function riskColor(score: number) {
  if (score >= 0.75) return '#dc2626'
  if (score >= 0.5) return '#ea580c'
  if (score >= 0.25) return '#d97706'
  return '#65a30d'
}

function riskLabel(score: number) {
  if (score >= 0.75) return 'خطر مرتفع جداً'
  if (score >= 0.5) return 'خطر مرتفع'
  if (score >= 0.25) return 'خطر متوسط'
  return 'خطر منخفض'
}

function boundsFromPoints(points: MapPoint[]): LatLngBoundsExpression | null {
  if (points.length === 0) return null

  const latitudes = points.map(point => point.lat)
  const longitudes = points.map(point => point.lon)
  const minLat = Math.min(...latitudes)
  const maxLat = Math.max(...latitudes)
  const minLon = Math.min(...longitudes)
  const maxLon = Math.max(...longitudes)
  const latSpan = Math.max(maxLat - minLat, 0.04)
  const lonSpan = Math.max(maxLon - minLon, 0.04)
  const latPadding = latSpan * 0.55
  const lonPadding = lonSpan * 0.55

  return [
    [minLat - latPadding, minLon - lonPadding],
    [maxLat + latPadding, maxLon + lonPadding],
  ]
}

function heatmapRadius(cell: HeatmapCell) {
  return Math.min(38, Math.max(14, 12 + Math.sqrt(cell.propertyCount) * 3 + cell.avgRiskScore * 10))
}

function heatmapStyle(cell: HeatmapCell, isSelected: boolean): PathOptions {
  const color = riskColor(cell.avgRiskScore)
  return {
    color: isSelected ? '#2563eb' : '#ffffff',
    fillColor: color,
    fillOpacity: isSelected ? 0.88 : 0.72,
    opacity: 1,
    weight: isSelected ? 4 : 2,
  }
}

function FitMapToData({ bounds }: { bounds: LatLngBoundsExpression | null }) {
  const map = useMap()

  useEffect(() => {
    if (bounds) {
      map.fitBounds(bounds, { padding: [48, 48], maxZoom: 14 })
    } else {
      map.setView(defaultCenter, 11)
    }
  }, [bounds, map])

  return null
}

function HeatmapLegend() {
  const entries = [
    ['خطر مرتفع جداً', '#dc2626'],
    ['خطر مرتفع', '#ea580c'],
    ['خطر متوسط', '#d97706'],
    ['خطر منخفض', '#65a30d'],
  ]

  return (
    <div className="absolute right-4 top-4 z-[1000] rounded-xl border border-white/70 bg-white/95 p-3 text-xs text-slate-600 shadow-lg" dir="rtl">
      <div className="mb-2 font-semibold text-slate-800">مفتاح الألوان</div>
      {entries.map(([label, color]) => (
        <div key={label} className="mb-1 flex items-center gap-2">
          <span className="h-3 w-3 rounded-full" style={{ backgroundColor: color }} />
          <span>{label}</span>
        </div>
      ))}
    </div>
  )
}

function HeatmapCircle({ cell, isSelected, onSelect }: { cell: HeatmapCell; isSelected: boolean; onSelect: (cell: HeatmapCell) => void }) {
  const riskPercent = Math.round(cell.avgRiskScore * 100)

  return (
    <CircleMarker
      center={[cell.centerLat, cell.centerLon]}
      radius={heatmapRadius(cell)}
      pathOptions={heatmapStyle(cell, isSelected)}
      eventHandlers={{
        click: () => onSelect(cell),
        mouseover: () => onSelect(cell),
      }}
    >
      <Popup>
        <div dir="rtl" className="min-w-44 text-right text-sm">
          <div className="mb-1 font-semibold text-slate-800">{riskLabel(cell.avgRiskScore)}</div>
          <div>متوسط الخطر: <strong>{riskPercent}%</strong></div>
          <div>عدد العقارات: <strong>{cell.propertyCount.toLocaleString()}</strong></div>
          <div className="mt-1 text-xs text-slate-500">{cell.centerLat.toFixed(4)}, {cell.centerLon.toFixed(4)}</div>
        </div>
      </Popup>
    </CircleMarker>
  )
}

export default function IntelligencePage() {
  const qc = useQueryClient()
  const [tab, setTab] = useState<'heatmap' | 'anomalies' | 'clusters'>('heatmap')
  const [selectedAnomaly, setSelectedAnomaly] = useState<Anomaly | null>(null)
  const [selectedHeatmapCell, setSelectedHeatmapCell] = useState<HeatmapCell | null>(null)

  const heatmapQuery = useQuery<HeatmapCell[]>({
    queryKey: ['heatmap', queryBoundsLabel],
    queryFn: () => api.get(`/v2/geo/risk-heatmap?minLat=${queryBounds.minLat}&minLon=${queryBounds.minLon}&maxLat=${queryBounds.maxLat}&maxLon=${queryBounds.maxLon}`).then(r => normalizeHeatmapCells(r.data.data?.cells)),
    enabled: tab === 'heatmap',
  })

  const anomaliesQuery = useQuery<Anomaly[]>({
    queryKey: ['anomalies', 'Open'],
    queryFn: () => api.get('/v2/geo/anomalies?status=Open').then(r => r.data.data ?? []),
    enabled: tab === 'anomalies',
  })

  const clustersQuery = useQuery<Cluster[]>({
    queryKey: ['geo-clusters'],
    queryFn: () => api.get('/v2/geo/clusters').then(r => r.data.data ?? []),
    enabled: tab === 'clusters',
  })

  const { data: summary } = useQuery({
    queryKey: ['intel-summary'],
    queryFn: () => api.get('/v2/intelligence/dashboard/summary').then(r => r.data.data),
  })

  const displayedHeatmap = useMemo(() => {
    if (tab !== 'heatmap' || heatmapQuery.isLoading) return []
    return heatmapQuery.data && heatmapQuery.data.length > 0 ? heatmapQuery.data : fallbackHeatmap
  }, [heatmapQuery.data, heatmapQuery.isLoading, tab])

  const anomalies = anomaliesQuery.data ?? []
  const clusters = clustersQuery.data ?? []
  const activeHeatmapCell = tab === 'heatmap' ? selectedHeatmapCell ?? displayedHeatmap[0] ?? null : null
  const isFallbackHeatmap = tab === 'heatmap' && !heatmapQuery.isLoading && (!heatmapQuery.data || heatmapQuery.data.length === 0)
  const mapBounds = useMemo(() => {
    if (tab === 'heatmap') return boundsFromPoints(displayedHeatmap.map(cell => ({ lat: cell.centerLat, lon: cell.centerLon })))
    if (tab === 'anomalies') return boundsFromPoints(anomalies.filter(item => item.lat && item.lon).map(item => ({ lat: item.lat!, lon: item.lon! })))
    return boundsFromPoints(clusters.filter(item => item.centroidLat && item.centroidLon).map(item => ({ lat: item.centroidLat!, lon: item.centroidLon! })))
  }, [anomalies, clusters, displayedHeatmap, tab])
  const heatmapErrorDetails = heatmapQuery.isError ? getHeatmapErrorDetails(heatmapQuery.error) : null
  const fallbackHeatmapTitle = heatmapErrorDetails?.title ?? 'لا توجد خلايا مخاطر حقيقية داخل النطاق الحالي'
  const fallbackHeatmapMessage = heatmapErrorDetails?.action ?? 'الخادم استجاب بنجاح لكن لم يرجع خلايا داخل النطاق الحالي؛ تأكد من وجود property_locations داخل نطاق الطلب.'

  const updateAnomaly = useMutation({
    mutationFn: ({ id, status, notes }: { id: string; status: string; notes?: string }) => api.patch(`/v2/geo/anomalies/${id}/status`, { status, notes }),
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
    <div className="space-y-6" dir="rtl">
      <PageHeader title="الذكاء الاصطناعي" subtitle="التحليل الجغرافي ونماذج التعلم الآلي" />

      <div className="flex flex-wrap gap-2">
        {tabs.map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            type="button"
            onClick={() => setTab(key)}
            className={`flex items-center gap-2 rounded-lg border px-4 py-2 text-sm transition ${tab === key ? 'border-blue-600 bg-blue-600 text-white' : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'}`}
          >
            <Icon className="h-4 w-4" />
            {label}
          </button>
        ))}
      </div>

      <div className="relative overflow-hidden rounded-xl border border-slate-200 shadow-sm" style={{ height: 560 }}>
        {tab === 'heatmap' && heatmapQuery.isLoading && (
          <div className="pointer-events-none absolute inset-x-4 top-4 z-[1000] rounded-lg border border-blue-100 bg-white/95 p-3 text-sm text-slate-700 shadow-sm">
            جار تحميل بيانات خريطة المخاطر...
          </div>
        )}

        {tab === 'heatmap' && isFallbackHeatmap && (
          <div className="absolute inset-x-4 top-4 z-[1000] rounded-lg border border-amber-200 bg-amber-50/95 p-3 text-sm text-amber-900 shadow-sm" dir="rtl">
            <div className="flex flex-wrap items-center gap-2 font-semibold">
              <span>وضع توضيحي للخريطة</span>
              {heatmapErrorDetails?.status && <span className="rounded bg-amber-100 px-2 py-0.5 text-xs">HTTP {heatmapErrorDetails.status}</span>}
            </div>
            <div className="mt-1 font-medium">{fallbackHeatmapTitle}</div>
            <div className="mt-1">{fallbackHeatmapMessage}</div>
            {heatmapErrorDetails?.serverMessage && <div className="mt-1 text-xs text-amber-700">رسالة الخادم: {heatmapErrorDetails.serverMessage}</div>}
            <div className="mt-1 text-xs text-amber-700">Endpoint: /api/v2/geo/risk-heatmap?minLat={queryBounds.minLat}&amp;minLon={queryBounds.minLon}&amp;maxLat={queryBounds.maxLat}&amp;maxLon={queryBounds.maxLon}</div>
            <button type="button" onClick={() => heatmapQuery.refetch()} className="mt-2 rounded border border-amber-300 bg-white px-3 py-1 text-xs font-semibold text-amber-900 hover:bg-amber-100">
              إعادة المحاولة
            </button>
          </div>
        )}

        <MapContainer center={defaultCenter} zoom={11} scrollWheelZoom className="h-full w-full">
          <TileLayer
            attribution="&copy; OpenStreetMap contributors"
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <FitMapToData bounds={mapBounds} />

          {tab === 'heatmap' && displayedHeatmap.map((cell, index) => (
            <HeatmapCircle key={`${cell.centerLat}-${cell.centerLon}-${index}`} cell={cell} isSelected={selectedHeatmapCell === cell} onSelect={setSelectedHeatmapCell} />
          ))}

          {tab === 'anomalies' && anomalies.filter(item => item.lat && item.lon).map(item => (
            <CircleMarker
              key={item.id}
              center={[item.lat!, item.lon!]}
              radius={12}
              pathOptions={{ color: '#ffffff', fillColor: severityColor[item.severity] ?? '#94a3b8', fillOpacity: 0.82, weight: 2 }}
              eventHandlers={{ click: () => setSelectedAnomaly(item) }}
            >
              <Popup>
                <div dir="rtl" className="min-w-48 text-right text-sm">
                  <div className="font-semibold text-slate-800">{anomalyTypeAr[item.anomalyType] ?? item.anomalyType}</div>
                  <div className="mt-1">الخطورة: {severityAr[item.severity] ?? item.severity}</div>
                  {item.description && <p className="mt-2 text-xs text-slate-500">{item.description}</p>}
                </div>
              </Popup>
            </CircleMarker>
          ))}

          {tab === 'clusters' && clusters.filter(item => item.centroidLat && item.centroidLon).map(item => {
            const radius = Math.min(28, Math.max(12, 9 + Math.log(item.propertyCount + 1) * 3))
            return (
              <CircleMarker
                key={item.id}
                center={[item.centroidLat!, item.centroidLon!]}
                radius={radius}
                pathOptions={{ color: '#1d4ed8', fillColor: '#bfdbfe', fillOpacity: 0.72, weight: 2 }}
              >
                <Popup>
                  <div dir="rtl" className="min-w-44 text-right text-sm">
                    <div className="font-semibold text-slate-800">تجمع #{item.clusterLabel}</div>
                    <div>العقارات: <strong>{item.propertyCount.toLocaleString()}</strong></div>
                    {item.governorate && <div>المحافظة: {item.governorate}</div>}
                    {item.medianValueSqm && <div>وسيط سعر المتر: {item.medianValueSqm.toLocaleString()}</div>}
                  </div>
                </Popup>
              </CircleMarker>
            )
          })}
        </MapContainer>

        <HeatmapLegend />

        <div className="absolute bottom-4 left-4 z-[1000] max-w-md rounded-lg border border-white/70 bg-white/95 px-3 py-2 text-xs text-slate-600 shadow-lg" dir="rtl">
          <div>خريطة OpenStreetMap حقيقية مع طبقات مخاطر داخلية.</div>
          <div className="mt-1 text-[11px] text-slate-400">نطاق الطلب: {queryBoundsLabel}</div>
          {isFallbackHeatmap && <div className="mt-1 font-semibold text-amber-700">المعروض الآن نقاط توضيحية لحين رجوع بيانات حقيقية من الخادم.</div>}
        </div>

        {tab === 'heatmap' && activeHeatmapCell && (
          <div className="absolute bottom-4 right-4 z-[1000] w-[min(24rem,calc(100%-2rem))] rounded-xl border border-slate-200 bg-white/95 p-4 text-sm shadow-xl backdrop-blur" dir="rtl">
            <div className="mb-2 flex items-start justify-between gap-3">
              <div>
                <h3 className="font-semibold text-slate-800">معلومات نقطة المخاطر</h3>
                <p className="text-xs text-slate-500">اضغط على الدائرة داخل خريطة OpenStreetMap لتثبيت التفاصيل.</p>
              </div>
              {selectedHeatmapCell && (
                <button type="button" onClick={() => setSelectedHeatmapCell(null)} className="text-slate-400 hover:text-slate-600" aria-label="إلغاء تحديد نقطة المخاطر">
                  ✕
                </button>
              )}
            </div>
            <div className="grid grid-cols-2 gap-2">
              <div className="rounded-lg bg-slate-50 p-2">
                <div className="text-xs text-slate-500">متوسط الخطر</div>
                <div className="font-bold text-slate-800">{Math.round(activeHeatmapCell.avgRiskScore * 100)}%</div>
              </div>
              <div className="rounded-lg bg-slate-50 p-2">
                <div className="text-xs text-slate-500">عدد العقارات</div>
                <div className="font-bold text-slate-800">{activeHeatmapCell.propertyCount.toLocaleString()}</div>
              </div>
            </div>
            <div className="mt-2 rounded-lg bg-slate-50 p-2 text-xs text-slate-600">
              مركز الخلية: {activeHeatmapCell.centerLat.toFixed(4)}, {activeHeatmapCell.centerLon.toFixed(4)}
            </div>
          </div>
        )}
      </div>

      {tab === 'heatmap' && selectedHeatmapCell && (
        <div className="mt-4 rounded-xl border border-slate-200 bg-white p-5">
          <div className="mb-3 flex items-start justify-between">
            <div>
              <h3 className="font-semibold text-slate-800">تفاصيل منطقة المخاطر</h3>
              <p className="mt-1 text-xs text-slate-500">تظهر هذه البطاقة عند النقر على أي دائرة في خريطة المخاطر.</p>
            </div>
            <button type="button" onClick={() => setSelectedHeatmapCell(null)} className="text-lg text-slate-400 hover:text-slate-600">
              ✕
            </button>
          </div>
          <div className="grid grid-cols-1 gap-3 text-sm md:grid-cols-3">
            <div className="rounded-lg bg-slate-50 p-3">
              <div className="mb-1 text-xs text-slate-500">متوسط درجة الخطر</div>
              <div className="font-bold text-slate-800">{Math.round(selectedHeatmapCell.avgRiskScore * 100)}%</div>
            </div>
            <div className="rounded-lg bg-slate-50 p-3">
              <div className="mb-1 text-xs text-slate-500">عدد العقارات في المنطقة</div>
              <div className="font-bold text-slate-800">{selectedHeatmapCell.propertyCount.toLocaleString()}</div>
            </div>
            <div className="rounded-lg bg-slate-50 p-3">
              <div className="mb-1 text-xs text-slate-500">مركز الخلية الجغرافية</div>
              <div className="font-bold text-slate-800">{selectedHeatmapCell.centerLat.toFixed(4)}, {selectedHeatmapCell.centerLon.toFixed(4)}</div>
            </div>
          </div>
        </div>
      )}

      {tab === 'anomalies' && (
        <div className="mt-4 flex flex-wrap gap-4">
          {Object.entries(severityColor).map(([severity, color]) => (
            <div key={severity} className="flex items-center gap-2">
              <div className="h-3 w-3 rounded-full" style={{ backgroundColor: color }} />
              <span className="text-sm text-slate-600">{severityAr[severity]}</span>
              <span className="text-xs text-slate-400">({anomalies.filter(item => item.severity === severity).length})</span>
            </div>
          ))}
        </div>
      )}

      {selectedAnomaly && (
        <div className="mt-4 rounded-xl border border-slate-200 bg-white p-5">
          <div className="mb-3 flex items-start justify-between">
            <div>
              <h3 className="font-semibold text-slate-800">{anomalyTypeAr[selectedAnomaly.anomalyType] ?? selectedAnomaly.anomalyType}</h3>
              <div className="mt-1 flex gap-2">
                <StatusBadge label={selectedAnomaly.severity} />
                <StatusBadge label={selectedAnomaly.status} />
              </div>
            </div>
            <button type="button" onClick={() => setSelectedAnomaly(null)} className="text-lg text-slate-400 hover:text-slate-600">
              ✕
            </button>
          </div>
          {selectedAnomaly.description && <p className="mb-4 text-sm text-slate-600">{selectedAnomaly.description}</p>}
          <div className="flex gap-2">
            {['Investigating', 'Resolved', 'FalsePositive'].map(status => (
              <button
                key={status}
                type="button"
                onClick={() => updateAnomaly.mutate({ id: selectedAnomaly.id, status })}
                disabled={updateAnomaly.isPending}
                className="rounded-lg border border-slate-200 px-3 py-1.5 text-xs hover:bg-slate-50 disabled:opacity-60"
              >
                {status === 'Investigating' ? 'قيد التحقيق' : status === 'Resolved' ? 'تم الحل' : 'غير صحيح'}
              </button>
            ))}
          </div>
        </div>
      )}

      {summary && (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <p className="text-sm text-slate-500">دقة النماذج</p>
            <p className="text-2xl font-bold text-slate-800">{Math.round((summary.modelAccuracy ?? 0) * 100)}%</p>
          </div>
          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <p className="text-sm text-slate-500">العقارات عالية المخاطر</p>
            <p className="text-2xl font-bold text-rose-600">{summary.highRiskProperties ?? 0}</p>
          </div>
          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <p className="text-sm text-slate-500">الشذوذات المفتوحة</p>
            <p className="text-2xl font-bold text-amber-600">{summary.openAnomalies ?? 0}</p>
          </div>
        </div>
      )}
    </div>
  )
}
