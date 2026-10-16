// Formato de montos y fechas en español de Ecuador (moneda: dólar estadounidense)

const currency = new Intl.NumberFormat('es-EC', { style: 'currency', currency: 'USD' })
const compactCurrency = new Intl.NumberFormat('es-EC', { style: 'currency', currency: 'USD', notation: 'compact', maximumFractionDigits: 1 })
const integer = new Intl.NumberFormat('es-EC')
const shortDate = new Intl.DateTimeFormat('es-EC', { weekday: 'short', day: '2-digit', month: 'short' })
const monthShort = new Intl.DateTimeFormat('es-EC', { month: 'short' })
const longDate = new Intl.DateTimeFormat('es-EC', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
const dateTime = new Intl.DateTimeFormat('es-EC', { dateStyle: 'medium', timeStyle: 'short' })
const timeOnly = new Intl.DateTimeFormat('es-EC', { timeStyle: 'short' })
const relative = new Intl.RelativeTimeFormat('es', { numeric: 'auto' })

export const formatMoney = (value: number) => currency.format(value)
export const formatCompactMoney = (value: number) => compactCurrency.format(value)
export const formatInteger = (value: number) => integer.format(value)

/** "2026-10-15" (día sin hora) -> "jue, 15 oct". Se construye en hora LOCAL para no correr el día. */
export function formatDay(isoDay: string) {
  const [year, month, day] = isoDay.split('-').map(Number)
  return shortDate.format(new Date(year, month - 1, day))
}

/** "2026-10-04" -> "4 oct" (etiquetas de ejes; sin el guion ni el punto que agrega es-EC) */
export function formatDayMonth(isoDay: string) {
  const [year, month, day] = isoDay.split('-').map(Number)
  return `${day} ${monthShort.format(new Date(year, month - 1, day)).replace('.', '')}`
}

export const formatLongDate = (date: Date) => longDate.format(date)

/** Fecha UTC de la API ("...Z") -> hora local del navegador */
export const formatDateTime = (utcIso: string) => dateTime.format(new Date(utcIso))
export const formatTime = (utcIso: string) => timeOnly.format(new Date(utcIso))

/** "hace 5 minutos", "ayer"... */
export function formatRelative(utcIso: string) {
  const seconds = (new Date(utcIso).getTime() - Date.now()) / 1000
  const units: [Intl.RelativeTimeFormatUnit, number][] = [['day', 86400], ['hour', 3600], ['minute', 60]]
  for (const [unit, size] of units) {
    if (Math.abs(seconds) >= size) return relative.format(Math.round(seconds / size), unit)
  }
  return 'hace un momento'
}

/** Fecha local de hoy (o desplazada N días) como "yyyy-MM-dd", el formato que esperan los reportes. */
export function localIsoDate(offsetDays = 0) {
  const date = new Date()
  date.setDate(date.getDate() + offsetDays)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

/** "Buenos días" / "Buenas tardes" / "Buenas noches" según la hora local */
export function greeting(date = new Date()) {
  const hour = date.getHours()
  return hour < 12 ? 'Buenos días' : hour < 19 ? 'Buenas tardes' : 'Buenas noches'
}
