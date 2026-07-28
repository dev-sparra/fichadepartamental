/**
 * Flujo funcional de una importación. Es la misma secuencia que ejecuta el backend
 * (validación de formato → estructura → datos → lote → procesamiento → publicación) y se muestra
 * al usuario para que sepa en qué etapa está y qué falta.
 */
export type ImportStageKey =
  | 'selected'
  | 'format'
  | 'structure'
  | 'data'
  | 'batch'
  | 'processing'
  | 'completed'
  | 'available';

export type ImportStageState = 'pending' | 'active' | 'done' | 'failed' | 'warning';

export interface ImportStage {
  key: ImportStageKey;
  label: string;
  detail: string;
  icon: string;
}

export const IMPORT_FLOW_STAGES: ImportStage[] = [
  {
    key: 'selected',
    label: 'Archivo seleccionado',
    detail: 'Eliges desde tu equipo la ficha diligenciada. El nombre del archivo puede ser cualquiera.',
    icon: 'attach_file'
  },
  {
    key: 'format',
    label: 'Validación del formato',
    detail: 'Se verifica que sea un libro de Excel .xlsm legible y con un tamaño válido.',
    icon: 'fact_check'
  },
  {
    key: 'structure',
    label: 'Validación de la estructura',
    detail:
      'Se comprueba que estén las cinco hojas que se importan, de Identificación a Actores, con todas sus columnas en la posición correcta.',
    icon: 'table_view'
  },
  {
    key: 'data',
    label: 'Validación de los datos',
    detail: 'Cada valor se compara con los listados oficiales del portal (departamentos, ejes, roles, años…).',
    icon: 'rule'
  },
  {
    key: 'batch',
    label: 'Creación del lote',
    detail: 'Queda un registro de la carga en el historial, con fecha, archivo y resultado para consulta posterior.',
    icon: 'inventory_2'
  },
  {
    key: 'processing',
    label: 'Procesamiento',
    detail:
      'La información válida se guarda en la ficha departamental: identificación, diagnóstico, oportunidades, ejes y actores.',
    icon: 'sync'
  },
  {
    key: 'completed',
    label: 'Importación completada',
    detail: 'Se informa el resultado: exitosa, completada con observaciones o rechazada.',
    icon: 'task_alt'
  },
  {
    key: 'available',
    label: 'Datos disponibles en Gobernanza',
    detail: 'La ficha queda visible y editable en el módulo de Gobernanza, lista para revisión del líder.',
    icon: 'account_balance'
  }
];

/** Códigos de incidencia que corresponden a la etapa de validación de formato. */
const FORMAT_CODES = [
  'FILE_EMPTY',
  'FILE_EXTENSION_INVALID',
  'FILE_NAME_INVALID',
  'FILE_TOO_LARGE',
  'FILE_NOT_READABLE'
];

/** Códigos de incidencia que corresponden a la etapa de validación de estructura. */
const STRUCTURE_CODES = ['FILE_SHEET_MISSING', 'FILE_HEADER_MISMATCH', 'FILE_WITHOUT_DATA'];

/** Códigos que ocurren al materializar la ficha (etapa de procesamiento). */
const PROCESSING_CODES = ['IMPORT_EXCEPTION', 'PERSIST_SECTION_ERROR', 'PERSIST_IDENTIFICATION_REQUIRED'];

/** Etapa en la que se detuvo la importación, deducida de los códigos de incidencia. */
export function resolveFailedStage(errorCodes: string[]): ImportStageKey {
  if (errorCodes.some((code) => FORMAT_CODES.includes(code))) return 'format';
  if (errorCodes.some((code) => STRUCTURE_CODES.includes(code))) return 'structure';
  if (errorCodes.some((code) => PROCESSING_CODES.includes(code))) return 'processing';
  return 'data';
}
