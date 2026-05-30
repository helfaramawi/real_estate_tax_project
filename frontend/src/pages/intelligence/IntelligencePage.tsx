import { useMemo, useState, type ReactNode } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, Map, TrendingUp } from 'lucide-react'
import api from '../../lib/api'
import PageHeader from '../../components/PageHeader'
import StatusBadge from '../../components/StatusBadge'

type HeatmapCell = { centerLat: number; centerLon: number; avgRiskScore: number; propertyCount: number }
type RawHeatmapCell = Partial<HeatmapCell> & { CenterLat?: number; CenterLon?: number; AvgRiskScore?: number; PropertyCount?: number }
type Anomaly = { id: string; anomalyType: string; severity: string; status: string; lat?: number; lon?: number; description?: string; detectedAt: string }
type Cluster = { id: string; clusterLabel: number; centroidLat?: number; centroidLon?: number; propertyCount: number; medianValueSqm?: number; governorate?: string; computedAt: string }
type MapPoint = { x: number; y: number }
type GeoBounds = { minLat: number; minLon: number; maxLat: number; maxLon: number }

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
const fallbackHeatmap: HeatmapCell[] = [
  { centerLat: 30.0475, centerLon: 31.2124, avgRiskScore: 0.82, propertyCount: 18 },
  { centerLat: 30.0586, centerLon: 31.2357, avgRiskScore: 0.64, propertyCount: 31 },
  { centerLat: 30.0328, centerLon: 31.2442, avgRiskScore: 0.41, propertyCount: 24 },
  { centerLat: 30.0219, centerLon: 31.2015, avgRiskScore: 0.18, propertyCount: 12 },
]

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

function clampPercent(value: number) {
  return Math.min(94, Math.max(6, value))
}

function boundsFromPoints(points: Array<{ lat: number; lon: number }>): GeoBounds {
  if (points.length === 0) return queryBounds

  const latitudes = points.map(point => point.lat)
  const longitudes = points.map(point => point.lon)
  const minLat = Math.min(...latitudes)
  const maxLat = Math.max(...latitudes)
  const minLon = Math.min(...longitudes)
  const maxLon = Math.max(...longitudes)
  const latSpan = Math.max(maxLat - minLat, 0.035)
  const lonSpan = Math.max(maxLon - minLon, 0.035)
  const latPadding = latSpan * 0.45
  const lonPadding = lonSpan * 0.45

  return {
    minLat: minLat - latPadding,
    maxLat: maxLat + latPadding,
    minLon: minLon - lonPadding,
    maxLon: maxLon + lonPadding,
  }
}

function projectPoint(lat: number, lon: number, viewBounds: GeoBounds): MapPoint {
  return {
    x: clampPercent(((lon - viewBounds.minLon) / (viewBounds.maxLon - viewBounds.minLon)) * 100),
    y: clampPercent(((viewBounds.maxLat - lat) / (viewBounds.maxLat - viewBounds.minLat)) * 100),
  }
}

function HeatmapMarker({ cell, isSelected, viewBounds, onSelect }: { cell: HeatmapCell; isSelected: boolean; viewBounds: GeoBounds; onSelect: (cell: HeatmapCell) => void }) {
  const point = projectPoint(cell.centerLat, cell.centerLon, viewBounds)
  const riskPercent = Math.round(cell.avgRiskScore * 100)

  return (
    <button
      type="button"
      onClick={() => onSelect(cell)}
      onMouseEnter={() => onSelect(cell)}
      onFocus={() => onSelect(cell)}
      className={`group absolute flex items-center justify-center rounded-full border-2 border-white shadow-lg outline-none transition-transform hover:scale-125 focus:scale-125 focus:ring-4 focus:ring-blue-200 ${isSelected ? 'scale-125 ring-4 ring-blue-200' : ''}`}
      style={{ left: `${point.x}%`, top: `${point.y}%`, width: 46, height: 46, transform: 'translate(-50%, -50%)', backgroundColor: riskColor(cell.avgRiskScore), zIndex: isSelected ? 60 : 40 }}
      aria-label={`تفاصيل منطقة مخاطر: متوسط الخطر ${riskPercent}%، عدد العقارات ${cell.propertyCount}`}
      title={`متوسط الخطر ${riskPercent}% - العقارات ${cell.propertyCount}`}
    >
      <span className="absolute inset-[-8px] rounded-full border-2 border-white/70 opacity-70" />
      <span className="relative z-10 text-xs font-bold text-white drop-shadow">{riskPercent}%</span>
      <span className="sr-only">تفاصيل منطقة مخاطر</span>
      <div className="pointer-events-none absolute bottom-14 left-1/2 z-[1200] w-56 -translate-x-1/2 rounded-lg bg-slate-900 px-3 py-2 text-xs text-white opacity-0 shadow-lg transition-opacity group-hover:opacity-100 group-focus:opacity-100">
        <div className="font-semibold">متوسط الخطر: {riskPercent}%</div>
        <div>العقارات: {cell.propertyCount.toLocaleString()}</div>
        <div className="text-slate-300">اضغط أو مرر المؤشر لتثبيت التفاصيل</div>
      </div>
    </button>
  )
}

function MapSurface({ children, viewBounds, fallbackMode }: { children: ReactNode; viewBounds: GeoBounds; fallbackMode: boolean }) {
  return (
    <div className="relative h-full w-full overflow-hidden bg-slate-100">
      <div className="absolute inset-0 bg-gradient-to-br from-blue-50 via-white to-emerald-50" />
      <div className="absolute inset-0 opacity-70" style={{ backgroundImage: 'linear-gradient(90deg, rgba(148,163,184,.22) 1px, transparent 1px), linear-gradient(0deg, rgba(148,163,184,.22) 1px, transparent 1px)', backgroundSize: '64px 64px' }} />
      <svg className="pointer-events-none absolute inset-0 h-full w-full" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">
        <path d="M8 92 C24 70 30 58 34 42 C38 27 49 18 68 6" fill="none" stroke="rgba(59,130,246,.24)" strokeWidth="5" />
        <path d="M0 72 C20 66 42 64 100 58" fill="none" stroke="rgba(100,116,139,.24)" strokeWidth="1.4" strokeDasharray="2 2" />
        <path d="M14 18 C34 35 52 42 96 38" fill="none" stroke="rgba(100,116,139,.2)" strokeWidth="1.2" strokeDasharray="2 3" />
        <path d="M18 8 L86 92" fill="none" stroke="rgba(16,185,129,.16)" strokeWidth="7" />
      </svg>
      <div className="absolute left-[18%] top-[72%] rounded bg-blue-50/90 px-2 py-1 text-[10px] font-semibold text-blue-700">النيل</div>
      <div className="absolute left-[54%] top-[35%] rounded bg-white/80 px-2 py-1 text-[10px] text-slate-500">محور رئيسي</div>
      <div className="absolute left-6 top-6 rounded-lg border border-white/70 bg-white/85 px-3 py-2 text-xs text-slate-600 shadow-sm">
        النطاق المعروض: {viewBounds.minLat.toFixed(3)}, {viewBounds.minLon.toFixed(3)} → {viewBounds.maxLat.toFixed(3)}, {viewBounds.maxLon.toFixed(3)}
        <div className="mt-1 text-[10px] text-slate-400">نطاق الطلب: {queryBoundsLabel}</div>
        {fallbackMode && <div className="mt-1 font-semibold text-amber-700">وضع توضيحي لحين توفر بيانات حقيقية</div>}
      </div>
      <div className="absolute bottom-4 left-4 rounded bg-white/90 px-2 py-1 text-[11px] text-slate-500 shadow-sm">
        خريطة تحليلية داخلية - الدوائر الملونة تعرض نسبة الخطر، واضغط على أي دائرة للتفاصيل
      </div>
      <div className="absolute right-4 top-4 z-20 rounded-xl border border-white/70 bg-white/90 p-3 text-xs text-slate-600 shadow-sm">
        <div className="mb-2 font-semibold text-slate-800">مفتاح الألوان</div>
        {[
          ['خطر مرتفع', '#dc2626'],
          ['خطر متوسط مرتفع', '#ea580c'],
          ['خطر متوسط', '#d97706'],
          ['خطر منخفض', '#65a30d'],
        ].map(([label, color]) => (
          <div key={label} className="mb-1 flex items-center gap-2">
            <span className="h-3 w-3 rounded-full" style={{ backgroundColor: color }} />
            <span>{label}</span>
          </div>
        ))}
      </div>
      {children}
    </div>
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
  const fallbackHeatmapMessage = heatmapQuery.isError
    ? 'تعذر تحميل البيانات الحقيقية من الخادم؛ نعرض طبقة توضيحية مؤقتة حتى تتأكد من صلاحيات المستخدم ووجود بيانات مواقع/مخاطر.'
    : 'لا توجد خلايا مخاطر حقيقية داخل النطاق الحالي؛ نعرض طبقة توضيحية مؤقتة قابلة للنقر.'

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
    <div>
      <PageHeader title="الذكاء الاصطناعي" subtitle="التحليل الجغرافي ونماذج التعلم الآلي" />

      {summary && (
        <div className="mb-6 grid grid-cols-2 gap-4 md:grid-cols-4">
          {[
            { label: 'تنبؤات عالية الخطر', value: summary.highRiskCount, color: 'text-red-600' },
            { label: 'مشبوه بالاحتيال', value: summary.fraudSuspectCount, color: 'text-orange-600' },
            { label: 'شذوذات مفتوحة', value: summary.openAnomalies, color: 'text-yellow-600' },
            { label: 'بانتظار المراجعة', value: summary.pendingReview, color: 'text-blue-600' },
          ].map(kpi => (
            <div key={kpi.label} className="rounded-xl border border-slate-200 bg-white p-4">
              <div className="mb-1 text-xs text-slate-500">{kpi.label}</div>
              <div className={`text-2xl font-bold ${kpi.color}`}>{kpi.value?.toLocaleString()}</div>
            </div>
          ))}
        </div>
      )}

      <div className="mb-4 flex gap-2">
        {tabs.map(tabItem => {
          const Icon = tabItem.icon
          const isActive = tab === tabItem.key
          return (
            <button
              key={tabItem.key}
              type="button"
              onClick={() => setTab(tabItem.key)}
              className={`flex items-center gap-2 rounded-lg px-4 py-2 text-sm transition-colors ${isActive ? 'bg-blue-600 text-white' : 'border border-slate-200 bg-white text-slate-600 hover:bg-slate-50'}`}
            >
              <Icon size={15} />
              {tabItem.label}
            </button>
          )
        })}
      </div>

      <div className="relative overflow-hidden rounded-xl border border-slate-200 shadow-sm" style={{ height: 520 }}>
        {tab === 'heatmap' && heatmapQuery.isLoading && (
          <div className="pointer-events-none absolute inset-x-4 top-4 z-[1000] rounded-lg border border-blue-100 bg-white/95 p-3 text-sm text-slate-700 shadow-sm">
            جار تحميل بيانات خريطة المخاطر...
          </div>
        )}

        {tab === 'heatmap' && isFallbackHeatmap && (
          <div className="pointer-events-none absolute inset-x-4 top-4 z-[1000] rounded-lg border border-amber-200 bg-amber-50/95 p-3 text-sm text-amber-900 shadow-sm">
            <div className="font-semibold">وضع توضيحي للخريطة</div>
            <div className="mt-1">{fallbackHeatmapMessage}</div>
          </div>
        )}

        <MapSurface viewBounds={mapBounds} fallbackMode={isFallbackHeatmap}>
          {tab === 'heatmap' && displayedHeatmap.map((cell, index) => (
            <HeatmapMarker key={`${cell.centerLat}-${cell.centerLon}-${index}`} cell={cell} isSelected={selectedHeatmapCell === cell} viewBounds={mapBounds} onSelect={setSelectedHeatmapCell} />
          ))}

          {tab === 'anomalies' && anomalies.filter(item => item.lat && item.lon).map(item => {
            const point = projectPoint(item.lat!, item.lon!, mapBounds)
            return (
              <button
                key={item.id}
                type="button"
                onClick={() => setSelectedAnomaly(item)}
                className="absolute rounded-full border-2 border-white shadow-lg"
                style={{ left: `${point.x}%`, top: `${point.y}%`, width: 22, height: 22, transform: 'translate(-50%, -50%)', backgroundColor: severityColor[item.severity] ?? '#94a3b8' }}
                title={`${anomalyTypeAr[item.anomalyType] ?? item.anomalyType} - ${severityAr[item.severity] ?? item.severity}`}
              />
            )
          })}

          {tab === 'clusters' && clusters.filter(item => item.centroidLat && item.centroidLon).map(item => {
            const point = projectPoint(item.centroidLat!, item.centroidLon!, mapBounds)
            const size = Math.min(26 + Math.log(item.propertyCount + 1) * 5, 54)
            return (
              <div
                key={item.id}
                className="absolute grid place-items-center rounded-full border border-blue-500 bg-blue-200/80 text-[10px] font-bold text-blue-900 shadow"
                style={{ left: `${point.x}%`, top: `${point.y}%`, width: size, height: size, transform: 'translate(-50%, -50%)' }}
                title={`العقارات: ${item.propertyCount}`}
              >
                {item.propertyCount}
              </div>
            )
          })}
        </MapSurface>

        {tab === 'heatmap' && activeHeatmapCell && (
          <div className="absolute bottom-4 right-4 z-[1100] w-[min(24rem,calc(100%-2rem))] rounded-xl border border-slate-200 bg-white/95 p-4 text-sm shadow-xl backdrop-blur" dir="rtl">
            <div className="mb-2 flex items-start justify-between gap-3">
              <div>
                <h3 className="font-semibold text-slate-800">معلومات نقطة المخاطر</h3>
                <p className="text-xs text-slate-500">تتغير هذه البطاقة فور تمرير المؤشر أو الضغط على أي دائرة.</p>
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
    </div>
  )
}
