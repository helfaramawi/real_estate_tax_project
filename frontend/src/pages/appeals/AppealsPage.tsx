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
    onError: (err: any) => setError(err.response?.data?.error ?? 'فشل تقديم الطعن'),
  })

  return (
    <Modal title="تقديم طعن جديد" onClose={onClose}>
      <div className="space-y-4">
        {error && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{error}</div>}
        <FormField label="معرف العقار" required>
          <Input value={form.propertyId} onChange={(e) => setForm({ ...form, propertyId: e.target.value })} placeholder="GUID" />
        </FormField>
        <FormField label="معرف المكلف" required>
          <Input value={form.taxpayerId} onChange={(e) => setForm({ ...form, taxpayerId: e.target.value })} placeholder="GUID" />
        </FormField>
        <FormField label="معرف التقييم (اختياري)">
          <Input value={form.assessmentId} onChange={(e) => setForm({ ...form, assessmentId: e.target.value })} placeholder="GUID" />
        </FormField>
        <FormField label="أسباب الطعن" required>
          <Textarea value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })}
            placeholder="صف أسباب الطعن بالتفصيل…" />
        </FormField>
        <div className="flex gap-3 justify-start pt-2">
          <button
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending || !form.propertyId || !form.reason}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
            {mutation.isPending ? 'جاري التقديم…' : 'تقديم الطعن'}
          </button>
          <button onClick={onClose} className="px-4 py-2 text-sm border border-slate-200 rounded-lg text-slate-600 hover:bg-slate-50">إلغاء</button>
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
        title="الطعون"
        subtitle="طعون المكلفين وقرارات الفصل فيها"
        action={
          <button
            onClick={() => setShowSubmit(true)}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors"
          >
            تقديم طعن
          </button>
        }
      />

      <DataTable<Appeal>
        loading={isLoading}
        columns={[
          { key: 'appealNumber', header: 'الرقم' },
          { key: 'reason', header: 'أسباب الطعن', render: (r) => <span className="max-w-xs truncate block">{r.reason}</span> },
          { key: 'statusName', header: 'الحالة', render: (r) => <StatusBadge label={r.statusName} /> },
          { key: 'submittedAt', header: 'تاريخ التقديم', render: (r) => new Date(r.submittedAt).toLocaleDateString('ar-EG') },
          { key: 'decisionAt', header: 'تاريخ القرار', render: (r) => r.decisionAt ? new Date(r.decisionAt).toLocaleDateString('ar-EG') : '—' },
          { key: 'decisionNotes', header: 'ملاحظات القرار', render: (r) => r.decisionNotes ?? '—' },
        ]}
        data={data ?? []}
      />

      {showSubmit && <SubmitAppealModal onClose={() => setShowSubmit(false)} />}
    </div>
  )
}
