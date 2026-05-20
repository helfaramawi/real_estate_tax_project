import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import api from '../../lib/api'
import type { Payment } from '../../lib/types'
import PageHeader from '../../components/PageHeader'
import DataTable from '../../components/DataTable'
import Modal from '../../components/Modal'
import FormField, { Input, Select } from '../../components/FormField'

const paymentMethods = [
  { value: 0, label: 'نقدي' },
  { value: 1, label: 'تحويل بنكي' },
  { value: 2, label: 'إلكتروني' },
  { value: 3, label: 'محفظة إلكترونية' },
  { value: 4, label: 'شيك' },
]

function RegisterPaymentModal({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()
  const [form, setForm] = useState({
    billId: '', amount: '', method: '0', reference: '', paidAt: new Date().toISOString().slice(0, 10),
  })
  const [error, setError] = useState('')

  const mutation = useMutation({
    mutationFn: async () => {
      const res = await api.post('/payments', {
        billId: form.billId,
        amount: parseFloat(form.amount),
        method: parseInt(form.method),
        reference: form.reference || undefined,
        paidAt: new Date(form.paidAt).toISOString(),
      })
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['payments'] })
      onClose()
    },
    onError: (err: any) => setError(err.response?.data?.error ?? 'فشل تسجيل الدفعة'),
  })

  return (
    <Modal title="تسجيل دفعة جديدة" onClose={onClose}>
      <div className="space-y-4">
        {error && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{error}</div>}
        <FormField label="معرف الفاتورة" required>
          <Input value={form.billId} onChange={(e) => setForm({ ...form, billId: e.target.value })} placeholder="GUID" />
        </FormField>
        <div className="grid grid-cols-2 gap-4">
          <FormField label="المبلغ (ج.م)" required>
            <Input type="number" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} />
          </FormField>
          <FormField label="طريقة الدفع" required>
            <Select value={form.method} onChange={(e) => setForm({ ...form, method: e.target.value })}>
              {paymentMethods.map((m) => <option key={m.value} value={m.value}>{m.label}</option>)}
            </Select>
          </FormField>
        </div>
        <div className="grid grid-cols-2 gap-4">
          <FormField label="رقم المرجع">
            <Input value={form.reference} onChange={(e) => setForm({ ...form, reference: e.target.value })} placeholder="إيصال / مرجع" />
          </FormField>
          <FormField label="تاريخ الدفع" required>
            <Input type="date" value={form.paidAt} onChange={(e) => setForm({ ...form, paidAt: e.target.value })} />
          </FormField>
        </div>
        <div className="flex gap-3 justify-start pt-2">
          <button
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending || !form.billId || !form.amount}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
            {mutation.isPending ? 'جاري التسجيل…' : 'تسجيل الدفعة'}
          </button>
          <button onClick={onClose} className="px-4 py-2 text-sm border border-slate-200 rounded-lg text-slate-600 hover:bg-slate-50">إلغاء</button>
        </div>
      </div>
    </Modal>
  )
}

export default function PaymentsPage() {
  const [showRegister, setShowRegister] = useState(false)
  const [billId, setBillId] = useState('')
  const [searched, setSearched] = useState('')

  const { data, isLoading } = useQuery<Payment[]>({
    queryKey: ['payments', searched],
    queryFn: async () => {
      if (!searched) return []
      const res = await api.get(`/payments/bill/${searched}`)
      return Array.isArray(res.data.data) ? res.data.data : []
    },
    enabled: !!searched,
  })

  return (
    <div>
      <PageHeader
        title="المدفوعات"
        subtitle="سجلات مدفوعات الفواتير الضريبية"
        action={
          <button onClick={() => setShowRegister(true)}
            className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors">
            تسجيل دفعة
          </button>
        }
      />

      <div className="mb-4 flex gap-2">
        <input type="text" placeholder="أدخل معرف الفاتورة (GUID)…"
          value={billId} onChange={(e) => setBillId(e.target.value)}
          className="border border-slate-300 rounded-lg px-3 py-2 text-sm w-80 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100" />
        <button onClick={() => setSearched(billId)}
          className="px-4 py-2 text-sm bg-slate-700 hover:bg-slate-800 text-white rounded-lg transition-colors">
          بحث
        </button>
      </div>

      <DataTable<Payment>
        loading={isLoading}
        emptyMessage={searched ? 'لا توجد مدفوعات لهذه الفاتورة' : 'أدخل معرف الفاتورة للبحث'}
        columns={[
          { key: 'amount', header: 'المبلغ', render: (r) => `${r.amount.toLocaleString()} ج.م` },
          { key: 'methodName', header: 'طريقة الدفع' },
          { key: 'reference', header: 'المرجع', render: (r) => r.reference ?? '—' },
          { key: 'paidAt', header: 'تاريخ الدفع', render: (r) => new Date(r.paidAt).toLocaleDateString('ar-EG') },
        ]}
        data={data ?? []}
      />

      {showRegister && <RegisterPaymentModal onClose={() => setShowRegister(false)} />}
    </div>
  )
}
