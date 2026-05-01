import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowRight, CheckCircle, Trash2 } from 'lucide-react'
import api from '../../lib/api'
import type { Property } from '../../lib/types'
import StatusBadge from '../../components/StatusBadge'
import { getStoredUser } from '../../lib/auth'

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
  const user = getStoredUser()
  const isAdmin = user?.roles?.some(r => r === 'Admin' || r === 'SuperAdmin') ?? false
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const [deleteReason, setDeleteReason] = useState('')

  const { data: property, isLoading, isError, error } = useQuery<Property>({
    queryKey: ['property', id],
    queryFn: async () => {
      const res = await api.get(`/properties/${id}`)
      return res.data.data
    },
    enabled: !!id,
    retry: false,
  })

  const verifyMutation = useMutation({
    mutationFn: async () => {
      await api.post(`/properties/${id}/verify`)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['property', id] }),
  })

  const deleteMutation = useMutation({
    mutationFn: async () => api.delete(`/properties/${id}`, { data: { reason: deleteReason } }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['properties'] })
      navigate('/properties')
    },
  })

  if (isLoading) {
    return <div className="p-8 text-sm text-slate-400">جاري تحميل بيانات العقار…</div>
  }

  if (isError) {
    const status = (error as any)?.response?.status
    return (
      <div className="p-8">
        <button onClick={() => navigate('/properties')}
          className="flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700 mb-4 transition-colors">
          <ArrowRight size={16} /> العودة إلى العقارات
        </button>
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-sm text-red-700">
          {status === 404 ? 'العقار غير موجود أو تم حذفه.' : 'تعذّر تحميل بيانات العقار — يرجى المحاولة مرة أخرى.'}
        </div>
      </div>
    )
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
          {isAdmin && (
            <button
              onClick={() => setShowDeleteDialog(true)}
              className="flex items-center gap-2 px-4 py-2 text-sm bg-red-50 hover:bg-red-100 text-red-700 border border-red-200 rounded-lg transition-colors"
            >
              <Trash2 size={16} /> حذف العقار
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

      {/* Delete confirmation dialog */}
      {showDeleteDialog && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="bg-white rounded-xl shadow-2xl w-full max-w-md p-6 space-y-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-red-100 flex items-center justify-center flex-shrink-0">
                <Trash2 size={18} className="text-red-600" />
              </div>
              <div>
                <h3 className="font-semibold text-slate-900">حذف العقار</h3>
                <p className="text-sm text-slate-500">{property.propertyCode}</p>
              </div>
            </div>

            <div className="bg-amber-50 border border-amber-200 rounded-lg px-3 py-2 text-sm text-amber-800">
              هذا الإجراء لا يُحذف العقار نهائياً — يُخفيه من النظام مع حفظه في سجل المراجعة.
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                سبب الحذف <span className="text-red-500">*</span>
              </label>
              <textarea
                rows={3}
                value={deleteReason}
                onChange={e => setDeleteReason(e.target.value)}
                placeholder="مثال: تم تسجيل العقار مرتين بالخطأ، رمز العقار الصحيح هو PROP-2026-0001234"
                className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-red-400 focus:ring-2 focus:ring-red-100 resize-none"
              />
            </div>

            {deleteMutation.isError && (
              <p className="text-sm text-red-600">فشل الحذف — تحقق من الصلاحيات وحاول مرة أخرى.</p>
            )}

            <div className="flex gap-3 pt-1">
              <button
                onClick={() => deleteMutation.mutate()}
                disabled={!deleteReason.trim() || deleteMutation.isPending}
                className="flex items-center gap-2 px-5 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded-lg disabled:opacity-50 transition-colors"
              >
                <Trash2 size={14} />
                {deleteMutation.isPending ? 'جاري الحذف…' : 'تأكيد الحذف'}
              </button>
              <button
                onClick={() => { setShowDeleteDialog(false); setDeleteReason('') }}
                className="px-4 py-2 text-sm text-slate-600 border border-slate-200 rounded-lg hover:bg-slate-50"
              >
                إلغاء
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
