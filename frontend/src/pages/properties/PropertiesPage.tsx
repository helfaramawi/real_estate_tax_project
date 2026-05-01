import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Upload } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import api from '../../lib/api'
import type { Property, PagedResult } from '../../lib/types'
import PageHeader from '../../components/PageHeader'
import DataTable from '../../components/DataTable'
import StatusBadge from '../../components/StatusBadge'
import Modal from '../../components/Modal'
import FormField, { Input, Select } from '../../components/FormField'
import MapLocationPicker from '../../components/MapLocationPicker'
import PropertyImportModal from './PropertyImportModal'

const propertyTypes = [
  { value: 0, label: 'سكني' },
  { value: 1, label: 'تجاري' },
  { value: 2, label: 'صناعي' },
  { value: 3, label: 'زراعي' },
  { value: 4, label: 'متعدد الاستخدامات' },
]

const governorates = [
  { ar: 'القاهرة', en: 'Cairo' },
  { ar: 'الجيزة', en: 'Giza' },
  { ar: 'الإسكندرية', en: 'Alexandria' },
  { ar: 'القليوبية', en: 'Qalyubia' },
  { ar: 'الشرقية', en: 'Sharqia' },
  { ar: 'الدقهلية', en: 'Dakahlia' },
  { ar: 'البحيرة', en: 'Beheira' },
  { ar: 'كفر الشيخ', en: 'Kafr El Sheikh' },
  { ar: 'الغربية', en: 'Gharbia' },
  { ar: 'المنوفية', en: 'Menoufia' },
  { ar: 'الإسماعيلية', en: 'Ismailia' },
  { ar: 'بورسعيد', en: 'Port Said' },
  { ar: 'السويس', en: 'Suez' },
  { ar: 'شمال سيناء', en: 'North Sinai' },
  { ar: 'جنوب سيناء', en: 'South Sinai' },
  { ar: 'مطروح', en: 'Matruh' },
  { ar: 'دمياط', en: 'Damietta' },
  { ar: 'الفيوم', en: 'Faiyum' },
  { ar: 'بني سويف', en: 'Beni Suef' },
  { ar: 'المنيا', en: 'Minya' },
  { ar: 'أسيوط', en: 'Asyut' },
  { ar: 'سوهاج', en: 'Sohag' },
  { ar: 'قنا', en: 'Qena' },
  { ar: 'الأقصر', en: 'Luxor' },
  { ar: 'أسوان', en: 'Aswan' },
  { ar: 'البحر الأحمر', en: 'Red Sea' },
  { ar: 'الوادي الجديد', en: 'New Valley' },
]

export default function PropertiesPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [showImport, setShowImport] = useState(false)
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

  const [location, setLocation] = useState<{ lat: number; lon: number } | null>(null)
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
        latitude: location?.lat,
        longitude: location?.lon,
      }
      const res = await api.post('/properties', payload)
      return res.data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['properties'] })
      setShowCreate(false)
      setLocation(null)
      setForm({ type: '0', builtUpArea: '', landArea: '', yearBuilt: '', streetAddress: '', district: '', city: '', governorate: 'Cairo' })
    },
    onError: (err: any) => {
      const detail = err.response?.data
      if (detail?.errors) setErrors(detail.errors)
      else setErrors({ _: detail?.error ?? 'فشل إنشاء العقار' })
    },
  })

  function validate() {
    const e: Record<string, string> = {}
    if (!form.builtUpArea || isNaN(parseFloat(form.builtUpArea))) e.builtUpArea = 'مطلوب'
    if (!form.governorate) e.governorate = 'مطلوب'
    setErrors(e)
    return Object.keys(e).length === 0
  }

  return (
    <div>
      <PageHeader
        title="العقارات"
        subtitle="إدارة العقارات المسجلة"
        action={
          <div className="flex gap-2">
            <button
              onClick={() => setShowImport(true)}
              className="flex items-center gap-2 bg-white hover:bg-slate-50 text-slate-700 text-sm font-medium px-4 py-2 rounded-lg border border-slate-200 transition-colors"
            >
              <Upload size={16} /> استيراد CSV
            </button>
            <button
              onClick={() => setShowCreate(true)}
              className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
            >
              <Plus size={16} /> عقار جديد
            </button>
          </div>
        }
      />

      <div className="mb-4">
        <input
          type="text"
          placeholder="بحث بالكود أو العنوان أو المدينة…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="border border-slate-300 rounded-lg px-3 py-2 text-sm w-72 focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
        />
      </div>

      <DataTable<Property>
        loading={isLoading}
        columns={[
          { key: 'propertyCode', header: 'الكود' },
          { key: 'typeName', header: 'النوع' },
          { key: 'builtUpArea', header: 'المساحة (م²)', render: (r) => r.builtUpArea?.toFixed(1) },
          { key: 'city', header: 'المدينة' },
          { key: 'governorate', header: 'المحافظة' },
          { key: 'statusName', header: 'الحالة', render: (r) => <StatusBadge label={r.statusName} /> },
          { key: 'currentTaxAmount', header: 'قيمة الضريبة', render: (r) => r.currentTaxAmount ? `${r.currentTaxAmount.toLocaleString()} ج.م` : '—' },
          { key: 'createdAt', header: 'تاريخ الإنشاء', render: (r) => new Date(r.createdAt).toLocaleDateString('ar-EG') },
        ]}
        data={properties}
        onRowClick={(r) => navigate(`/properties/${r.id}`)}
      />

      {showImport && <PropertyImportModal onClose={() => setShowImport(false)} />}

      {showCreate && (
        <Modal title="تسجيل عقار جديد" onClose={() => setShowCreate(false)}>
          <div className="space-y-4">
            {errors._ && <div className="bg-red-50 text-red-700 text-sm rounded px-3 py-2">{errors._}</div>}
            <div className="grid grid-cols-2 gap-4">
              <FormField label="النوع" required>
                <Select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value })}>
                  {propertyTypes.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
                </Select>
              </FormField>
              <FormField label="مساحة البناء (م²)" required error={errors.builtUpArea}>
                <Input type="number" value={form.builtUpArea} error={!!errors.builtUpArea}
                  onChange={(e) => setForm({ ...form, builtUpArea: e.target.value })} placeholder="120.5" />
              </FormField>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <FormField label="مساحة الأرض (م²)">
                <Input type="number" value={form.landArea}
                  onChange={(e) => setForm({ ...form, landArea: e.target.value })} placeholder="اختياري" />
              </FormField>
              <FormField label="سنة البناء">
                <Input type="number" value={form.yearBuilt}
                  onChange={(e) => setForm({ ...form, yearBuilt: e.target.value })} placeholder="2010" />
              </FormField>
            </div>
            <FormField label="عنوان الشارع">
              <Input value={form.streetAddress}
                onChange={(e) => setForm({ ...form, streetAddress: e.target.value })} placeholder="15 كورنيش النيل" />
            </FormField>
            <div className="grid grid-cols-3 gap-4">
              <FormField label="الحي">
                <Input value={form.district}
                  onChange={(e) => setForm({ ...form, district: e.target.value })} placeholder="جاردن سيتي" />
              </FormField>
              <FormField label="المدينة">
                <Input value={form.city}
                  onChange={(e) => setForm({ ...form, city: e.target.value })} placeholder="القاهرة" />
              </FormField>
              <FormField label="المحافظة" required error={errors.governorate}>
                <Select value={form.governorate} error={!!errors.governorate}
                  onChange={(e) => setForm({ ...form, governorate: e.target.value })}>
                  {governorates.map((g) => <option key={g.en} value={g.en}>{g.ar}</option>)}
                </Select>
              </FormField>
            </div>
            <FormField label="الموقع الجغرافي (GPS)">
              <MapLocationPicker value={location} onChange={setLocation} />
            </FormField>
            <div className="flex gap-3 justify-start pt-2">
              <button
                onClick={() => { if (validate()) createMutation.mutate() }}
                disabled={createMutation.isPending}
                className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-60">
                {createMutation.isPending ? 'جاري الإنشاء…' : 'إنشاء عقار'}
              </button>
              <button onClick={() => setShowCreate(false)}
                className="px-4 py-2 text-sm text-slate-600 hover:text-slate-800 border border-slate-200 rounded-lg">
                إلغاء
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
