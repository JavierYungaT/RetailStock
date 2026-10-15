import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes } from 'react'

const controlClass =
  'block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm shadow-xs placeholder:text-slate-400 ' +
  'focus:border-brand-500 focus:outline-2 focus:outline-brand-500/30 aria-invalid:border-red-400 disabled:bg-slate-100'

/** Etiqueta + control + error del campo (el error se asocia con aria-describedby). */
export function Field({ label, htmlFor, error, children }: { label: string; htmlFor: string; error?: string; children: ReactNode }) {
  return (
    <div>
      <label htmlFor={htmlFor} className="mb-1 block text-sm font-medium text-slate-700">{label}</label>
      {children}
      {error && <p id={`${htmlFor}-error`} className="mt-1 text-xs text-red-600">{error}</p>}
    </div>
  )
}

export function TextInput({ invalid, ...props }: InputHTMLAttributes<HTMLInputElement> & { invalid?: boolean }) {
  return (
    <input
      {...props}
      aria-invalid={invalid || undefined}
      aria-describedby={invalid ? `${props.id}-error` : undefined}
      className={`${controlClass} ${props.className ?? ''}`}
    />
  )
}

export function SelectInput({ invalid, children, ...props }: SelectHTMLAttributes<HTMLSelectElement> & { invalid?: boolean }) {
  return (
    <select {...props} aria-invalid={invalid || undefined} className={`${controlClass} ${props.className ?? ''}`}>
      {children}
    </select>
  )
}
