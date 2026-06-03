import { useState, useCallback } from 'react'
import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet'
import { Crosshair, MapPin } from 'lucide-react'
import 'leaflet/dist/leaflet.css'
import L from 'leaflet'

// Fix default marker icon broken by webpack/vite
import markerIconPng from 'leaflet/dist/images/marker-icon.png'
import markerShadowPng from 'leaflet/dist/images/marker-shadow.png'

const defaultIcon = L.icon({
  iconUrl: markerIconPng,
  shadowUrl: markerShadowPng,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
})

type LatLon = { lat: number; lon: number }

function ClickHandler({ onPick }: { onPick: (ll: LatLon) => void }) {
  useMapEvents({ click: (e) => onPick({ lat: e.latlng.lat, lon: e.latlng.lng }) })
  return null
}

type Props = {
  value: LatLon | null
  onChange: (ll: LatLon | null) => void
  error?: boolean
}

export default function MapLocationPicker({ value, onChange, error }: Props) {
  const [open, setOpen] = useState(false)
  const [gpsError, setGpsError] = useState<string | null>(null)
  const [locating, setLocating] = useState(false)

  const handlePick = useCallback((ll: LatLon) => {
    onChange(ll)
    setGpsError(null)
    setOpen(false)
  }, [onChange])

  const useCurrentGps = useCallback(() => {
    if (!navigator.geolocation) {
      setGpsError('المتصفح لا يدعم تحديد الموقع عبر GPS.')
      return
    }

    setLocating(true)
    setGpsError(null)
    navigator.geolocation.getCurrentPosition(
      (position) => {
        onChange({ lat: position.coords.latitude, lon: position.coords.longitude })
        setLocating(false)
        setOpen(true)
      },
      () => {
        setGpsError('تعذر قراءة موقع الجهاز. اسمح بالوصول للموقع أو اختر النقطة من الخريطة.')
        setLocating(false)
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 },
    )
  }, [onChange])

  return (
    <div>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className={`flex items-center gap-2 px-3 py-2 text-sm border rounded-lg hover:bg-slate-50 transition-colors w-full ${error ? 'border-red-300 ring-2 ring-red-100' : 'border-slate-300'}`}
      >
        <MapPin size={15} className={value ? 'text-blue-600' : 'text-slate-400'} />
        {value
          ? <span className="text-slate-700 font-mono text-xs">{value.lat.toFixed(6)}, {value.lon.toFixed(6)}</span>
          : <span className="text-slate-400">انقر لتحديد الموقع على الخريطة</span>
        }
        {value && (
          <button
            type="button"
            onClick={(e) => { e.stopPropagation(); onChange(null) }}
            className="ms-auto text-slate-400 hover:text-red-500 text-xs"
          >✕</button>
        )}
      </button>
      <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-slate-500">
        <button
          type="button"
          onClick={useCurrentGps}
          disabled={locating}
          className="inline-flex items-center gap-1 rounded-md border border-blue-200 bg-blue-50 px-2 py-1 text-blue-700 hover:bg-blue-100 disabled:opacity-60"
        >
          <Crosshair size={13} /> {locating ? 'جاري قراءة GPS…' : 'استخدام موقع الجهاز GPS'}
        </button>
        <span>أو اضغط على الخريطة لتثبيت موقع العقار بدقة.</span>
      </div>
      {gpsError && <p className="mt-1 text-xs text-amber-700">{gpsError}</p>}

      {open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="bg-white rounded-xl shadow-2xl w-full max-w-2xl flex flex-col gap-3 p-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold text-slate-800">حدد موقع العقار على الخريطة</h3>
              <button onClick={() => setOpen(false)} className="text-slate-400 hover:text-slate-700 text-lg">✕</button>
            </div>
            <div className="flex items-center justify-between gap-3">
              <p className="text-xs text-slate-500">انقر على الخريطة لتثبيت الموقع، أو استخدم GPS من زر موقع الجهاز.</p>
              <button
                type="button"
                onClick={useCurrentGps}
                disabled={locating}
                className="inline-flex items-center gap-1 rounded-md border border-blue-200 bg-blue-50 px-2 py-1 text-xs text-blue-700 hover:bg-blue-100 disabled:opacity-60"
              >
                <Crosshair size={13} /> {locating ? 'جاري قراءة GPS…' : 'GPS'}
              </button>
            </div>
            <div className="rounded-lg overflow-hidden border border-slate-200" style={{ height: 420 }}>
              <MapContainer
                center={value ? [value.lat, value.lon] : [26.8206, 30.8025]}
                zoom={value ? 15 : 6}
                style={{ height: '100%', width: '100%' }}
              >
                <TileLayer
                  url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                  attribution='© <a href="https://openstreetmap.org">OpenStreetMap</a>'
                />
                <ClickHandler onPick={handlePick} />
                {value && (
                  <Marker position={[value.lat, value.lon]} icon={defaultIcon} />
                )}
              </MapContainer>
            </div>
            {value && (
              <div className="text-xs text-slate-500 text-center">
                الإحداثيات: <span className="font-mono">{value.lat.toFixed(6)}, {value.lon.toFixed(6)}</span>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
