import { useQuery } from '@tanstack/react-query'
import {
  Building2, Users, FileText, DollarSign,
  AlertTriangle, Scale, ShieldCheck, TrendingUp,
} from 'lucide-react'
import api from '../lib/api'
import type { DashboardKpis } from '../lib/types'

function KpiCard({
  label, value, icon: Icon, color
}: {
  label: string
  value?: number | string
  icon: React.ElementType
  color: string
}) {
  return (
    <div className="bg-white rounded-xl border border-slate-200 p-5">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-slate-500 font-medium">{label}</p>
          <p className="text-2xl font-bold text-slate-900 mt-1">
            {value === undefined ? '—' : value.toLocaleString()}
          </p>
        </div>
        <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${color}`}>
          <Icon size={20} className="text-white" />
        </div>
      </div>
    </div>
  )
}

function fmt(n?: number) {
  if (n === undefined) return '—'
  if (n >= 1_000_000) return `EGP ${(n / 1_000_000).toFixed(1)}M`
  if (n >= 1_000) return `EGP ${(n / 1_000).toFixed(0)}K`
  return `EGP ${n.toFixed(0)}`
}

export default function DashboardPage() {
  const { data, isLoading } = useQuery<DashboardKpis>({
    queryKey: ['dashboard-kpis'],
    queryFn: async () => {
      const res = await api.get('/admin/dashboard/kpis')
      return res.data.data
    },
    retry: false,
  })

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-slate-900">Dashboard</h1>
        <p className="text-sm text-slate-500 mt-1">Real Estate Tax Administration Overview</p>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="bg-white rounded-xl border border-slate-200 p-5 h-24 animate-pulse" />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <KpiCard label="Total Properties" value={data?.totalProperties} icon={Building2} color="bg-blue-600" />
          <KpiCard label="Taxpayers" value={data?.totalTaxpayers} icon={Users} color="bg-indigo-600" />
          <KpiCard label="Bills Issued" value={data?.totalBillsIssued} icon={FileText} color="bg-violet-600" />
          <KpiCard label="Total Collected" value={fmt(data?.totalCollected)} icon={DollarSign} color="bg-green-600" />
          <KpiCard label="Overdue Amount" value={fmt(data?.totalOverdue)} icon={AlertTriangle} color="bg-red-500" />
          <KpiCard label="Pending Appeals" value={data?.pendingAppeals} icon={Scale} color="bg-orange-500" />
          <KpiCard label="Pending Exemptions" value={data?.pendingExemptions} icon={ShieldCheck} color="bg-amber-500" />
          <KpiCard label="High Risk Properties" value={data?.highRiskProperties} icon={TrendingUp} color="bg-rose-600" />
        </div>
      )}

      {!isLoading && !data && (
        <div className="mt-4 text-center text-sm text-slate-400 bg-white rounded-xl border border-slate-200 py-10">
          Dashboard KPIs unavailable — ensure the backend is running and you have Admin access.
        </div>
      )}

      <div className="mt-8 bg-white rounded-xl border border-slate-200 p-5">
        <h2 className="text-base font-semibold text-slate-900 mb-3">Quick Actions</h2>
        <div className="flex flex-wrap gap-3">
          {[
            { label: 'Register Property', to: '/properties/new' },
            { label: 'Add Taxpayer', to: '/taxpayers/new' },
            { label: 'Generate Bill', to: '/bills' },
            { label: 'Record Payment', to: '/payments' },
          ].map(({ label, to }) => (
            <a
              key={to}
              href={to}
              className="px-4 py-2 rounded-lg border border-slate-200 text-sm font-medium text-slate-700 hover:bg-slate-50 transition-colors"
            >
              {label}
            </a>
          ))}
        </div>
      </div>
    </div>
  )
}
