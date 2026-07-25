/**
 * Conversión entre las fechas del API (`DateOnly` en formato `aaaa-mm-dd`) y los objetos `Date`
 * que usa el selector de fecha de Material. Se trabaja siempre en hora local para que la fecha
 * capturada no se desplace un día por la zona horaria.
 */

/** `2026-03-15` → `Date` local (sin desplazamiento por zona horaria). */
export function parseIsoDate(value: string | null | undefined): Date | null {
  if (!value) {
    return null;
  }

  const [datePart] = value.split('T');
  const [year, month, day] = datePart.split('-').map(Number);
  if (!year || !month || !day) {
    return null;
  }

  return new Date(year, month - 1, day);
}

/** `Date` (o cadena) → `aaaa-mm-dd`, el formato que espera el API. */
export function toIsoDate(value: Date | string | null | undefined): string {
  if (!value) {
    return '';
  }

  const date = value instanceof Date ? value : parseIsoDate(value);
  if (!date || Number.isNaN(date.getTime())) {
    return '';
  }

  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

/** Formato moneda colombiana para mostrar valores capturados en pesos. */
export function formatCop(value: number | string | null | undefined): string {
  if (value === null || value === undefined || value === '') {
    return '';
  }

  const amount = Number(value);
  if (Number.isNaN(amount)) {
    return '';
  }

  return new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP',
    maximumFractionDigits: 0
  }).format(amount);
}
