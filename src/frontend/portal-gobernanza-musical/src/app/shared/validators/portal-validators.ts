import { AbstractControl, FormGroup, ValidationErrors, ValidatorFn } from '@angular/forms';

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

/** ¿El control tiene algún valor? Una selección múltiple vacía cuenta como sin valor. */
function hasValue(value: unknown): boolean {
  if (Array.isArray(value)) {
    return value.length > 0;
  }

  return value !== null && value !== undefined && value !== '';
}

/**
 * Marca o limpia el error de dependencia sin pisar los errores propios del control. Solo escribe
 * cuando el estado cambia, para que la revalidación del grupo no se realimente.
 */
function applyDependencyError(control: AbstractControl, message: string | null): void {
  const current = control.errors?.['dependencyRequired']?.message ?? null;
  if (current === message) {
    return;
  }

  const errors: ValidationErrors = { ...(control.errors ?? {}) };
  if (message) {
    errors['dependencyRequired'] = { message };
  } else {
    delete errors['dependencyRequired'];
  }

  control.setErrors(Object.keys(errors).length > 0 ? errors : null, { emitEvent: false });
}

/**
 * ¿La fila tiene algo diligenciado? El identificador no cuenta: una fila recién agregada lo trae
 * en blanco igual que el resto.
 */
export function isRowFilled(group: AbstractControl, ignore: readonly string[] = ['id']): boolean {
  if (!(group instanceof FormGroup)) {
    return false;
  }

  return Object.entries(group.controls).some(
    ([name, control]) => !ignore.includes(name) && hasValue(control.value)
  );
}

/**
 * Campo obligatorio solo cuando la fila tiene algún dato. Así, la fila vacía que el formulario
 * muestra por comodidad no bloquea el guardado: simplemente no se envía.
 */
export function requiredWhenRowFilledValidator(field: string, message: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const control = group.get(field);
    if (!control) {
      return null;
    }

    const missing = isRowFilled(group) && !hasValue(control.value);
    applyDependencyError(control, missing ? message : null);
    return null;
  };
}

/**
 * Dos campos que se diligencian en pareja porque uno filtra al otro (Eje PNMC → Componente PNMC,
 * Tipo de agente → Rol en el ecosistema). Si se diligencia uno, el otro pasa a ser obligatorio; si
 * la fila está en blanco no exige nada. Se aplica al grupo de la fila y marca el campo que falta,
 * de modo que se resalte en rojo y aparezca por su nombre en el aviso de la sección.
 */
export function dependentPairValidator(
  parentField: string,
  childField: string,
  messages: { parent: string; child: string }
): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const parent = group.get(parentField);
    const child = group.get(childField);

    if (!parent || !child) {
      return null;
    }

    const parentFilled = hasValue(parent.value);
    const childFilled = hasValue(child.value);

    applyDependencyError(parent, childFilled && !parentFilled ? messages.parent : null);
    applyDependencyError(child, parentFilled && !childFilled ? messages.child : null);

    return null;
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

  // El error de formato de fecha va primero: cuando lo escrito no es una fecha real, el datepicker
  // deja el control vacío y añade también `required`. Avisar de "campo obligatorio" a quien acaba
  // de escribir algo despista; lo que necesita saber es que la fecha no existe.
  if (errors['matDatepickerParse']) {
    return 'Escribe la fecha con el formato dd/mm/aaaa (por ejemplo 15/03/2026) o elígela en el calendario.';
  }
  if (errors['required']) return `${label} es obligatorio.`;
  if (errors['dependencyRequired']) return errors['dependencyRequired'].message as string;
  if (errors['emailFormat'] || errors['email']) return 'Ingresa un correo con el formato usuario@dominio.com.';
  if (errors['onlyDigits']) return 'Escribe solo números, sin espacios, puntos ni guiones.';
  if (errors['mobilePhone']) {
    return `El celular debe tener exactamente ${MOBILE_PHONE_DIGITS} dígitos (recibidos: ${errors['mobilePhone'].actualDigits}).`;
  }
  if (errors['copAmount']) return 'Ingresa un valor en pesos mayor o igual a cero.';
  if (errors['percentage']) return 'Ingresa un porcentaje entre 0 y 100.';
  if (errors['numberFormat']) return 'Ingresa un valor numérico válido.';
  if (errors['urlFormat']) return 'Ingresa un enlace válido que empiece por https://.';
  if (errors['dateFormat']) {
    return 'Escribe la fecha con el formato dd/mm/aaaa (por ejemplo 15/03/2026) o elígela en el calendario.';
  }
  if (errors['dateRange'] || errors['matDatepickerMin'] || errors['matDatepickerMax']) {
    return 'Ingresa una fecha entre el 01/01/2000 y el 31/12/2100.';
  }
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
