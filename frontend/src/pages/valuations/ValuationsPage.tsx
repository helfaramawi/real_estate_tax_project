import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import api from '../../lib/api'
import type { Valuation } from '../../lib/types'
import PageHeader from '../../components/PageHeader'
import DataTable from '../../components/DataTable'
import StatusBadge from '../../components/StatusBadge'
import Modal from '../../components/Modal'
import FormField, { Input, Select } from '../../components/FormField'

const methods = [
  { value: 0, label: 'Rental Value' },
  { value: 1, label: 'Market Comparison' },
  { value: 2, label: 'Cost Approach' },
]

function CreateValuationModal({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()
  const [form, setForm] = useState({
    propertyId: '', method: '0', grossValue: '',
    deductionRate: '30', taxYear: new Date().getFullYear().toString(),
  })
  const [error, setError] = useState('')

  const mutation = useMutation({
    mutationFn: async () => {
      const res = await api.post('/valuations', {
        propertyId: form.propertyId,
        method: parseInt(form.method),
        grossValue: parseFloat(form.grossValue),
        deductionRate: parseFloat(form.deductionRate),
        taxYear: parseInt(form.taxYear),
      })
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['valuations'] })
      onClose()
    },
    onError: (err: any) => setError(err.response?.data?.error ?? 'Failed to create valuation'),
  })

  return (
    <Modal title="New Valuation" onClose={onClose}>
      <div className="space-y-4">
        {error && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{error}</div>}
        <FormField label="Property ID" required>
          <Input value={form.propertyId} onChange={(e) => setForm({ ...form, propertyId: e.target.value })} placeholder="GUID" />
        </FormField>
        <div className="grid grid-cols-2 gap-4">
          <FormField label="Valuation Method" required>
            <Select value={form.method} onChange={(e) => setForm({ ...form, method: e.target.value })}>
              {methods.map((m) => <option key={m.value} value={m.value}>{m.label}</option>)}
            </Select>
          </FormField>
          <FormField label="Tax Year" required>
            <Input type="number" value={form.taxYear} onChange={(e) => setForm({ ...form, taxYear: e.target.value })} />
          </FormField>
        </div>
        <div className="grid grid-cols-2 gap-4">
          <FormField label="Gross Value (EGP)" required>
            <Input type="number" value={form.grossValue} onChange={(e) => setForm({ ...form, grossValue: e.target.value })} />
          </FormField>
          <FormField label="Deduction Rate %">
            <Input type="number" value={form.deductionRate} onChange={(e) => setForm({ ...form, deductionRate: e.target.value })} />
          </FormField>
        </div>
        <div className="flex gap-3 justify-end pt-2">
          <button onClick={onClose} className="px-4 py-2 text-sm border border-slate-200 rounded-lg text-slate-600 hover:bg-slate-50">Cancel</button>
          <button
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending || !form.propertyId || !form.grossValue}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
            {mutation.isPending ? 'Saving…' : 'Create Valuation'}
          </button>
        </div>
      </div>
    </Modal>
  )
}

export default function ValuationsPage() {
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [propertyId, setPropertyId] = useState('')
  const [searched, setSearched] = useState('')

  const { data, isLoading } = useQuery<Valuation[]>({
    queryKey: ['valuations', searched],
    queryFn: async () => {
      if (!searched) return []
      const res = await api.get(`/valuations/property/${searched}`)
      return Array.isArray(res.data.data) ? res.data.data : []
    },
    enabled: !!searched,
  })

  const approveMutation = useMutation({
    mutationFn: (id: string) => api.post(`/valuations/${id}/approve`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['valuations'] }),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      api.post(`/valuations/${id}/reject`, { reason }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['valuations'] }),
  })

  return (
    <div>
      <PageHeader
        title="Valuations"
        subtitle="Property valuations and assessments"
        action={
          <button onClick={() => setShowCreate(true)}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors">
            New Valuation
          </button>
        }
      />

      <div className="mb-4 flex gap-2">
        <input type="text" placeholder="Enter Property ID (GUID)…"
          value={propertyId} onChange={(e) => setPropertyId(e.target.value)}
          className="border border-slate-300 rounded-lg px-3 py-2 text-sm w-80 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100" />
        <button onClick={() => setSearched(propertyId)}
          className="px-4 py-2 text-sm bg-slate-700 hover:bg-slate-800 text-white rounded-lg transition-colors">
          Search
        </button>
      </div>

      <DataTable<Valuation>
        loading={isLoading}
        emptyMessage={searched ? 'No valuations for this property' : 'Enter a Property ID to search'}
        columns={[
          { key: 'methodName', header: 'Method' },
          { key: 'taxYear', header: 'Year' },
          { key: 'grossValue', header: 'Gross Value', render: (r) => `EGP ${r.grossValue.toLocaleString()}` },
          { key: 'deductionRate', header: 'Deduction', render: (r) => `${r.deductionRate}%` },
          { key: 'netValue', header: 'Net Value', render: (r) => `EGP ${r.netValue.toLocaleString()}` },
          { key: 'statusName', header: 'Status', render: (r) => <StatusBadge label={r.statusName} /> },
          { key: 'valuedAt', header: 'Dated', render: (r) => new Date(r.valuedAt).toLocaleDateString('en-GB') },
          {
            key: 'actions', header: 'Actions',
            render: (r) => r.statusName === 'Submitted' ? (
              <div className="flex gap-2">
                <button onClick={(e) => { e.stopPropagation(); approveMutation.mutate(r.id) }}
                  className="text-xs px-2 py-1 bg-green-100 text-green-700 rounded hover:bg-green-200">Approve</button>
                <button onClick={(e) => { e.stopPropagation(); rejectMutation.mutate({ id: r.id, reason: 'Rejected by reviewer' }) }}
                  className="text-xs px-2 py-1 bg-red-100 text-red-700 rounded hover:bg-red-200">Reject</button>
              </div>
            ) : null
          },
        ]}
        data={data ?? []}
      />

      {showCreate && <CreateValuationModal onClose={() => setShowCreate(false)} />}
    </div>
  )
}
