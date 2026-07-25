/**
 * Reglas de aceptación del archivo de importación, espejo de `ImportFileRules` del backend.
 * Se aplican antes de subir el archivo para avisar de inmediato y evitar cargas inútiles; el
 * servidor las vuelve a aplicar como validación definitiva.
 */
export const OFFICIAL_IMPORT_FILE_NAME = 'ficha_departamental_gobernanza.xlsm';
export const OFFICIAL_IMPORT_EXTENSION = '.xlsm';
export const MAX_IMPORT_FILE_SIZE_BYTES = 10 * 1024 * 1024;

/** Nombre oficial admitiendo sufijos del navegador ("(1)") o del territorio ("_antioquia"). */
const OFFICIAL_NAME_PATTERN = /^ficha[\s_-]*departamental[\s_-]*gobernanza([\s_\-(].*)?$/i;

export interface ImportFileCheck {
  valid: boolean;
  /** Mensaje principal, redactado para el usuario. */
  message?: string;
  /** Acción concreta para corregirlo. */
  hint?: string;
}

function baseName(fileName: string): string {
  const lastDot = fileName.lastIndexOf('.');
  return (lastDot > 0 ? fileName.slice(0, lastDot) : fileName).trim();
}

function extension(fileName: string): string {
  const lastDot = fileName.lastIndexOf('.');
  return lastDot >= 0 ? fileName.slice(lastDot).toLowerCase() : '';
}

/** Valida extensión, nombre y tamaño del archivo seleccionado. */
export function checkImportFile(file: File | null | undefined): ImportFileCheck {
  if (!file) {
    return {
      valid: false,
      message: 'No se seleccionó ningún archivo.',
      hint: `Seleccione el archivo oficial ${OFFICIAL_IMPORT_FILE_NAME} diligenciado.`
    };
  }

  if (file.size === 0) {
    return {
      valid: false,
      message: 'El archivo seleccionado está vacío.',
      hint: 'Verifique que el archivo se haya guardado con la información diligenciada.'
    };
  }

  if (extension(file.name) !== OFFICIAL_IMPORT_EXTENSION) {
    return {
      valid: false,
      message: 'El archivo seleccionado no corresponde al formato oficial de la Ficha Departamental de Gobernanza.',
      hint: `Por favor utilice el archivo oficial ${OFFICIAL_IMPORT_FILE_NAME}. Puede descargarlo con el botón "Descargar plantilla".`
    };
  }

  if (!OFFICIAL_NAME_PATTERN.test(baseName(file.name))) {
    return {
      valid: false,
      message: 'El nombre del archivo no corresponde al de la Ficha Departamental de Gobernanza oficial.',
      hint: `Renombre el archivo como ${OFFICIAL_IMPORT_FILE_NAME} o descargue de nuevo la plantilla oficial.`
    };
  }

  if (file.size > MAX_IMPORT_FILE_SIZE_BYTES) {
    return {
      valid: false,
      message: 'El archivo supera el tamaño máximo permitido (10 MB).',
      hint: 'Elimine imágenes u hojas adicionales agregadas al archivo oficial y vuelva a cargarlo.'
    };
  }

  return { valid: true };
}
