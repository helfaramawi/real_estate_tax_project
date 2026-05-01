import { useState, useRef } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Upload, Download, CheckCircle, XCircle, AlertTriangle } from 'lucide-react'
import api from '../../lib/api'

// ── CSV columns (order matters — matches template) ────────────────────────────
const COLUMNS = [
  { key: 'type',            label: 'نوع_العقار',         required: true,  hint: '0=سكني 1=تجاري 2=صناعي 3=زراعي 4=متعدد' },
  { key: 'builtUpArea',     label: 'مساحة_البناء_م2',    required: true,  hint: 'رقم عشري' },
  { key: 'landArea',        label: 'مساحة_الأرض_م2',     required: false, hint: 'رقم عشري أو فارغ' },
  { key: 'numberOfFloors',  label: 'عدد_الطوابق',        required: false, hint: 'رقم صحيح أو فارغ' },
  { key: 'yearBuilt',       label: 'سنة_البناء',         required: false, hint: 'مثال: 2005' },
  { key: 'buildingNumber',  label: 'رقم_المبنى',         required: false, hint: 'نص' },
  { key: 'streetAddress',   label: 'الشارع',             required: false, hint: 'نص' },
  { key: 'district',        label: 'الحي',               required: false, hint: 'نص' },
  { key: 'city',            label: 'المدينة',            required: false, hint: 'نص' },
  { key: 'governorate',     label: 'المحافظة',           required: true,  hint: 'مثال: Cairo' },
  { key: 'latitude',        label: 'خط_العرض',           required: true,  hint: 'مثال: 30.044444' },
  { key: 'longitude',       label: 'خط_الطول',           required: true,  hint: 'مثال: 31.235753' },
  { key: 'parcelNumber',    label: 'رقم_القطعة',         required: false, hint: 'نص' },
  { key: 'cadastralRef',    label: 'الرقم_العقاري',      required: false, hint: 'نص' },
]

type ParsedRow = Record<string, string>

type ImportResult = {
  succeeded: number
  failed: number
  errors: { row: number; message: string }[]
}

function downloadTemplate() {
  const header = COLUMNS.map(c => c.label).join(',')
  const example = [
    '0', '120.5', '200', '3', '2005',
    '15', 'كورنيش النيل', 'جاردن سيتي', 'القاهرة', 'Cairo',
    '30.044444', '31.235753', 'P-001', 'CAD-001',
  ].join(',')
  const bom = '﻿' // UTF-8 BOM for Excel
  const blob = new Blob([bom + header + '\n' + example], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'قالب_استيراد_العقارات.csv'
  a.click()
  URL.revokeObjectURL(url)
}

function parseCsv(text: string): ParsedRow[] {
  const lines = text.replace(/\r/g, '').split('\n').filter(l => l.trim())
  if (lines.length < 2) return []
  const keys = COLUMNS.map(c => c.key)
  return lines.slice(1).map(line => {
    // Handle quoted fields
    const values: string[] = []
    let cur = '', inQ = false
    for (const ch of line) {
      if (ch === '"') { inQ = !inQ }
      else if (ch === ',' && !inQ) { values.push(cur.trim()); cur = '' }
      else cur += ch
    }
    values.push(cur.trim())
    return Object.fromEntries(keys.map((k, i) => [k, values[i] ?? '']))
  })
}

function validateRow(row: ParsedRow, idx: number): string | null {
  if (!row.builtUpArea || isNaN(parseFloat(row.builtUpArea)))
    return `صف ${idx + 1}: مساحة البناء مطلوبة ويجب أن تكون رقماً`
  if (!row.governorate)
    return `صف ${idx + 1}: المحافظة مطلوبة`
  if (!row.latitude || isNaN(parseFloat(row.latitude)))
    return `صف ${idx + 1}: خط العرض (Latitude) مطلوب ويجب أن يكون رقماً`
  if (!row.longitude || isNaN(parseFloat(row.longitude)))
    return `صف ${idx + 1}: خط الطول (Longitude) مطلوب ويجب أن يكون رقماً`
  const lat = parseFloat(row.latitude)
  const lon = parseFloat(row.longitude)
  if (lat < 22 || lat > 31.5 || lon < 25 || lon > 37)
    return `صف ${idx + 1}: الإحداثيات خارج نطاق مصر الجغرافي`
  return null
}

function toApiPayload(row: ParsedRow) {
  return {
    type: parseInt(row.type) || 0,
    builtUpArea: parseFloat(row.builtUpArea),
    landArea: row.landArea ? parseFloat(row.landArea) : undefined,
    numberOfFloors: row.numberOfFloors ? parseInt(row.numberOfFloors) : undefined,
    yearBuilt: row.yearBuilt ? parseInt(row.yearBuilt) : undefined,
    buildingNumber: row.buildingNumber || undefined,
    streetAddress: row.streetAddress || undefined,
    district: row.district || undefined,
    city: row.city || undefined,
    governorate: row.governorate,
    latitude: parseFloat(row.latitude),
    longitude: parseFloat(row.longitude),
    parcelNumber: row.parcelNumber || undefined,
    cadastralReference: row.cadastralRef || undefined,
  }
}

type Props = { onClose: () => void }

export default function PropertyImportModal({ onClose }: Props) {
  const qc = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)
  const [rows, setRows] = useState<ParsedRow[]>([])
  const [parseErrors, setParseErrors] = useState<string[]>([])
  const [result, setResult] = useState<ImportResult | null>(null)

  function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = (ev) => {
      const text = ev.target?.result as string
      const parsed = parseCsv(text)
      const errs = parsed.map((r, i) => validateRow(r, i)).filter(Boolean) as string[]
      setParseErrors(errs)
      setRows(parsed)
      setResult(null)
    }
    reader.readAsText(file, 'UTF-8')
  }

  const importMutation = useMutation({
    mutationFn: () => api.post('/properties/bulk-import', rows.map(toApiPayload)).then(r => r.data.data as ImportResult),
    onSuccess: (data) => {
      setResult(data)
      qc.invalidateQueries({ queryKey: ['properties'] })
    },
  })

  const canImport = rows.length > 0 && parseErrors.length === 0 && !result

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-4xl flex flex-col gap-4 p-6 max-h-[90vh] overflow-y-auto">

        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold text-slate-800">استيراد عقارات من ملف CSV</h2>
            <p className="text-xs text-slate-500 mt-0.5">يجب أن يحتوي الملف على الإحداثيات الجغرافية — الحد الأقصى 500 عقار</p>
          </div>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-700 text-xl">✕</button>
        </div>

        {/* Step 1: Download template */}
        <div className="bg-blue-50 border border-blue-100 rounded-lg p-4 flex items-center justify-between">
          <div>
            <p className="text-sm font-medium text-blue-800">الخطوة 1 — تحميل القالب</p>
            <p className="text-xs text-blue-600 mt-0.5">افتح الملف في Excel أو أي محرر CSV وأدخل بيانات العقارات</p>
          </div>
          <button
            onClick={downloadTemplate}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm rounded-lg transition-colors"
          >
            <Download size={15} /> تحميل القالب
          </button>
        </div>

        {/* Step 2: Upload file */}
        <div className="border-2 border-dashed border-slate-200 rounded-lg p-6 text-center hover:border-blue-300 transition-colors cursor-pointer"
          onClick={() => fileRef.current?.click()}>
          <Upload size={24} className="mx-auto text-slate-400 mb-2" />
          <p className="text-sm font-medium text-slate-600">الخطوة 2 — رفع الملف المعبأ</p>
          <p className="text-xs text-slate-400 mt-1">اضغط هنا أو اسحب ملف CSV</p>
          <input ref={fileRef} type="file" accept=".csv" className="hidden" onChange={handleFile} />
        </div>

        {/* Validation errors */}
        {parseErrors.length > 0 && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-3">
            <p className="text-sm font-medium text-red-700 mb-2 flex items-center gap-1">
              <AlertTriangle size={14} /> أخطاء في الملف ({parseErrors.length})
            </p>
            <ul className="text-xs text-red-600 space-y-1 max-h-32 overflow-y-auto">
              {parseErrors.map((e, i) => <li key={i}>• {e}</li>)}
            </ul>
          </div>
        )}

        {/* Preview table */}
        {rows.length > 0 && parseErrors.length === 0 && !result && (
          <div>
            <p className="text-sm font-medium text-slate-700 mb-2">
              معاينة — {rows.length} عقار جاهز للاستيراد
            </p>
            <div className="overflow-x-auto border border-slate-200 rounded-lg max-h-56">
              <table className="text-xs w-full">
                <thead className="bg-slate-50 sticky top-0">
                  <tr>
                    <th className="px-2 py-1.5 text-right text-slate-500">#</th>
                    <th className="px-2 py-1.5 text-right text-slate-500">النوع</th>
                    <th className="px-2 py-1.5 text-right text-slate-500">المساحة م²</th>
                    <th className="px-2 py-1.5 text-right text-slate-500">المحافظة</th>
                    <th className="px-2 py-1.5 text-right text-slate-500">خط العرض</th>
                    <th className="px-2 py-1.5 text-right text-slate-500">خط الطول</th>
                    <th className="px-2 py-1.5 text-right text-slate-500">الشارع</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {rows.slice(0, 50).map((r, i) => (
                    <tr key={i} className="hover:bg-slate-50">
                      <td className="px-2 py-1 text-slate-400">{i + 1}</td>
                      <td className="px-2 py-1">{['سكني','تجاري','صناعي','زراعي','متعدد'][parseInt(r.type)] ?? r.type}</td>
                      <td className="px-2 py-1">{r.builtUpArea}</td>
                      <td className="px-2 py-1">{r.governorate}</td>
                      <td className="px-2 py-1 font-mono">{parseFloat(r.latitude).toFixed(5)}</td>
                      <td className="px-2 py-1 font-mono">{parseFloat(r.longitude).toFixed(5)}</td>
                      <td className="px-2 py-1 text-slate-500 truncate max-w-[120px]">{r.streetAddress}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {rows.length > 50 && (
                <p className="text-xs text-slate-400 text-center py-2">تعرض أول 50 صف من {rows.length}</p>
              )}
            </div>
          </div>
        )}

        {/* Result */}
        {result && (
          <div className="rounded-lg border p-4 space-y-3">
            <div className="flex gap-6">
              <div className="flex items-center gap-2 text-green-700">
                <CheckCircle size={18} />
                <span className="text-sm font-semibold">{result.succeeded} تم بنجاح</span>
              </div>
              {result.failed > 0 && (
                <div className="flex items-center gap-2 text-red-600">
                  <XCircle size={18} />
                  <span className="text-sm font-semibold">{result.failed} فشل</span>
                </div>
              )}
            </div>
            {result.errors.length > 0 && (
              <ul className="text-xs text-red-600 space-y-1 max-h-32 overflow-y-auto bg-red-50 rounded p-2">
                {result.errors.map((e, i) => <li key={i}>• صف {e.row}: {e.message}</li>)}
              </ul>
            )}
          </div>
        )}

        {/* Actions */}
        <div className="flex gap-3 pt-1 border-t border-slate-100">
          {!result ? (
            <button
              onClick={() => importMutation.mutate()}
              disabled={!canImport || importMutation.isPending}
              className="flex items-center gap-2 px-5 py-2 bg-green-600 hover:bg-green-700 text-white text-sm rounded-lg disabled:opacity-50 transition-colors"
            >
              <Upload size={15} />
              {importMutation.isPending ? `جاري الاستيراد…` : `استيراد ${rows.length} عقار`}
            </button>
          ) : (
            <button
              onClick={() => { setRows([]); setResult(null); setParseErrors([]); if (fileRef.current) fileRef.current.value = '' }}
              className="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm rounded-lg"
            >
              استيراد ملف آخر
            </button>
          )}
          <button onClick={onClose} className="px-4 py-2 text-sm text-slate-600 border border-slate-200 rounded-lg hover:bg-slate-50">
            إغلاق
          </button>
        </div>
      </div>
    </div>
  )
}
