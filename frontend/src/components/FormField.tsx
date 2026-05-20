interface Props {
  label: string
  error?: string
  required?: boolean
  children: React.ReactNode
}

export default function FormField({ label, error, required, children }: Props) {
  return (
    <div>
      <label className="block text-sm font-medium text-slate-700 mb-1">
        {label}{required && <span className="text-red-500 ml-0.5">*</span>}
      </label>
      {children}
      {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
    </div>
  )
}

export function Input(props: React.InputHTMLAttributes<HTMLInputElement> & { error?: boolean }) {
  const { error, className, ...rest } = props
  return (
    <input
      {...rest}
      className={`w-full border rounded-lg px-3 py-2 text-sm outline-none transition-colors
        ${error
          ? 'border-red-300 focus:border-red-500 focus:ring-2 focus:ring-red-100'
          : 'border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100'
        } ${className ?? ''}`}
    />
  )
}

export function Select(props: React.SelectHTMLAttributes<HTMLSelectElement> & { error?: boolean }) {
  const { error, className, ...rest } = props
  return (
    <select
      {...rest}
      className={`w-full border rounded-lg px-3 py-2 text-sm outline-none transition-colors bg-white
        ${error
          ? 'border-red-300 focus:border-red-500 focus:ring-2 focus:ring-red-100'
          : 'border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100'
        } ${className ?? ''}`}
    />
  )
}

export function Textarea(props: React.TextareaHTMLAttributes<HTMLTextAreaElement> & { error?: boolean }) {
  const { error, className, ...rest } = props
  return (
    <textarea
      {...rest}
      rows={3}
      className={`w-full border rounded-lg px-3 py-2 text-sm outline-none transition-colors resize-y
        ${error
          ? 'border-red-300 focus:border-red-500 focus:ring-2 focus:ring-red-100'
          : 'border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100'
        } ${className ?? ''}`}
    />
  )
}
