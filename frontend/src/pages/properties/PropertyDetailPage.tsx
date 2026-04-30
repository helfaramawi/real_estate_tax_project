import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowRight, CheckCircle } from 'lucide-react'
import api from '../../lib/api'
import type { Property } from '../../lib/types'
import StatusBadge from '../../components/StatusBadge'

function Detail({ label, value }: { label: string; value?: string | number | null }) {
  return (
    <div>
      <dt className="text-xs text-slate-500 font-medium">{label}</dt>
      <dd className="mt-0.5 text-sm text-slate-900">{value ?? '—'}</dd>
    </div>
  )
}

export default function PropertyDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const { data: property, isLoading } = useQuery<Property>({
    queryKey: ['property', id],
    queryFn: async () => {
      const res = await api.get(`/properties/${id}`)
      return res.data.data
    },
    enabled: !!id,
  })

  const verifyMutation = useMutation({
    mutationFn: async () => {
      await api.post(`/properties/${id}/verify`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['property', id] }),
  })

  if (isLoading) {
    return <div className="p-8 text-sm text-slate-400">جاري تحميل بيانات العقار…</div>
  }

  if (!property) {
    return <div className="p-8 text-sm text-red-500">العقار غير موجود.</div>
  }

  return (
    <div>
      <button onClick={() => navigate('/properties')}
        className="flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700 mb-5 transition-colors">
        <ArrowRight size={16} /> العودة إلى العقارات
      </button>

      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{property.propertyCode}</h1>
          <div className="flex items-center gap-3 mt-1">
            <StatusBadge label={property.statusName} />
            <span className="text-sm text-slate-500">{property.typeName}</span>
          </div>
        </div>
        <div className="flex gap-2">
          {property.statusName !== 'Verified' && property.statusName !== 'Taxable' && (
            <button
              onClick={() => verifyMutation.mutate()}
              disabled={verifyMutation.isPending}
              className="flex items-center gap-2 px-4 py-2 text-sm bg-green-600 hover:bg-green-700 text-white rounded-lg disabled:opacity-60 transition-colors"
            >
              <CheckCircle size={16} />
              {verifyMutation.isPending ? 'جاري التوثيق…' : 'توثيق العقار'}
            </button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        {/* Property Details */}
        <div className="lg:col-span-2 bg-white rounded-xl border border-slate-200 p-5">
          <h2 className="text-base font-semibold text-slate-900 mb-4">بيانات العقار</h2>
          <dl className="grid grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-4">
            <Detail label="كود العقار" value={property.propertyCode} />
            <Detail label="النوع" value={property.typeName} />
            <Detail label="الحالة" value={property.statusName} />
            <Detail label="مساحة البناء" value={property.builtUpArea ? `${property.builtUpArea.toFixed(1)} م²` : undefined} />
            <Detail label="مساحة الأرض" value={property.landArea ? `${property.landArea.toFixed(1)} م²` : undefined} />
            <Detail label="سنة البناء" value={property.yearBuilt} />
            <Detail label="عنوان الشارع" value={property.streetAddress} />
            <Detail label="الحي" value={property.district} />
            <Detail label="المدينة" value={property.city} />
            <Detail label="المحافظة" value={property.governorate} />
            <Detail label="تاريخ الإنشاء" value={new Date(property.createdAt).toLocaleDateString('ar-EG')} />
          </dl>
        </div>

        {/* Tax Summary */}
        <div className="bg-white rounded-xl border border-slate-200 p-5">
          <h2 className="text-base font-semibold text-slate-900 mb-4">ملخص الضريبة</h2>
          <div className="space-y-4">
            <div className="bg-slate-50 rounded-lg p-4">
              <p className="text-xs text-slate-500">القيمة المقيَّمة</p>
              <p className="text-xl font-bold text-slate-900 mt-1">
                {property.currentAssessedValue
                  ? `${property.currentAssessedValue.toLocaleString()} ج.م`
                  : '—'}
              </p>
            </div>
            <div className="bg-blue-50 rounded-lg p-4">
              <p className="text-xs text-slate-500">الضريبة السنوية</p>
              <p className="text-xl font-bold text-blue-700 mt-1">
                {property.currentTaxAmount
                  ? `${property.currentTaxAmount.toLocaleString()} ج.م`
                  : '—'}
              </p>
            </div>
          </div>

          <div className="mt-5 space-y-2">
            <h3 className="text-sm font-medium text-slate-700">روابط سريعة</h3>
            {[
              { label: 'عرض التقييمات', href: `/valuations?propertyId=${id}` },
              { label: 'عرض الفواتير', href: `/bills?propertyId=${id}` },
              { label: 'عرض الإعفاءات', href: `/exemptions?propertyId=${id}` },
              { label: 'عرض الطعون', href: `/appeals?propertyId=${id}` },
            ].map(({ label, href }) => (
              <a key={href} href={href}
                className="block text-sm text-blue-600 hover:text-blue-800 hover:underline">
                {label} ←
              </a>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
