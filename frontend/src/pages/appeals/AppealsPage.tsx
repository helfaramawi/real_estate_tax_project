import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import api from '../../lib/api'
import type { Appeal } from '../../lib/types'
import PageHeader from '../../components/PageHeader'
import DataTable from '../../components/DataTable'
import StatusBadge from '../../components/StatusBadge'
import Modal from '../../components/Modal'
import FormField, { Input, Textarea } from '../../components/FormField'

function SubmitAppealModal({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()
  const [form, setForm] = useState({ propertyId: '', taxpayerId: '', reason: '', assessmentId: '' })
  const [error, setError] = useState('')

  const mutation = useMutation({
    mutationFn: async () => {
      const res = await api.post('/appeals', {
        propertyId: form.propertyId,
        taxpayerId: form.taxpayerId,
        reason: form.reason,
        assessmentId: form.assessmentId || undefined,
      })
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['appeals'] })
      onClose()
    },
    onError: (err: any) => setError(err.response?.data?.error ?? 'Failed to submit appeal'),
  })

  return (
    <Modal title="Submit Appeal" onClose={onClose}>
      <div className="space-y-4">
        {error && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{error}</div>}
        <FormField label="Property ID" required>
          <Input value={form.propertyId} onChange={(e) => setForm({ ...form, propertyId: e.target.value })} placeholder="GUID" />
        </FormField>
        <FormField label="Taxpayer ID" required>
          <Input value={form.taxpayerId} onChange={(e) => setForm({ ...form, taxpayerId: e.target.value })} placeholder="GUID" />
        </FormField>
        <FormField label="Assessment ID (optional)">
          <Input value={form.assessmentId} onChange={(e) => setForm({ ...form, assessmentId: e.target.value })} placeholder="GUID" />
        </FormField>
        <FormField label="Reason" required>
          <Textarea value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })}
            placeholder="Describe the grounds for appeal…" />
        </FormField>
        <div className="flex gap-3 justify-end pt-2">
          <button onClick={onClose} className="px-4 py-2 text-sm border border-slate-200 rounded-lg text-slate-600 hover:bg-slate-50">Cancel</button>
          <button
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending || !form.propertyId || !form.reason}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
            {mutation.isPending ? 'Submitting…' : 'Submit Appeal'}
          </button>
        </div>
      </div>
    </Modal>
  )
}

export default function AppealsPage() {
  const [showSubmit, setShowSubmit] = useState(false)

  const { data, isLoading } = useQuery<Appeal[]>({
    queryKey: ['appeals'],
    queryFn: async () => {
      const res = await api.get('/appeals?page=1&pageSize=50')
      return Array.isArray(res.data.data) ? res.data.data : res.data.data?.items ?? []
    },
  })

  return (
    <div>
      <PageHeader
        title="Appeals"
        subtitle="Taxpayer appeal submissions and decisions"
        action={
          <button
            onClick={() => setShowSubmit(true)}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors"
          >
            Submit Appeal
          </button>
        }
      />

      <DataTable<Appeal>
        loading={isLoading}
        columns={[
          { key: 'appealNumber', header: 'Number' },
          { key: 'reason', header: 'Reason', render: (r) => <span className="max-w-xs truncate block">{r.reason}</span> },
          { key: 'statusName', header: 'Status', render: (r) => <StatusBadge label={r.statusName} /> },
          { key: 'submittedAt', header: 'Submitted', render: (r) => new Date(r.submittedAt).toLocaleDateString('en-GB') },
          { key: 'decisionAt', header: 'Decision', render: (r) => r.decisionAt ? new Date(r.decisionAt).toLocaleDateString('en-GB') : '—' },
          { key: 'decisionNotes', header: 'Decision Notes', render: (r) => r.decisionNotes ?? '—' },
        ]}
        data={data ?? []}
      />

      {showSubmit && <SubmitAppealModal onClose={() => setShowSubmit(false)} />}
    </div>
  )
}
