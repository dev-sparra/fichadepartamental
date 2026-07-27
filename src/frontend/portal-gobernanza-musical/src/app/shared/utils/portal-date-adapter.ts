import { Provider } from '@angular/core';
import {
  DateAdapter,
  MAT_DATE_FORMATS,
  MAT_DATE_LOCALE,
  MatDateFormats,
  NativeDateAdapter
} from '@angular/material/core';

/**
 * Adaptador de fechas del portal.
 *
 * El adaptador nativo de Material interpreta el texto con `Date.parse`, que espera el formato
 * anglosajón (mm/dd/aaaa): al escribir "27/07/2026" la fecha quedaba inválida y solo se podía
 * elegir desde el calendario. Aquí se interpreta el formato colombiano dd/mm/aaaa —el mismo del
 * archivo oficial— y se admiten las variantes que la gente escribe en la práctica.
 */

/** Rango de años admitido con año de dos cifras (26 → 2026, 99 → 1999). */
const TWO_DIGIT_YEAR_PIVOT = 50;

/** dd/mm/aaaa con `/`, `-` o `.` como separador; el año puede tener 2 o 4 cifras. */
const SEPARATED_DATE = /^(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{2}|\d{4})$/;

/** ddmmaaaa escrito de corrido, tal como sale de teclear sin separadores. */
const COMPACT_DATE = /^(\d{2})(\d{2})(\d{4})$/;

/** aaaa-mm-dd (ISO), que es como llega la fecha desde la API. */
const ISO_DATE = /^(\d{4})-(\d{2})-(\d{2})$/;

function expandYear(year: number): number {
  if (year >= 100) {
    return year;
  }

  return year < TWO_DIGIT_YEAR_PIVOT ? 2000 + year : 1900 + year;
}

/** Construye la fecha solo si el día y el mes existen de verdad (rechaza 31/02, por ejemplo). */
function buildDate(day: number, month: number, year: number): Date | null {
  if (month < 1 || month > 12 || day < 1 || day > 31) {
    return null;
  }

  const date = new Date(year, month - 1, day);
  date.setHours(0, 0, 0, 0);

  const isRealDate =
    date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;

  return isRealDate ? date : null;
}

function pad(value: number): string {
  return value.toString().padStart(2, '0');
}

export class PortalDateAdapter extends NativeDateAdapter {
  /** Interpreta lo que escribe el usuario; devuelve una fecha inválida si no se reconoce. */
  override parse(value: unknown): Date | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    if (value instanceof Date) {
      return this.clone(value);
    }

    if (typeof value === 'number') {
      return new Date(value);
    }

    const text = value.toString().trim();
    if (!text) {
      return null;
    }

    const iso = ISO_DATE.exec(text);
    if (iso) {
      return buildDate(Number(iso[3]), Number(iso[2]), Number(iso[1])) ?? this.invalid();
    }

    const separated = SEPARATED_DATE.exec(text);
    if (separated) {
      return (
        buildDate(Number(separated[1]), Number(separated[2]), expandYear(Number(separated[3]))) ??
        this.invalid()
      );
    }

    const compact = COMPACT_DATE.exec(text);
    if (compact) {
      return (
        buildDate(Number(compact[1]), Number(compact[2]), Number(compact[3])) ?? this.invalid()
      );
    }

    // Texto no reconocido: se marca inválido para que el campo muestre el error de formato
    // en lugar de dejar el valor anterior sin avisar.
    return this.invalid();
  }

  /** El campo siempre muestra dd/mm/aaaa, que es lo que el usuario puede volver a escribir. */
  override format(date: Date, displayFormat: unknown): string {
    if (!this.isValid(date)) {
      return '';
    }

    if (displayFormat === 'monthYear') {
      return super.format(date, { year: 'numeric', month: 'short' });
    }

    return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`;
  }
}

/** Formatos ligados al adaptador: entrada y etiquetas del calendario en español. */
export const PORTAL_DATE_FORMATS: MatDateFormats = {
  parse: {
    dateInput: 'dd/MM/yyyy'
  },
  display: {
    dateInput: 'dd/MM/yyyy',
    monthYearLabel: 'monthYear',
    dateA11yLabel: { year: 'numeric', month: 'long', day: 'numeric' },
    monthYearA11yLabel: { year: 'numeric', month: 'long' }
  }
};

/** Selector de fecha del portal: escribible en dd/mm/aaaa y con calendario en español. */
export function providePortalDateAdapter(): Provider[] {
  return [
    { provide: MAT_DATE_LOCALE, useValue: 'es-CO' },
    { provide: DateAdapter, useClass: PortalDateAdapter, deps: [MAT_DATE_LOCALE] },
    { provide: MAT_DATE_FORMATS, useValue: PORTAL_DATE_FORMATS }
  ];
}
