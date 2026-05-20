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
  { value: 0, label: 'دخل محدود' },
  { value: 1, label: 'ذوو إعاقة' },
  { value: 2, label: 'أرامل وأيتام' },
  { value: 3, label: 'جهات دينية' },
  { value: 4, label: 'جهات حكومية' },
  { value: 5, label: 'مباني تاريخية' },
  { value: 6, label: 'أراضٍ زراعية' },
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
    onError: (err: any) => setError(err.response?.data?.error ?? 'فشل إنشاء الإعفاء'),
  })

  return (
    <Modal title="طلب إعفاء جديد" onClose={onClose}>
      <div className="space-y-4">
        {error && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{error}</div>}
        <FormField label="معرف العقار" required>
          <Input value={form.propertyId} onChange={(e) => setForm({ ...form, propertyId: e.target.value })} placeholder="GUID" />
        </FormField>
        <FormField label="معرف المكلف">
          <Input value={form.taxpayerId} onChange={(e) => setForm({ ...form, taxpayerId: e.target.value })} placeholder="GUID (اختياري)" />
        </FormField>
        <FormField label="نوع الإعفاء" required>
          <Select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value })}>
            {exemptionTypes.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
          </Select>
        </FormField>
        <div className="grid grid-cols-2 gap-4">
          <FormField label="نسبة الإعفاء (0–100)">
            <Input type="number" value={form.exemptionPercentage}
              onChange={(e) => setForm({ ...form, exemptionPercentage: e.target.value })} placeholder="50" />
          </FormField>
          <FormField label="مبلغ الإعفاء الثابت (ج.م)">
            <Input type="number" value={form.exemptAmount}
              onChange={(e) => setForm({ ...form, exemptAmount: e.target.value })} placeholder="أو مبلغ ثابت" />
          </FormField>
        </div>
        <div className="grid grid-cols-2 gap-4">
          <FormField label="تاريخ البداية">
            <Input type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} />
          </FormField>
          <FormField label="تاريخ الانتهاء">
            <Input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} />
          </FormField>
        </div>
        <div className="flex gap-3 justify-start pt-2">
          <button
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending || !form.propertyId}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
            {mutation.isPending ? 'جاري التقديم…' : 'تقديم الإعفاء'}
          </button>
          <button onClick={onClose} className="px-4 py-2 text-sm border border-slate-200 rounded-lg text-slate-600 hover:bg-slate-50">إلغاء</button>
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
        title="الإعفاءات"
        subtitle="طلبات الإعفاء من ضريبة العقارات"
        action={
          <button onClick={() => setShowCreate(true)}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors">
            إعفاء جديد
          </button>
        }
      />

      <div className="mb-4 flex gap-2">
        <input
          type="text"
          placeholder="أدخل معرف العقار (GUID)…"
          value={propertyId}
          onChange={(e) => setPropertyId(e.target.value)}
          className="border border-slate-300 rounded-lg px-3 py-2 text-sm w-80 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
        />
        <button
          onClick={() => setSearched(propertyId)}
          className="px-4 py-2 text-sm bg-slate-700 hover:bg-slate-800 text-white rounded-lg transition-colors">
          بحث
        </button>
      </div>

      <DataTable<Exemption>
        loading={isLoading}
        emptyMessage={searched ? 'لا توجد إعفاءات لهذا العقار' : 'أدخل معرف العقار للبحث'}
        columns={[
          { key: 'typeName', header: 'النوع' },
          { key: 'statusName', header: 'الحالة', render: (r) => <StatusBadge label={r.statusName} /> },
          { key: 'exemptionPercentage', header: 'نسبة الإعفاء', render: (r) => r.exemptionPercentage ? `${r.exemptionPercentage}%` : '—' },
          { key: 'exemptAmount', header: 'المبلغ الثابت', render: (r) => r.exemptAmount ? `${r.exemptAmount.toLocaleString()} ج.م` : '—' },
          { key: 'startDate', header: 'البداية', render: (r) => r.startDate ? new Date(r.startDate).toLocaleDateString('ar-EG') : '—' },
          { key: 'endDate', header: 'الانتهاء', render: (r) => r.endDate ? new Date(r.endDate).toLocaleDateString('ar-EG') : '—' },
          {
            key: 'actions', header: 'الإجراءات',
            render: (r) => r.statusName === 'Submitted' ? (
              <div className="flex gap-2">
                <button onClick={(e) => { e.stopPropagation(); approveMutation.mutate(r.id) }}
                  className="text-xs px-2 py-1 bg-green-100 text-green-700 rounded hover:bg-green-200">موافقة</button>
                <button onClick={(e) => { e.stopPropagation(); rejectMutation.mutate(r.id) }}
                  className="text-xs px-2 py-1 bg-red-100 text-red-700 rounded hover:bg-red-200">رفض</button>
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
