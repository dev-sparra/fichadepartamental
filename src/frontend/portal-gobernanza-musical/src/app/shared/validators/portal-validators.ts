import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Validadores de captura por tipo de dato, espejo de `PortalFieldRules` del backend. Se comparten
 * en todo el portal para que un mismo tipo de campo se valide y se explique igual en cualquier
 * formulario. Un valor vacío nunca falla: la obligatoriedad se declara con `Validators.required`.
 */

/** Dígitos exactos de un número de celular colombiano. */
export const MOBILE_PHONE_DIGITS = 10;

const EMAIL_PATTERN = /^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$/;
const DIGITS_PATTERN = /^\d+$/;

function rawValue(control: AbstractControl): string {
  return (control.value ?? '').toString().trim();
}

/** Correo con formato usuario@dominio.com. */
export function emailFormatValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = rawValue(control);
    return !value || EMAIL_PATTERN.test(value) ? null : { emailFormat: true };
  };
}

/** Celular: solo dígitos y exactamente 10. */
export function mobilePhoneValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = rawValue(control);
    if (!value) {
      return null;
    }

    if (!DIGITS_PATTERN.test(value)) {
      return { onlyDigits: true };
    }

    return value.length === MOBILE_PHONE_DIGITS
      ? null
      : { mobilePhone: { requiredDigits: MOBILE_PHONE_DIGITS, actualDigits: value.length } };
  };
}

/** Solo números enteros positivos (sin separadores). */
export function digitsOnlyValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = rawValue(control);
    return !value || DIGITS_PATTERN.test(value) ? null : { onlyDigits: true };
  };
}

/** Valor en pesos colombianos: número mayor o igual a cero. */
export function copAmountValidator(max = 1_000_000_000_000): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const amount = Number(value);
    if (Number.isNaN(amount)) {
      return { numberFormat: true };
    }

    return amount >= 0 && amount <= max ? null : { copAmount: { max } };
  };
}

/** Porcentaje entre 0 y 100. */
export function percentageValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const amount = Number(value);
    if (Number.isNaN(amount)) {
      return { numberFormat: true };
    }

    return amount >= 0 && amount <= 100 ? null : { percentage: true };
  };
}

/** Enlace http/https bien formado. */
export function urlValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = rawValue(control);
    if (!value) {
      return null;
    }

    try {
      const url = new URL(value);
      return url.protocol === 'http:' || url.protocol === 'https:' ? null : { urlFormat: true };
    } catch {
      return { urlFormat: true };
    }
  };
}

/** Fecha dentro del rango de captura permitido (mismo rango que el archivo oficial). */
export function dateRangeValidator(min: Date, max: Date): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }

    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) {
      return { dateFormat: true };
    }

    return date >= min && date <= max ? null : { dateRange: { min, max } };
  };
}

/** Texto obligatorio que no admite solo espacios. */
export function requiredTextValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    return rawValue(control) ? null : { required: true };
  };
}

/**
 * Mensaje de error único y accionable para un control, según el primer error presente.
 * Centralizarlo evita textos distintos para la misma regla en cada formulario.
 */
export function describeFieldError(control: AbstractControl | null, label = 'Este campo'): string | null {
  if (!control || !control.errors || (!control.touched && !control.dirty)) {
    return null;
  }

  const errors = control.errors;

  if (errors['required']) return `${label} es obligatorio.`;
  if (errors['emailFormat'] || errors['email']) return 'Ingresa un correo con el formato usuario@dominio.com.';
  if (errors['onlyDigits']) return 'Escribe solo números, sin espacios, puntos ni guiones.';
  if (errors['mobilePhone']) {
    return `El celular debe tener exactamente ${MOBILE_PHONE_DIGITS} dígitos (recibidos: ${errors['mobilePhone'].actualDigits}).`;
  }
  if (errors['copAmount']) return 'Ingresa un valor en pesos mayor o igual a cero.';
  if (errors['percentage']) return 'Ingresa un porcentaje entre 0 y 100.';
  if (errors['numberFormat']) return 'Ingresa un valor numérico válido.';
  if (errors['urlFormat']) return 'Ingresa un enlace válido que empiece por https://.';
  if (errors['dateFormat']) return 'Ingresa una fecha válida con el formato dd/mm/aaaa.';
  if (errors['dateRange']) return 'Ingresa una fecha entre el 01/01/2000 y el 31/12/2100.';
  if (errors['min']) return `El valor mínimo permitido es ${errors['min'].min}.`;
  if (errors['max']) return `El valor máximo permitido es ${errors['max'].max}.`;
  if (errors['maxlength']) return `Máximo ${errors['maxlength'].requiredLength} caracteres.`;
  if (errors['excelEmail']) return 'Ingresa un correo con el formato usuario@dominio.com.';
  if (errors['textLengthRange']) {
    const { minLength, maxLength } = errors['textLengthRange'];
    return `Debe tener entre ${minLength} y ${maxLength} caracteres.`;
  }

  return 'Revisa el valor ingresado.';
}
