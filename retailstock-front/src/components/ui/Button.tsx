import { LoaderCircle } from 'lucide-react'
import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { cn } from '../../lib/cn'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'soft'
type Size = 'sm' | 'md' | 'lg' | 'icon'

const variants: Record<Variant, string> = {
  primary: 'bg-primary text-white shadow-sm hover:bg-primary-hover disabled:bg-primary/50',
  secondary: 'border border-border bg-surface text-fg shadow-xs hover:bg-surface-hover disabled:text-fg-subtle',
  ghost: 'text-fg-muted hover:bg-surface-hover hover:text-fg disabled:text-fg-subtle',
  danger: 'bg-red-600 text-white shadow-sm hover:bg-red-700 disabled:bg-red-600/50',
  soft: 'bg-primary-soft text-primary-soft-fg hover:bg-primary-soft/70',
}

const sizes: Record<Size, string> = {
  sm: 'h-8 gap-1.5 rounded-md px-2.5 text-[13px]',
  md: 'h-9 gap-2 rounded-lg px-3.5 text-sm',
  lg: 'h-11 gap-2 rounded-lg px-5 text-sm',
  icon: 'size-9 rounded-lg',
}

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  loading?: boolean
  icon?: ReactNode
}

export function Button({ variant = 'primary', size = 'md', loading = false, icon, disabled, className, children, type = 'button', ...props }: ButtonProps) {
  return (
    <button
      type={type}
      {...props}
      disabled={disabled || loading}
      className={cn(
        'inline-flex shrink-0 items-center justify-center font-medium whitespace-nowrap transition-colors select-none',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:cursor-not-allowed',
        variants[variant],
        sizes[size],
        className,
      )}
    >
      {loading ? <LoaderCircle className="size-4 animate-spin" aria-hidden /> : icon}
      {children}
    </button>
  )
}
