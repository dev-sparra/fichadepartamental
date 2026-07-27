/**
 * Reglas de aceptación del archivo de importación, espejo de `ImportFileRules` del backend.
 * Se valida el **formato** (.xlsm) y el tamaño; el **nombre del archivo es libre**, porque en
 * territorio es común renombrarlo sin alterar su contenido. La estructura (hojas y columnas) la
 * verifica el servidor, que es la validación definitiva.
 */
export const OFFICIAL_IMPORT_FILE_NAME = 'ficha_departamental_gobernanza.xlsm';
export const OFFICIAL_IMPORT_EXTENSION = '.xlsm';
export const MAX_IMPORT_FILE_SIZE_BYTES = 10 * 1024 * 1024;

export interface ImportFileCheck {
  valid: boolean;
  /** Mensaje principal, redactado para el usuario. */
  message?: string;
  /** Acción concreta para corregirlo. */
  hint?: string;
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
      hint: `Debe ser un archivo ${OFFICIAL_IMPORT_EXTENSION} diligenciado sobre la plantilla oficial. El nombre del archivo puede ser cualquiera.`
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
