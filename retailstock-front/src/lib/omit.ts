/** Copia del objeto sin la clave indicada (para quitar el error de un campo al corregirlo). */
export function omit<T extends Record<string, unknown>>(object: T, key: string): T {
  const copy = { ...object }
  delete copy[key]
  return copy
}
