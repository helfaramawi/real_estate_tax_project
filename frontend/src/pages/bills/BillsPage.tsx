import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import api from '../../lib/api'
import type { TaxBill } from '../../lib/types'
import PageHeader from '../../components/PageHeader'
import DataTable from '../../components/DataTable'
import StatusBadge from '../../components/StatusBadge'
import Modal from '../../components/Modal'
import FormField, { Input } from '../../components/FormField'

function GenerateBillModal({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()
  const [form, setForm] = useState({ assessmentId: '', taxYear: new Date().getFullYear().toString() })
  const [error, setError] = useState('')

  const mutation = useMutation({
    mutationFn: async () => {
      const res = await api.post('/bills/generate', {
        assessmentId: form.assessmentId,
        taxYear: parseInt(form.taxYear),
      })
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['bills'] })
      onClose()
    },
    onError: (err: any) => {
      setError(err.response?.data?.error ?? 'Failed to generate bill')
    },
  })

  return (
    <Modal title="Generate Tax Bill" onClose={onClose}>
      <div className="space-y-4">
        {error && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{error}</div>}
        <FormField label="Assessment ID" required>
          <Input value={form.assessmentId}
            onChange={(e) => setForm({ ...form, assessmentId: e.target.value })}
            placeholder="GUID of approved assessment" />
        </FormField>
        <FormField label="Tax Year" required>
          <Input type="number" value={form.taxYear}
            onChange={(e) => setForm({ ...form, taxYear: e.target.value })} />
        </FormField>
        <div className="flex gap-3 justify-end pt-2">
          <button onClick={onClose} className="px-4 py-2 text-sm border border-slate-200 rounded-lg text-slate-600 hover:bg-slate-50">Cancel</button>
          <button
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending || !form.assessmentId}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
            {mutation.isPending ? 'Generating…' : 'Generate'}
          </button>
        </div>
      </div>
    </Modal>
  )
}

export default function BillsPage() {
  const [showGenerate, setShowGenerate] = useState(false)
  const [statusFilter, setStatusFilter] = useState('')

  const { data, isLoading } = useQuery<TaxBill[]>({
    queryKey: ['bills', statusFilter],
    queryFn: async () => {
      const params = new URLSearchParams({ page: '1', pageSize: '50' })
      if (statusFilter) params.set('status', statusFilter)
      const res = await api.get(`/bills?${params}`)
      return Array.isArray(res.data.data) ? res.data.data : res.data.data?.items ?? []
    },
  })

  return (
    <div>
      <PageHeader
        title="Tax Bills"
        subtitle="View and manage issued tax bills"
        action={
          <button
            onClick={() => setShowGenerate(true)}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors"
          >
            Generate Bill
          </button>
        }
      />

      <div className="mb-4 flex gap-3">
        {['', 'Issued', 'Paid', 'Overdue', 'Cancelled'].map((s) => (
          <button key={s}
            onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 text-sm rounded-lg border transition-colors ${statusFilter === s ? 'bg-blue-600 text-white border-blue-600' : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50'}`}>
            {s || 'All'}
          </button>
        ))}
      </div>

      <DataTable<TaxBill>
        loading={isLoading}
        columns={[
          { key: 'billNumber', header: 'Bill Number' },
          { key: 'taxYear', header: 'Tax Year' },
          { key: 'totalAmount', header: 'Total', render: (r) => `EGP ${r.totalAmount.toLocaleString()}` },
          { key: 'paidAmount', header: 'Paid', render: (r) => `EGP ${r.paidAmount.toLocaleString()}` },
          { key: 'remainingAmount', header: 'Remaining', render: (r) => `EGP ${r.remainingAmount.toLocaleString()}` },
          { key: 'dueDate', header: 'Due Date', render: (r) => new Date(r.dueDate).toLocaleDateString('en-GB') },
          { key: 'statusName', header: 'Status', render: (r) => <StatusBadge label={r.statusName} /> },
        ]}
        data={data ?? []}
      />

      {showGenerate && <GenerateBillModal onClose={() => setShowGenerate(false)} />}
    </div>
  )
}
