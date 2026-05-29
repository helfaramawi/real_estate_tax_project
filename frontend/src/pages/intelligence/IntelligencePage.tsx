import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { MapContainer, TileLayer, CircleMarker, Tooltip } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { AlertTriangle, Map, TrendingUp } from 'lucide-react'
import api from '../../lib/api'
import PageHeader from '../../components/PageHeader'
import StatusBadge from '../../components/StatusBadge'

type HeatmapCell = { centerLat: number; centerLon: number; avgRiskScore: number; propertyCount: number }
type Anomaly = { id: string; anomalyType: string; severity: string; status: string; lat?: number; lon?: number; description?: string; detectedAt: string }
type Cluster = { id: string; clusterLabel: number; centroidLat?: number; centroidLon?: number; propertyCount: number; medianValueSqm?: number; governorate?: string; computedAt: string }

const SEVERITY_COLOR: Record<string, string> = {
  Critical: '#dc2626', High: '#ea580c', Medium: '#d97706', Low: '#65a30d',
}

const RISK_COLOR = (s: number) => s >= 0.75 ? '#dc2626' : s >= 0.5 ? '#ea580c' : s >= 0.25 ? '#d97706' : '#65a30d'

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
const defaultMapCenter: [number, number] = [30.0444, 31.2357]
const mapContainerStyle = { height: '100%', width: '100%' }

const fallbackHeatmap: HeatmapCell[] = [
  { centerLat: 30.0475, centerLon: 31.2124, avgRiskScore: 0.82, propertyCount: 18 },
  { centerLat: 30.0586, centerLon: 31.2357, avgRiskScore: 0.64, propertyCount: 31 },
  { centerLat: 30.0328, centerLon: 31.2442, avgRiskScore: 0.41, propertyCount: 24 },
  { centerLat: 30.0219, centerLon: 31.2015, avgRiskScore: 0.18, propertyCount: 12 },
]

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

  // Cairo bounding box default
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
      <PageHeader
        title="الذكاء الاصطناعي"
        subtitle="التحليل الجغرافي ونماذج التعلم الآلي"
      />

      {/* KPI Summary Row */}
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

      {/* Tab bar */}
      <div className="flex gap-2 mb-4">
        {tabs.map(t => {
          const Icon = t.icon
          return (
            <button key={t.key} onClick={() => setTab(t.key)}
              className={`flex items-center gap-2 px-4 py-2 text-sm rounded-lg transition-colors ${
                tab === t.key ? 'bg-blue-600 text-white' : 'bg-white border border-slate-200 text-slate-600 hover:bg-slate-50'
              }`}>
              <Icon size={15} /> {t.label}
            </button>
          )
        })}
      </div>

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
            attribution='© <a href="https://openstreetmap.org">OpenStreetMap</a>'
          />

          {tab === 'heatmap' && displayedHeatmap.map((cell, i) => (
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

          {tab === 'anomalies' && anomalies?.filter(a => a.lat && a.lon).map(a => (
            <CircleMarker key={a.id} center={[a.lat!, a.lon!]}
              radius={9} fillOpacity={0.9} weight={2} color="#fff"
              fillColor={SEVERITY_COLOR[a.severity] ?? '#94a3b8'}
              eventHandlers={{ click: () => setSelectedAnomaly(a) }}>
              <Tooltip>
                <div className="text-xs">
                  <div className="font-semibold">{anomalyTypeAr[a.anomalyType] ?? a.anomalyType}</div>
                  <div>{severityAr[a.severity] ?? a.severity}</div>
                </div>
              </Tooltip>
            </CircleMarker>
          ))}

          {tab === 'clusters' && clusters?.filter(c => c.centroidLat && c.centroidLon).map(c => (
            <CircleMarker key={c.id} center={[c.centroidLat!, c.centroidLon!]}
              radius={Math.min(6 + Math.log(c.propertyCount + 1) * 3, 24)}
              fillOpacity={0.7} weight={1} color="#3b82f6" fillColor="#93c5fd">
              <Tooltip>
                <div className="text-xs">
                  <div className="font-semibold">{c.governorate ?? 'تجمع رقم ' + c.clusterLabel}</div>
                  <div>العقارات: {c.propertyCount}</div>
                  {c.medianValueSqm && <div>متوسط القيمة: {Number(c.medianValueSqm).toLocaleString()} ج.م/م²</div>}
                </div>
              </Tooltip>
            </CircleMarker>
          ))}
        </MapContainer>
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
                ({anomalies?.filter(a => a.severity === sev).length ?? 0})
              </span>
            </div>
          ))}
        </div>
      )}

      {/* Anomaly detail panel */}
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
            {['Investigating', 'Resolved', 'FalsePositive'].map(s => (
              <button key={s}
                onClick={() => updateAnomaly.mutate({ id: selectedAnomaly.id, status: s })}
                disabled={updateAnomaly.isPending}
                className="px-3 py-1.5 text-xs border border-slate-200 rounded-lg hover:bg-slate-50 disabled:opacity-60">
                {s === 'Investigating' ? 'قيد التحقيق' : s === 'Resolved' ? 'تم الحل' : 'غير صحيح'}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
