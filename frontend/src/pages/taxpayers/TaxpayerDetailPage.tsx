import { useParams, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'
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

  if (isLoading) return <div className="p-8 text-sm text-slate-400">Loading…</div>
  if (!taxpayer) return <div className="p-8 text-sm text-red-500">Taxpayer not found.</div>

  const fullName = taxpayer.isCorporate
    ? taxpayer.companyName
    : `${taxpayer.firstName ?? ''} ${taxpayer.lastName ?? ''}`.trim()

  return (
    <div>
      <button onClick={() => navigate('/taxpayers')}
        className="flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700 mb-5 transition-colors">
        <ArrowLeft size={16} /> Back to Taxpayers
      </button>

      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-slate-900">{fullName}</h1>
        <div className="flex items-center gap-3 mt-1">
          <span className="text-sm bg-slate-100 text-slate-600 px-2 py-0.5 rounded">
            {taxpayer.taxpayerCode}
          </span>
          <span className="text-sm text-slate-500">
            {taxpayer.isCorporate ? 'Corporate' : 'Individual'}
          </span>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-slate-200 p-5">
        <h2 className="text-base font-semibold text-slate-900 mb-4">Taxpayer Information</h2>
        <dl className="grid grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-4">
          <Detail label="Taxpayer Code" value={taxpayer.taxpayerCode} />
          <Detail label="National ID / Reg. No." value={taxpayer.nationalId} />
          <Detail label="Type" value={taxpayer.isCorporate ? 'Corporate' : 'Individual'} />
          {taxpayer.isCorporate
            ? <Detail label="Company Name" value={taxpayer.companyName} />
            : <>
              <Detail label="First Name" value={taxpayer.firstName} />
              <Detail label="Last Name" value={taxpayer.lastName} />
            </>
          }
          <Detail label="Email" value={taxpayer.email} />
          <Detail label="Phone" value={taxpayer.phoneNumber} />
          <Detail label="Governorate" value={taxpayer.governorate} />
          <Detail label="City" value={taxpayer.city} />
          <Detail label="Registered On" value={new Date(taxpayer.createdAt).toLocaleDateString('en-GB')} />
        </dl>
      </div>
    </div>
  )
}
