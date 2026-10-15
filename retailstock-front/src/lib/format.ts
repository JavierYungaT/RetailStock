// Formato de montos y fechas en español de Ecuador (moneda: dólar estadounidense)

const currency = new Intl.NumberFormat('es-EC', { style: 'currency', currency: 'USD' })
const integer = new Intl.NumberFormat('es-EC')
const shortDate = new Intl.DateTimeFormat('es-EC', { weekday: 'short', day: '2-digit', month: 'short' })
const dateTime = new Intl.DateTimeFormat('es-EC', { dateStyle: 'medium', timeStyle: 'short' })

export const formatMoney = (value: number) => currency.format(value)
export const formatInteger = (value: number) => integer.format(value)

/** "2026-10-15" (día sin hora) -> "jue, 15 oct". Se construye en hora LOCAL para no correr el día. */
export function formatDay(isoDay: string) {
  const [year, month, day] = isoDay.split('-').map(Number)
  return shortDate.format(new Date(year, month - 1, day))
}

/** Fecha UTC de la API ("...Z") -> hora local del navegador */
export const formatDateTime = (utcIso: string) => dateTime.format(new Date(utcIso))

/** Fecha local de hoy (o desplazada N días) como "yyyy-MM-dd", el formato que esperan los reportes. */
export function localIsoDate(offsetDays = 0) {
  const date = new Date()
  date.setDate(date.getDate() + offsetDays)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}
