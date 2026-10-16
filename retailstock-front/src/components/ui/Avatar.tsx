import { cn } from '../../lib/cn'

const palette = [
  'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-200',
  'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-200',
  'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-200',
  'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-200',
  'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-200',
  'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-200',
]

/** Iniciales con color estable por nombre (el mismo usuario siempre tiene el mismo color). */
export function Avatar({ name, className }: { name: string; className?: string }) {
  const initials = name.split(/[\s@._-]+/).filter(Boolean).slice(0, 2).map((p) => p[0]!.toUpperCase()).join('')
  const hash = [...name].reduce((acc, c) => acc + c.charCodeAt(0), 0)
  return (
    <span aria-hidden className={cn('inline-flex size-8 shrink-0 items-center justify-center rounded-full text-xs font-semibold', palette[hash % palette.length], className)}>
      {initials || '?'}
    </span>
  )
}
