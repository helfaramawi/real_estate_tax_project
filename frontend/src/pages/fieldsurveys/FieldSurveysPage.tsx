import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import api from '../../lib/api'
import PageHeader from '../../components/PageHeader'
import DataTable from '../../components/DataTable'
import StatusBadge from '../../components/StatusBadge'
import Modal from '../../components/Modal'
import FormField, { Input, Textarea } from '../../components/FormField'

interface FieldSurvey {
  id: string
  propertyId: string
  assignedToId?: string
  scheduledDate?: string
  completedDate?: string
  status: number
  statusName: string
  notes?: string
  createdAt: string
}

function CreateSurveyModal({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()
  const [form, setForm] = useState({ propertyId: '', assignedToId: '', scheduledDate: '', notes: '' })
  const [error, setError] = useState('')

  const mutation = useMutation({
    mutationFn: async () => {
      const res = await api.post('/field-surveys', {
        propertyId: form.propertyId,
        assignedToId: form.assignedToId || undefined,
        scheduledDate: form.scheduledDate || undefined,
        notes: form.notes || undefined,
      })
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['field-surveys'] })
      onClose()
    },
    onError: (err: any) => setError(err.response?.data?.error ?? 'Failed to create survey'),
  })

  return (
    <Modal title="Assign Field Survey" onClose={onClose}>
      <div className="space-y-4">
        {error && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{error}</div>}
        <FormField label="Property ID" required>
          <Input value={form.propertyId} onChange={(e) => setForm({ ...form, propertyId: e.target.value })} placeholder="GUID" />
        </FormField>
        <FormField label="Assign To (Inspector User ID)">
          <Input value={form.assignedToId} onChange={(e) => setForm({ ...form, assignedToId: e.target.value })} placeholder="GUID (optional)" />
        </FormField>
        <FormField label="Scheduled Date">
          <Input type="date" value={form.scheduledDate} onChange={(e) => setForm({ ...form, scheduledDate: e.target.value })} />
        </FormField>
        <FormField label="Notes">
          <Textarea value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
        </FormField>
        <div className="flex gap-3 justify-end pt-2">
          <button onClick={onClose} className="px-4 py-2 text-sm border border-slate-200 rounded-lg text-slate-600 hover:bg-slate-50">Cancel</button>
          <button
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending || !form.propertyId}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
            {mutation.isPending ? 'Assigning…' : 'Assign Survey'}
          </button>
        </div>
      </div>
    </Modal>
  )
}

export default function FieldSurveysPage() {
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [statusFilter, setStatusFilter] = useState('')

  const { data, isLoading } = useQuery<FieldSurvey[]>({
    queryKey: ['field-surveys', statusFilter],
    queryFn: async () => {
      const params = new URLSearchParams({ page: '1', pageSize: '50' })
      if (statusFilter) params.set('status', statusFilter)
      const res = await api.get(`/field-surveys?${params}`)
      return Array.isArray(res.data.data) ? res.data.data : res.data.data?.items ?? []
    },
  })

  const submitMutation = useMutation({
    mutationFn: (id: string) => api.post(`/field-surveys/${id}/submit`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['field-surveys'] }),
  })

  return (
    <div>
      <PageHeader
        title="Field Surveys"
        subtitle="Property inspection and survey assignments"
        action={
          <button onClick={() => setShowCreate(true)}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors">
            Assign Survey
          </button>
        }
      />

      <div className="mb-4 flex gap-3 flex-wrap">
        {['', 'Assigned', 'InProgress', 'Submitted', 'Approved'].map((s) => (
          <button key={s}
            onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 text-sm rounded-lg border transition-colors ${statusFilter === s ? 'bg-blue-600 text-white border-blue-600' : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50'}`}>
            {s || 'All'}
          </button>
        ))}
      </div>

      <DataTable<FieldSurvey>
        loading={isLoading}
        columns={[
          { key: 'propertyId', header: 'Property ID', render: (r) => <span className="font-mono text-xs">{r.propertyId.slice(0, 8)}…</span> },
          { key: 'scheduledDate', header: 'Scheduled', render: (r) => r.scheduledDate ? new Date(r.scheduledDate).toLocaleDateString('en-GB') : '—' },
          { key: 'completedDate', header: 'Completed', render: (r) => r.completedDate ? new Date(r.completedDate).toLocaleDateString('en-GB') : '—' },
          { key: 'statusName', header: 'Status', render: (r) => <StatusBadge label={r.statusName} /> },
          { key: 'notes', header: 'Notes', render: (r) => <span className="max-w-xs truncate block text-slate-500">{r.notes ?? '—'}</span> },
          {
            key: 'actions', header: '',
            render: (r) => r.statusName === 'InProgress' ? (
              <button onClick={(e) => { e.stopPropagation(); submitMutation.mutate(r.id) }}
                className="text-xs px-2 py-1 bg-blue-100 text-blue-700 rounded hover:bg-blue-200">
                Submit
              </button>
            ) : null
          },
        ]}
        data={data ?? []}
      />

      {showCreate && <CreateSurveyModal onClose={() => setShowCreate(false)} />}
    </div>
  )
}
