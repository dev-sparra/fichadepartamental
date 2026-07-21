// Contrato del Blueprint de la Ficha Departamental de Gobernanza.
// Refleja PortalNacionalGobernanzaMusical.Application.Governance.Blueprint.FichaBlueprint.
// Es la fuente única de verdad para construir el formulario dinámico de /governance.

export type BlueprintFieldType =
  | 'text'
  | 'date'
  | 'integer'
  | 'decimal'
  | 'list'
  | 'dependentList'
  | 'calculated'
  | 'fixed';

export type BlueprintSheetKind = 'singleRow' | 'collection' | 'fixedCatalog';

export type BlueprintCascade =
  | 'municipalitiesByDepartment'
  | 'componentsByAxis'
  | 'rolesByAgentType';

export interface BlueprintValidation {
  required: boolean;
  min: number | null;
  max: number | null;
  minLength: number | null;
  maxLength: number | null;
  dateMin: string | null;
  dateMax: string | null;
  excelFormula: string | null;
  message: string | null;
}

export interface BlueprintField {
  column: string;
  columnIndex: number;
  key: string;
  label: string;
  type: BlueprintFieldType;
  editable: boolean;
  multiSelect: boolean;
  multiSeparator: string | null;
  multiRange: string | null;
  catalog: string | null;
  excelRange: string | null;
  inlineOptions: string[] | null;
  dependsOnColumn: string | null;
  cascade: BlueprintCascade | null;
  formula: string | null;
  prompt: string | null;
  validation: BlueprintValidation | null;
}

export interface BlueprintSheet {
  name: string;
  key: string;
  table: string | null;
  range: string;
  headerRow: number;
  dataStartRow: number;
  dataEndRow: number;
  kind: BlueprintSheetKind;
  editableByRole: string;
  fields: BlueprintField[];
}

export interface FichaBlueprint {
  version: string;
  sourceWorkbook: string;
  multiSelectSeparator: string;
  sheets: BlueprintSheet[];
}
