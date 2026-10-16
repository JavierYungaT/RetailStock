/** Une clases condicionales: cn('a', cond && 'b', undefined) -> "a b" (como clsx, sin dependencia). */
export function cn(...classes: unknown[]) {
  return classes.filter((c): c is string => typeof c === 'string' && c.length > 0).join(' ')
}
