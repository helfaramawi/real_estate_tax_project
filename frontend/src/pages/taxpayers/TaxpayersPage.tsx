import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import api from '../../lib/api'
import type { Taxpayer, PagedResult } from '../../lib/types'
import PageHeader from '../../components/PageHeader'
import DataTable from '../../components/DataTable'
import Modal from '../../components/Modal'
import FormField, { Input, Select } from '../../components/FormField'

const governorates = [
  'Cairo', 'Giza', 'Alexandria', 'Qalyubia', 'Sharqia', 'Dakahlia',
  'Beheira', 'Kafr El Sheikh', 'Gharbia', 'Menoufia', 'Ismailia',
  'Port Said', 'Suez', 'Damietta', 'Faiyum', 'Beni Suef',
  'Minya', 'Asyut', 'Sohag', 'Qena', 'Luxor', 'Aswan',
]

export default function TaxpayersPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [search, setSearch] = useState('')

  const { data, isLoading } = useQuery<PagedResult<Taxpayer>>({
    queryKey: ['taxpayers', search],
    queryFn: async () => {
      const params = new URLSearchParams({ page: '1', pageSize: '20' })
      if (search) params.set('searchTerm', search)
      const res = await api.get(`/taxpayers?${params}`)
      return res.data.data
    },
  })

  const taxpayers: Taxpayer[] = Array.isArray(data)
    ? data
    : (data as any)?.items ?? []

  const [isCorporate, setIsCorporate] = useState(false)
  const [form, setForm] = useState({
    nationalId: '', firstName: '', lastName: '', companyName: '',
    email: '', phoneNumber: '', governorate: 'Cairo', city: '',
  })
  const [errors, setErrors] = useState<Record<string, string>>({})

  const createMutation = useMutation({
    mutationFn: async () => {
      const payload = {
        nationalId: form.nationalId,
        firstName: !isCorporate ? form.firstName : undefined,
        lastName: !isCorporate ? form.lastName : undefined,
        companyName: isCorporate ? form.companyName : undefined,
        isCorporate,
        email: form.email || undefined,
        phoneNumber: form.phoneNumber || undefined,
        governorate: form.governorate || undefined,
        city: form.city || undefined,
      }
      const res = await api.post('/taxpayers', payload)
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['taxpayers'] })
      setShowCreate(false)
    },
    onError: (err: any) => {
      const detail = err.response?.data
      setErrors({ _: detail?.error ?? JSON.stringify(detail?.errors) ?? 'Failed to create taxpayer' })
    },
  })

  function validate() {
    const e: Record<string, string> = {}
    if (!form.nationalId || form.nationalId.length !== 14) e.nationalId = 'Must be exactly 14 digits'
    if (!isCorporate && !form.firstName) e.firstName = 'Required for individuals'
    if (!isCorporate && !form.lastName) e.lastName = 'Required for individuals'
    if (isCorporate && !form.companyName) e.companyName = 'Required for corporate'
    setErrors(e)
    return Object.keys(e).length === 0
  }

  return (
    <div>
      <PageHeader
        title="Taxpayers"
        subtitle="Registered individual and corporate taxpayers"
        action={
          <button
            onClick={() => setShowCreate(true)}
            className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
          >
            <Plus size={16} /> New Taxpayer
          </button>
        }
      />

      <div className="mb-4">
        <input
          type="text"
          placeholder="Search by name, NID, code…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="border border-slate-300 rounded-lg px-3 py-2 text-sm w-72 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
        />
      </div>

      <DataTable<Taxpayer>
        loading={isLoading}
        columns={[
          { key: 'taxpayerCode', header: 'Code' },
          { key: 'nationalId', header: 'National ID / Reg No.' },
          {
            key: 'name', header: 'Name',
            render: (r) => r.isCorporate ? r.companyName ?? '—' : `${r.firstName ?? ''} ${r.lastName ?? ''}`.trim() || '—'
          },
          { key: 'isCorporate', header: 'Type', render: (r) => r.isCorporate ? 'Corporate' : 'Individual' },
          { key: 'governorate', header: 'Governorate' },
          { key: 'phoneNumber', header: 'Phone' },
          { key: 'createdAt', header: 'Created', render: (r) => new Date(r.createdAt).toLocaleDateString('en-GB') },
        ]}
        data={taxpayers}
        onRowClick={(r) => navigate(`/taxpayers/${r.id}`)}
      />

      {showCreate && (
        <Modal title="Register Taxpayer" onClose={() => setShowCreate(false)}>
          <div className="space-y-4">
            {errors._ && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{errors._}</div>}

            <div className="flex gap-4">
              {['Individual', 'Corporate'].map((label, i) => (
                <label key={label} className="flex items-center gap-2 cursor-pointer">
                  <input type="radio" name="type" checked={isCorporate === !!i}
                    onChange={() => setIsCorporate(!!i)} />
                  <span className="text-sm text-slate-700">{label}</span>
                </label>
              ))}
            </div>

            <FormField label="National ID / Reg. Number" required error={errors.nationalId}>
              <Input value={form.nationalId} error={!!errors.nationalId} maxLength={14}
                onChange={(e) => setForm({ ...form, nationalId: e.target.value })} placeholder="14-digit number" />
            </FormField>

            {!isCorporate ? (
              <div className="grid grid-cols-2 gap-4">
                <FormField label="First Name" required error={errors.firstName}>
                  <Input value={form.firstName} error={!!errors.firstName}
                    onChange={(e) => setForm({ ...form, firstName: e.target.value })} />
                </FormField>
                <FormField label="Last Name" required error={errors.lastName}>
                  <Input value={form.lastName} error={!!errors.lastName}
                    onChange={(e) => setForm({ ...form, lastName: e.target.value })} />
                </FormField>
              </div>
            ) : (
              <FormField label="Company Name" required error={errors.companyName}>
                <Input value={form.companyName} error={!!errors.companyName}
                  onChange={(e) => setForm({ ...form, companyName: e.target.value })} />
              </FormField>
            )}

            <div className="grid grid-cols-2 gap-4">
              <FormField label="Email">
                <Input type="email" value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })} />
              </FormField>
              <FormField label="Phone">
                <Input value={form.phoneNumber}
                  onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} placeholder="01012345678" />
              </FormField>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <FormField label="Governorate">
                <Select value={form.governorate}
                  onChange={(e) => setForm({ ...form, governorate: e.target.value })}>
                  {governorates.map((g) => <option key={g}>{g}</option>)}
                </Select>
              </FormField>
              <FormField label="City">
                <Input value={form.city}
                  onChange={(e) => setForm({ ...form, city: e.target.value })} />
              </FormField>
            </div>

            <div className="flex gap-3 justify-end pt-2">
              <button onClick={() => setShowCreate(false)}
                className="px-4 py-2 text-sm text-slate-600 border border-slate-200 rounded-lg hover:bg-slate-50">
                Cancel
              </button>
              <button
                onClick={() => { if (validate()) createMutation.mutate() }}
                disabled={createMutation.isPending}
                className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
                {createMutation.isPending ? 'Creating…' : 'Create Taxpayer'}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
