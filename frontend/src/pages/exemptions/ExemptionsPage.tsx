import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import api from '../../lib/api'
import type { Exemption } from '../../lib/types'
import PageHeader from '../../components/PageHeader'
import DataTable from '../../components/DataTable'
import StatusBadge from '../../components/StatusBadge'
import Modal from '../../components/Modal'
import FormField, { Input, Select } from '../../components/FormField'

const exemptionTypes = [
  { value: 0, label: 'LowIncome' },
  { value: 1, label: 'Disabled' },
  { value: 2, label: 'WidowOrphan' },
  { value: 3, label: 'Religious' },
  { value: 4, label: 'Government' },
  { value: 5, label: 'Historical' },
  { value: 6, label: 'Agricultural' },
]

function CreateExemptionModal({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()
  const [form, setForm] = useState({
    propertyId: '', taxpayerId: '', type: '0',
    exemptionPercentage: '', exemptAmount: '',
    startDate: '', endDate: '',
  })
  const [error, setError] = useState('')

  const mutation = useMutation({
    mutationFn: async () => {
      const res = await api.post('/exemptions', {
        propertyId: form.propertyId,
        taxpayerId: form.taxpayerId || undefined,
        type: parseInt(form.type),
        exemptionPercentage: form.exemptionPercentage ? parseFloat(form.exemptionPercentage) : undefined,
        exemptAmount: form.exemptAmount ? parseFloat(form.exemptAmount) : undefined,
        startDate: form.startDate || undefined,
        endDate: form.endDate || undefined,
      })
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['exemptions'] })
      onClose()
    },
    onError: (err: any) => setError(err.response?.data?.error ?? 'Failed to create exemption'),
  })

  return (
    <Modal title="New Exemption" onClose={onClose}>
      <div className="space-y-4">
        {error && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{error}</div>}
        <FormField label="Property ID" required>
          <Input value={form.propertyId} onChange={(e) => setForm({ ...form, propertyId: e.target.value })} placeholder="GUID" />
        </FormField>
        <FormField label="Taxpayer ID">
          <Input value={form.taxpayerId} onChange={(e) => setForm({ ...form, taxpayerId: e.target.value })} placeholder="GUID (optional)" />
        </FormField>
        <FormField label="Type" required>
          <Select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value })}>
            {exemptionTypes.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
          </Select>
        </FormField>
        <div className="grid grid-cols-2 gap-4">
          <FormField label="Exemption % (0–100)">
            <Input type="number" value={form.exemptionPercentage}
              onChange={(e) => setForm({ ...form, exemptionPercentage: e.target.value })} placeholder="50" />
          </FormField>
          <FormField label="Fixed Exempt Amount (EGP)">
            <Input type="number" value={form.exemptAmount}
              onChange={(e) => setForm({ ...form, exemptAmount: e.target.value })} placeholder="Or fixed amount" />
          </FormField>
        </div>
        <div className="grid grid-cols-2 gap-4">
          <FormField label="Start Date">
            <Input type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} />
          </FormField>
          <FormField label="End Date">
            <Input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} />
          </FormField>
        </div>
        <div className="flex gap-3 justify-end pt-2">
          <button onClick={onClose} className="px-4 py-2 text-sm border border-slate-200 rounded-lg text-slate-600 hover:bg-slate-50">Cancel</button>
          <button
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending || !form.propertyId}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
            {mutation.isPending ? 'Submitting…' : 'Submit Exemption'}
          </button>
        </div>
      </div>
    </Modal>
  )
}

export default function ExemptionsPage() {
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [propertyId, setPropertyId] = useState('')
  const [searched, setSearched] = useState('')

  const { data, isLoading } = useQuery<Exemption[]>({
    queryKey: ['exemptions', searched],
    queryFn: async () => {
      if (!searched) return []
      const res = await api.get(`/exemptions/property/${searched}`)
      return Array.isArray(res.data.data) ? res.data.data : []
    },
    enabled: !!searched,
  })

  const approveMutation = useMutation({
    mutationFn: (id: string) => api.post(`/exemptions/${id}/approve`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['exemptions'] }),
  })

  const rejectMutation = useMutation({
    mutationFn: (id: string) => api.post(`/exemptions/${id}/reject`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['exemptions'] }),
  })

  return (
    <div>
      <PageHeader
        title="Exemptions"
        subtitle="Property tax exemption requests"
        action={
          <button onClick={() => setShowCreate(true)}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors">
            New Exemption
          </button>
        }
      />

      <div className="mb-4 flex gap-2">
        <input
          type="text"
          placeholder="Enter Property ID (GUID)…"
          value={propertyId}
          onChange={(e) => setPropertyId(e.target.value)}
          className="border border-slate-300 rounded-lg px-3 py-2 text-sm w-80 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
        />
        <button
          onClick={() => setSearched(propertyId)}
          className="px-4 py-2 text-sm bg-slate-700 hover:bg-slate-800 text-white rounded-lg transition-colors">
          Search
        </button>
      </div>

      <DataTable<Exemption>
        loading={isLoading}
        emptyMessage={searched ? 'No exemptions found for this property' : 'Enter a Property ID to search'}
        columns={[
          { key: 'typeName', header: 'Type' },
          { key: 'statusName', header: 'Status', render: (r) => <StatusBadge label={r.statusName} /> },
          { key: 'exemptionPercentage', header: 'Exemption %', render: (r) => r.exemptionPercentage ? `${r.exemptionPercentage}%` : '—' },
          { key: 'exemptAmount', header: 'Fixed Amount', render: (r) => r.exemptAmount ? `EGP ${r.exemptAmount.toLocaleString()}` : '—' },
          { key: 'startDate', header: 'Start', render: (r) => r.startDate ? new Date(r.startDate).toLocaleDateString('en-GB') : '—' },
          { key: 'endDate', header: 'End', render: (r) => r.endDate ? new Date(r.endDate).toLocaleDateString('en-GB') : '—' },
          {
            key: 'actions', header: 'Actions',
            render: (r) => r.statusName === 'Submitted' ? (
              <div className="flex gap-2">
                <button onClick={(e) => { e.stopPropagation(); approveMutation.mutate(r.id) }}
                  className="text-xs px-2 py-1 bg-green-100 text-green-700 rounded hover:bg-green-200">Approve</button>
                <button onClick={(e) => { e.stopPropagation(); rejectMutation.mutate(r.id) }}
                  className="text-xs px-2 py-1 bg-red-100 text-red-700 rounded hover:bg-red-200">Reject</button>
              </div>
            ) : null
          },
        ]}
        data={data ?? []}
      />

      {showCreate && <CreateExemptionModal onClose={() => setShowCreate(false)} />}
    </div>
  )
}
