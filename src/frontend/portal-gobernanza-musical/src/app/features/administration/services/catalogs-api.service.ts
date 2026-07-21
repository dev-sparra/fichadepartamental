import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { AppConfigService } from '../../../core/services/app-config.service';
import {
  CatalogDefinition,
  CatalogItem,
  CatalogOption,
  DepartmentCatalogOption,
  EcosystemRoleCatalogOption,
  IndicatorDefinitionCatalog,
  MunicipalityCatalogOption,
  PnmcComponentCatalogOption,
  UpsertCatalogItemRequest
} from '../../../shared/models/catalog.models';

@Injectable({
  providedIn: 'root'
})
export class CatalogsApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  getDepartments() {
    return this.http.get<DepartmentCatalogOption[]>(`${this.appConfigService.apiBaseUrl}/catalogs/departments`);
  }

  getMunicipalities(departmentId: number) {
    return this.http.get<MunicipalityCatalogOption[]>(`${this.appConfigService.apiBaseUrl}/catalogs/departments/${departmentId}/municipalities`);
  }

  getLookup(name: string) {
    return this.http.get<CatalogOption[]>(`${this.appConfigService.apiBaseUrl}/catalogs/lookups/${name}`);
  }

  getPnmcComponents(pnmcAxisId: number) {
    return this.http.get<PnmcComponentCatalogOption[]>(`${this.appConfigService.apiBaseUrl}/catalogs/pnmc-axes/${pnmcAxisId}/components`);
  }

  getEcosystemRoles(agentTypeId: number) {
    return this.http.get<EcosystemRoleCatalogOption[]>(`${this.appConfigService.apiBaseUrl}/catalogs/agent-types/${agentTypeId}/ecosystem-roles`);
  }

  getIndicatorDefinitions() {
    return this.http.get<IndicatorDefinitionCatalog[]>(`${this.appConfigService.apiBaseUrl}/catalogs/indicator-definitions`);
  }

  getCatalogDefinitions() {
    return this.http.get<CatalogDefinition[]>(`${this.appConfigService.apiBaseUrl}/catalogs/admin/definitions`);
  }

  getCatalogItems(catalogKey: string, parentId?: number) {
    const params: Record<string, number> = {};
    if (parentId != null) {
      params['parentId'] = parentId;
    }
    return this.http.get<CatalogItem[]>(`${this.appConfigService.apiBaseUrl}/catalogs/admin/${catalogKey}/items`, { params });
  }

  createCatalogItem(catalogKey: string, request: UpsertCatalogItemRequest) {
    return this.http.post<CatalogItem>(`${this.appConfigService.apiBaseUrl}/catalogs/admin/${catalogKey}/items`, request);
  }

  updateCatalogItem(catalogKey: string, id: number, request: UpsertCatalogItemRequest) {
    return this.http.put<CatalogItem>(`${this.appConfigService.apiBaseUrl}/catalogs/admin/${catalogKey}/items/${id}`, request);
  }

  deleteCatalogItem(catalogKey: string, id: number) {
    return this.http.delete<void>(`${this.appConfigService.apiBaseUrl}/catalogs/admin/${catalogKey}/items/${id}`);
  }
}
