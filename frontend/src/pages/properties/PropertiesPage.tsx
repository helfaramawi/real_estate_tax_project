import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import api from '../../lib/api'
import type { Property, PagedResult } from '../../lib/types'
import PageHeader from '../../components/PageHeader'
import DataTable from '../../components/DataTable'
import StatusBadge from '../../components/StatusBadge'
import Modal from '../../components/Modal'
import FormField, { Input, Select } from '../../components/FormField'

const propertyTypes = [
  { value: 0, label: 'Residential' },
  { value: 1, label: 'Commercial' },
  { value: 2, label: 'Industrial' },
  { value: 3, label: 'Agricultural' },
  { value: 4, label: 'Mixed Use' },
]

const governorates = [
  'Cairo', 'Giza', 'Alexandria', 'Qalyubia', 'Sharqia', 'Dakahlia',
  'Beheira', 'Kafr El Sheikh', 'Gharbia', 'Menoufia', 'Ismailia',
  'Port Said', 'Suez', 'North Sinai', 'South Sinai', 'Matruh',
  'Damietta', 'Faiyum', 'Beni Suef', 'Minya', 'Asyut', 'Sohag',
  'Qena', 'Luxor', 'Aswan', 'Red Sea', 'New Valley',
]

export default function PropertiesPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [search, setSearch] = useState('')
  const [page] = useState(1)

  const { data, isLoading } = useQuery<PagedResult<Property>>({
    queryKey: ['properties', page, search],
    queryFn: async () => {
      const params = new URLSearchParams({ page: String(page), pageSize: '20' })
      if (search) params.set('searchTerm', search)
      const res = await api.get(`/properties?${params}`)
      return res.data.data ?? { items: res.data.data ?? [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1 }
    },
  })

  const properties: Property[] = Array.isArray(data)
    ? data
    : (data as any)?.items ?? data ?? []

  const [form, setForm] = useState({
    type: '0', builtUpArea: '', landArea: '', yearBuilt: '',
    streetAddress: '', district: '', city: '', governorate: 'Cairo',
  })
  const [errors, setErrors] = useState<Record<string, string>>({})

  const createMutation = useMutation({
    mutationFn: async () => {
      const payload = {
        type: parseInt(form.type),
        builtUpArea: parseFloat(form.builtUpArea),
        landArea: form.landArea ? parseFloat(form.landArea) : undefined,
        yearBuilt: form.yearBuilt ? parseInt(form.yearBuilt) : undefined,
        streetAddress: form.streetAddress || undefined,
        district: form.district || undefined,
        city: form.city || undefined,
        governorate: form.governorate,
      }
      const res = await api.post('/properties', payload)
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['properties'] })
      setShowCreate(false)
      setForm({ type: '0', builtUpArea: '', landArea: '', yearBuilt: '', streetAddress: '', district: '', city: '', governorate: 'Cairo' })
    },
    onError: (err: any) => {
      const detail = err.response?.data
      if (detail?.errors) setErrors(detail.errors)
      else setErrors({ _: detail?.error ?? 'Failed to create property' })
    },
  })

  function validate() {
    const e: Record<string, string> = {}
    if (!form.builtUpArea || isNaN(parseFloat(form.builtUpArea))) e.builtUpArea = 'Required'
    if (!form.governorate) e.governorate = 'Required'
    setErrors(e)
    return Object.keys(e).length === 0
  }

  return (
    <div>
      <PageHeader
        title="Properties"
        subtitle="Manage registered real estate properties"
        action={
          <button
            onClick={() => setShowCreate(true)}
            className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
          >
            <Plus size={16} /> New Property
          </button>
        }
      />

      <div className="mb-4">
        <input
          type="text"
          placeholder="Search by code, address, city…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="border border-slate-300 rounded-lg px-3 py-2 text-sm w-72 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
        />
      </div>

      <DataTable<Property>
        loading={isLoading}
        columns={[
          { key: 'propertyCode', header: 'Code' },
          { key: 'typeName', header: 'Type' },
          { key: 'builtUpArea', header: 'Area (m²)', render: (r) => r.builtUpArea?.toFixed(1) },
          { key: 'city', header: 'City' },
          { key: 'governorate', header: 'Governorate' },
          { key: 'statusName', header: 'Status', render: (r) => <StatusBadge label={r.statusName} /> },
          { key: 'currentTaxAmount', header: 'Tax Amount', render: (r) => r.currentTaxAmount ? `EGP ${r.currentTaxAmount.toLocaleString()}` : '—' },
          { key: 'createdAt', header: 'Created', render: (r) => new Date(r.createdAt).toLocaleDateString('en-GB') },
        ]}
        data={properties}
        onRowClick={(r) => navigate(`/properties/${r.id}`)}
      />

      {showCreate && (
        <Modal title="Register New Property" onClose={() => setShowCreate(false)}>
          <div className="space-y-4">
            {errors._ && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{errors._}</div>}
            <div className="grid grid-cols-2 gap-4">
              <FormField label="Type" required>
                <Select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value })}>
                  {propertyTypes.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
                </Select>
              </FormField>
              <FormField label="Built-Up Area (m²)" required error={errors.builtUpArea}>
                <Input type="number" value={form.builtUpArea} error={!!errors.builtUpArea}
                  onChange={(e) => setForm({ ...form, builtUpArea: e.target.value })} placeholder="120.5" />
              </FormField>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <FormField label="Land Area (m²)">
                <Input type="number" value={form.landArea}
                  onChange={(e) => setForm({ ...form, landArea: e.target.value })} placeholder="Optional" />
              </FormField>
              <FormField label="Year Built">
                <Input type="number" value={form.yearBuilt}
                  onChange={(e) => setForm({ ...form, yearBuilt: e.target.value })} placeholder="2010" />
              </FormField>
            </div>
            <FormField label="Street Address">
              <Input value={form.streetAddress}
                onChange={(e) => setForm({ ...form, streetAddress: e.target.value })} placeholder="15 Nile Corniche" />
            </FormField>
            <div className="grid grid-cols-3 gap-4">
              <FormField label="District">
                <Input value={form.district}
                  onChange={(e) => setForm({ ...form, district: e.target.value })} placeholder="Garden City" />
              </FormField>
              <FormField label="City">
                <Input value={form.city}
                  onChange={(e) => setForm({ ...form, city: e.target.value })} placeholder="Cairo" />
              </FormField>
              <FormField label="Governorate" required error={errors.governorate}>
                <Select value={form.governorate} error={!!errors.governorate}
                  onChange={(e) => setForm({ ...form, governorate: e.target.value })}>
                  {governorates.map((g) => <option key={g}>{g}</option>)}
                </Select>
              </FormField>
            </div>
            <div className="flex gap-3 justify-end pt-2">
              <button onClick={() => setShowCreate(false)}
                className="px-4 py-2 text-sm text-slate-600 hover:text-slate-800 border border-slate-200 rounded-lg">
                Cancel
              </button>
              <button
                onClick={() => { if (validate()) createMutation.mutate() }}
                disabled={createMutation.isPending}
                className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
                {createMutation.isPending ? 'Creating…' : 'Create Property'}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
