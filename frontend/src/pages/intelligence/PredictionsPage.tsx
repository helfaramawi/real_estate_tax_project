import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { CheckCircle, XCircle, ArrowUpCircle } from 'lucide-react'
import api from '../../lib/api'
import PageHeader from '../../components/PageHeader'
import DataTable from '../../components/DataTable'

type Prediction = {
  id: string
  propertyId: string
  predictionType: string
  score: number
  label: string
  confidence: number
  predictedAt: string
  isReviewed: boolean
  explanation?: Record<string, number>
}

const predictionTypeAr: Record<string, string> = {
  RiskScore: 'درجة المخاطر',
  FraudProbability: 'احتمال الاحتيال',
  DuplicateDetection: 'اكتشاف التكرار',
  ValuationAnomaly: 'شذوذ في التقييم',
  CollectionProbability: 'احتمال التحصيل',
}

const labelColor: Record<string, string> = {
  High: 'bg-red-100 text-red-700',
  Medium: 'bg-yellow-100 text-yellow-700',
  Low: 'bg-green-100 text-green-700',
  Suspect: 'bg-red-100 text-red-700',
  Clean: 'bg-green-100 text-green-700',
}

const labelAr: Record<string, string> = {
  High: 'مرتفع', Medium: 'متوسط', Low: 'منخفض',
  Suspect: 'مشبوه', Clean: 'نظيف',
}

export default function PredictionsPage() {
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [typeFilter, setTypeFilter] = useState('')
  const [minScore, setMinScore] = useState('')
  const [selected, setSelected] = useState<Prediction | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['predictions-review', page, typeFilter, minScore],
    queryFn: () => {
      const params = new URLSearchParams({ page: String(page), pageSize: '20' })
      if (typeFilter) params.set('type', typeFilter)
      if (minScore) params.set('minScore', minScore)
      return api.get(`/v2/intelligence/predictions/pending-review?${params}`).then(r => r.data.data)
    },
  })

  const predictions: Prediction[] = data?.items ?? []
  const total: number = data?.totalCount ?? 0

  const reviewMutation = useMutation({
    mutationFn: ({ id, outcome }: { id: string; outcome: string }) =>
      api.patch(`/v2/intelligence/predictions/${id}/review`, { outcome }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['predictions-review'] })
      setSelected(null)
    },
  })

  return (
    <div>
      <PageHeader
        title="مراجعة التنبؤات"
        subtitle="نتائج نماذج الذكاء الاصطناعي بانتظار مراجعة المختص"
      />

      {/* Filters */}
      <div className="flex gap-3 mb-4 flex-wrap">
        <select
          value={typeFilter}
          onChange={e => { setTypeFilter(e.target.value); setPage(1) }}
          className="border border-slate-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue-500">
          <option value="">جميع الأنواع</option>
          {Object.entries(predictionTypeAr).map(([k, v]) => (
            <option key={k} value={k}>{v}</option>
          ))}
        </select>
        <input
          type="number" min="0" max="1" step="0.05"
          placeholder="حد أدنى للنتيجة (0–1)"
          value={minScore}
          onChange={e => { setMinScore(e.target.value); setPage(1) }}
          className="border border-slate-300 rounded-lg px-3 py-2 text-sm w-48 focus:outline-none focus:border-blue-500"
        />
        <div className="text-sm text-slate-500 self-center">
          إجمالي: <strong>{total.toLocaleString()}</strong> تنبؤ
        </div>
      </div>

      <DataTable<Prediction>
        loading={isLoading}
        emptyMessage="لا توجد تنبؤات بانتظار المراجعة"
        columns={[
          { key: 'predictionType', header: 'النوع', render: r => predictionTypeAr[r.predictionType] ?? r.predictionType },
          {
            key: 'score', header: 'النتيجة',
            render: r => (
              <div className="flex items-center gap-2">
                <div className="w-24 bg-slate-100 rounded-full h-2">
                  <div className="h-2 rounded-full transition-all"
                    style={{ width: `${Math.round(r.score * 100)}%`, backgroundColor: r.score >= 0.75 ? '#dc2626' : r.score >= 0.45 ? '#d97706' : '#65a30d' }} />
                </div>
                <span className="text-xs font-mono text-slate-600">{(r.score * 100).toFixed(0)}%</span>
              </div>
            )
          },
          {
            key: 'label', header: 'التصنيف',
            render: r => (
              <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${labelColor[r.label] ?? 'bg-slate-100 text-slate-600'}`}>
                {labelAr[r.label] ?? r.label}
              </span>
            )
          },
          { key: 'confidence', header: 'الثقة', render: r => `${(r.confidence * 100).toFixed(0)}%` },
          { key: 'predictedAt', header: 'وقت التنبؤ', render: r => new Date(r.predictedAt).toLocaleString('ar-EG') },
          {
            key: 'actions', header: 'الإجراءات',
            render: r => (
              <div className="flex gap-1">
                <button onClick={e => { e.stopPropagation(); reviewMutation.mutate({ id: r.id, outcome: 'Confirmed' }) }}
                  className="p-1 text-green-600 hover:bg-green-50 rounded" title="تأكيد">
                  <CheckCircle size={16} />
                </button>
                <button onClick={e => { e.stopPropagation(); reviewMutation.mutate({ id: r.id, outcome: 'Dismissed' }) }}
                  className="p-1 text-red-500 hover:bg-red-50 rounded" title="رفض">
                  <XCircle size={16} />
                </button>
                <button onClick={e => { e.stopPropagation(); reviewMutation.mutate({ id: r.id, outcome: 'Escalated' }) }}
                  className="p-1 text-orange-500 hover:bg-orange-50 rounded" title="تصعيد">
                  <ArrowUpCircle size={16} />
                </button>
              </div>
            )
          },
        ]}
        data={predictions}
        onRowClick={r => setSelected(r)}
      />

      {/* Pagination */}
      {total > 20 && (
        <div className="flex gap-2 justify-center mt-4">
          <button disabled={page === 1} onClick={() => setPage(p => p - 1)}
            className="px-3 py-1.5 text-sm border border-slate-200 rounded-lg disabled:opacity-40 hover:bg-slate-50">
            السابق
          </button>
          <span className="px-3 py-1.5 text-sm text-slate-600">
            صفحة {page} من {Math.ceil(total / 20)}
          </span>
          <button disabled={page >= Math.ceil(total / 20)} onClick={() => setPage(p => p + 1)}
            className="px-3 py-1.5 text-sm border border-slate-200 rounded-lg disabled:opacity-40 hover:bg-slate-50">
            التالي
          </button>
        </div>
      )}

      {/* Explanation drawer */}
      {selected && (
        <div className="mt-4 bg-white rounded-xl border border-slate-200 p-5">
          <div className="flex justify-between items-start mb-3">
            <h3 className="font-semibold text-slate-800">تفسير التنبؤ (SHAP)</h3>
            <button onClick={() => setSelected(null)} className="text-slate-400 hover:text-slate-600">✕</button>
          </div>
          {selected.explanation && Object.keys(selected.explanation).length > 0 ? (
            <div className="space-y-2">
              {Object.entries(selected.explanation)
                .sort(([, a], [, b]) => Math.abs(b) - Math.abs(a))
                .slice(0, 8)
                .map(([feature, value]) => (
                  <div key={feature} className="flex items-center gap-3">
                    <div className="text-xs text-slate-500 w-48 truncate">{feature}</div>
                    <div className="flex-1 bg-slate-100 rounded-full h-2">
                      <div className="h-2 rounded-full"
                        style={{
                          width: `${Math.min(Math.abs(value) * 100, 100)}%`,
                          backgroundColor: value > 0 ? '#dc2626' : '#65a30d',
                        }} />
                    </div>
                    <div className={`text-xs font-mono w-12 text-right ${value > 0 ? 'text-red-600' : 'text-green-600'}`}>
                      {value > 0 ? '+' : ''}{value.toFixed(3)}
                    </div>
                  </div>
                ))}
              <p className="text-xs text-slate-400 mt-2">
                القيم الموجبة (حمراء) تزيد من النتيجة، السالبة (خضراء) تخفضها.
              </p>
            </div>
          ) : (
            <p className="text-sm text-slate-500">لا يوجد تفسير متاح لهذا التنبؤ.</p>
          )}
        </div>
      )}
    </div>
  )
}
