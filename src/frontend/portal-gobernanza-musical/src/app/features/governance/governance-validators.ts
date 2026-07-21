import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Réplica de la validación de correo del Excel (Actores!G):
 * `AND(ISNUMBER(SEARCH("@",G)), ISNUMBER(SEARCH(".",G)), LEN(G) >= minLength)`.
 * Un valor vacío es válido (el campo es opcional).
 */
export function excelEmailValidator(minLength: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString().trim();
    if (!value) {
      return null;
    }

    const valid = value.includes('@') && value.includes('.') && value.length >= minLength;
    return valid ? null : { excelEmail: { minLength } };
  };
}

/**
 * Réplica de la validación de longitud de texto del Excel (Actores!F: entre min y max).
 * Un valor vacío es válido (el campo es opcional).
 */
export function textLengthRangeValidator(minLength: number, maxLength: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString().trim();
    if (!value) {
      return null;
    }

    const valid = value.length >= minLength && value.length <= maxLength;
    return valid ? null : { textLengthRange: { minLength, maxLength } };
  };
}
