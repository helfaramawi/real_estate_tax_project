import { useState, useCallback } from 'react'
import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet'
import { MapPin } from 'lucide-react'
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
}

export default function MapLocationPicker({ value, onChange }: Props) {
  const [open, setOpen] = useState(false)

  const handlePick = useCallback((ll: LatLon) => {
    onChange(ll)
    setOpen(false)
  }, [onChange])

  return (
    <div>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="flex items-center gap-2 px-3 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors w-full"
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

      {open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="bg-white rounded-xl shadow-2xl w-full max-w-2xl flex flex-col gap-3 p-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold text-slate-800">حدد موقع العقار على الخريطة</h3>
              <button onClick={() => setOpen(false)} className="text-slate-400 hover:text-slate-700 text-lg">✕</button>
            </div>
            <p className="text-xs text-slate-500">انقر على الخريطة لتثبيت الموقع</p>
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
