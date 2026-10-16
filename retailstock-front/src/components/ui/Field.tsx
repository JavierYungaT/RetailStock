import { forwardRef, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes } from 'react'
import { ChevronDown } from 'lucide-react'
import { cn } from '../../lib/cn'

const controlBase =
  'block w-full rounded-lg border border-border bg-surface text-sm text-fg shadow-xs transition-colors placeholder:text-fg-subtle ' +
  'hover:border-border-strong focus:border-primary focus:outline-none focus:ring-3 focus:ring-ring ' +
  'aria-invalid:border-red-400 aria-invalid:focus:ring-red-500/20 disabled:cursor-not-allowed disabled:bg-surface-muted disabled:text-fg-muted'

/** Etiqueta + control + ayuda/error. El error se asocia al control con aria-describedby. */
export function Field({ label, htmlFor, error, hint, required, children, className }: {
  label: string
  htmlFor: string
  error?: string
  hint?: string
  required?: boolean
  children: ReactNode
  className?: string
}) {
  return (
    <div className={className}>
      <label htmlFor={htmlFor} className="mb-1.5 block text-[13px] font-medium text-fg">
        {label}
        {required && <span className="ml-0.5 text-red-500" aria-hidden>*</span>}
      </label>
      {children}
      {error ? (
        <p id={`${htmlFor}-error`} className="mt-1.5 text-xs text-red-600 dark:text-red-400">{error}</p>
      ) : hint ? (
        <p className="mt-1.5 text-xs text-fg-muted">{hint}</p>
      ) : null}
    </div>
  )
}

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  invalid?: boolean
  leftIcon?: ReactNode
  rightSlot?: ReactNode
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input({ invalid, leftIcon, rightSlot, className, ...props }, ref) {
  return (
    <div className="relative">
      {leftIcon && <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-fg-subtle [&_svg]:size-4">{leftIcon}</span>}
      <input
        ref={ref}
        {...props}
        aria-invalid={invalid || undefined}
        aria-describedby={invalid && props.id ? `${props.id}-error` : undefined}
        className={cn(controlBase, 'h-9 px-3', leftIcon && 'pl-9', rightSlot && 'pr-10', className)}
      />
      {rightSlot && <span className="absolute inset-y-0 right-1.5 flex items-center">{rightSlot}</span>}
    </div>
  )
})

export function Select({ invalid, className, children, ...props }: SelectHTMLAttributes<HTMLSelectElement> & { invalid?: boolean }) {
  return (
    <div className="relative">
      <select
        {...props}
        aria-invalid={invalid || undefined}
        aria-describedby={invalid && props.id ? `${props.id}-error` : undefined}
        className={cn(controlBase, 'h-9 appearance-none pr-9 pl-3', className)}
      >
        {children}
      </select>
      <ChevronDown className="pointer-events-none absolute top-1/2 right-3 size-4 -translate-y-1/2 text-fg-subtle" aria-hidden />
    </div>
  )
}
