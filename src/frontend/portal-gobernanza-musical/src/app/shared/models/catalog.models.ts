export interface CatalogOption {
  id: number;
  name: string;
  displayOrder: number;
}

export interface DepartmentCatalogOption extends CatalogOption {}

export interface MunicipalityCatalogOption extends CatalogOption {
  departmentId: number;
}

export interface IndicatorDefinitionCatalog {
  id: number;
  actionName: string;
  indicatorName: string;
  targetValue: number;
  displayOrder: number;
}

export interface PnmcComponentCatalogOption extends CatalogOption {}

export interface EcosystemRoleCatalogOption extends CatalogOption {}

export interface CatalogDefinition {
  key: string;
  displayName: string;
  hasParent: boolean;
  parentKey: string | null;
}

export interface CatalogItem {
  id: number;
  name: string;
  displayOrder: number;
  isActive: boolean;
  parentId: number | null;
}

export interface UpsertCatalogItemRequest {
  name: string;
  displayOrder: number;
  isActive: boolean;
  parentId: number | null;
}
