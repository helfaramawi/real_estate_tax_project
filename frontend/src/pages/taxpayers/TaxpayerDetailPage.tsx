import { useParams, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowRight } from 'lucide-react'
import api from '../../lib/api'
import type { Taxpayer } from '../../lib/types'

function Detail({ label, value }: { label: string; value?: string | null }) {
  return (
    <div>
      <dt className="text-xs text-slate-500 font-medium">{label}</dt>
      <dd className="mt-0.5 text-sm text-slate-900">{value ?? '—'}</dd>
    </div>
  )
}

export default function TaxpayerDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const { data: taxpayer, isLoading } = useQuery<Taxpayer>({
    queryKey: ['taxpayer', id],
    queryFn: async () => {
      const res = await api.get(`/taxpayers/${id}`)
      return res.data.data
    },
    enabled: !!id,
  })

  if (isLoading) return <div className="p-8 text-sm text-slate-400">جاري التحميل…</div>
  if (!taxpayer) return <div className="p-8 text-sm text-red-500">المكلف غير موجود.</div>

  const fullName = taxpayer.isCorporate
    ? taxpayer.companyName
    : `${taxpayer.firstName ?? ''} ${taxpayer.lastName ?? ''}`.trim()

  return (
    <div>
      <button onClick={() => navigate('/taxpayers')}
        className="flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700 mb-5 transition-colors">
        <ArrowRight size={16} /> العودة إلى المكلفين
      </button>

      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-slate-900">{fullName}</h1>
        <div className="flex items-center gap-3 mt-1">
          <span className="text-sm bg-slate-100 text-slate-600 px-2 py-0.5 rounded">
            {taxpayer.taxpayerCode}
          </span>
          <span className="text-sm text-slate-500">
            {taxpayer.isCorporate ? 'شركة' : 'فرد'}
          </span>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-slate-200 p-5">
        <h2 className="text-base font-semibold text-slate-900 mb-4">بيانات المكلف</h2>
        <dl className="grid grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-4">
          <Detail label="كود المكلف" value={taxpayer.taxpayerCode} />
          <Detail label="الرقم القومي / رقم السجل" value={taxpayer.nationalId} />
          <Detail label="النوع" value={taxpayer.isCorporate ? 'شركة' : 'فرد'} />
          {taxpayer.isCorporate
            ? <Detail label="اسم الشركة" value={taxpayer.companyName} />
            : <>
              <Detail label="الاسم الأول" value={taxpayer.firstName} />
              <Detail label="الاسم الأخير" value={taxpayer.lastName} />
            </>
          }
          <Detail label="البريد الإلكتروني" value={taxpayer.email} />
          <Detail label="الهاتف" value={taxpayer.phoneNumber} />
          <Detail label="المحافظة" value={taxpayer.governorate} />
          <Detail label="المدينة" value={taxpayer.city} />
          <Detail label="تاريخ التسجيل" value={new Date(taxpayer.createdAt).toLocaleDateString('ar-EG')} />
        </dl>
      </div>
    </div>
  )
}
