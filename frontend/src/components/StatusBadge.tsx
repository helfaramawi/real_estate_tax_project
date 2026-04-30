import clsx from 'clsx'

const colorMap: Record<string, string> = {
  draft: 'bg-slate-100 text-slate-700',
  active: 'bg-green-100 text-green-800',
  verified: 'bg-blue-100 text-blue-800',
  taxable: 'bg-indigo-100 text-indigo-800',
  exempt: 'bg-purple-100 text-purple-800',
  archived: 'bg-slate-100 text-slate-500',
  issued: 'bg-blue-100 text-blue-800',
  paid: 'bg-green-100 text-green-800',
  overdue: 'bg-red-100 text-red-800',
  cancelled: 'bg-slate-100 text-slate-500',
  submitted: 'bg-yellow-100 text-yellow-800',
  approved: 'bg-green-100 text-green-800',
  rejected: 'bg-red-100 text-red-800',
  pending: 'bg-yellow-100 text-yellow-800',
  open: 'bg-orange-100 text-orange-800',
  closed: 'bg-slate-100 text-slate-500',
  needsreview: 'bg-orange-100 text-orange-800',
  inprogress: 'bg-blue-100 text-blue-700',
}

export default function StatusBadge({ label }: { label: string }) {
  const key = label?.toLowerCase().replace(/\s/g, '') ?? ''
  return (
    <span
      className={clsx(
        'inline-flex items-center px-2 py-0.5 rounded text-xs font-medium',
        colorMap[key] ?? 'bg-slate-100 text-slate-700'
      )}
    >
      {label}
    </span>
  )
}
