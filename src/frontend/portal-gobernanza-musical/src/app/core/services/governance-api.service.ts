import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { AppConfigService } from './app-config.service';
import { FichaBlueprint } from '../../shared/models/ficha-blueprint.models';
import { GovernanceActor, GovernanceDiagnostic, GovernanceFichaDetail, GovernanceFichaSummary, GovernanceOpportunity, GovernancePnmcAxis, UpdateGovernanceFichaRequest } from '../../shared/models/governance.models';

@Injectable({ providedIn: 'root' })
export class GovernanceApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  getBlueprint() {
    return this.http.get<FichaBlueprint>(`${this.appConfigService.apiBaseUrl}/governance/blueprint`);
  }

  getFichas() {
    return this.http.get<GovernanceFichaSummary[]>(`${this.appConfigService.apiBaseUrl}/governance/fichas`);
  }

  getFicha(id: string) {
    return this.http.get<GovernanceFichaDetail>(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}`);
  }

  createFicha(request: UpdateGovernanceFichaRequest) {
    return this.http.post<GovernanceFichaDetail>(`${this.appConfigService.apiBaseUrl}/governance/fichas`, request);
  }

  updateFicha(id: string, request: UpdateGovernanceFichaRequest) {
    return this.http.put<GovernanceFichaDetail>(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}`, request);
  }

  deleteFicha(id: string) {
    return this.http.delete(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}`);
  }

  getDiagnostic(id: string) {
    return this.http.get<GovernanceDiagnostic>(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}/diagnostic`);
  }

  updateDiagnostic(id: string, request: GovernanceDiagnostic) {
    return this.http.put<GovernanceDiagnostic>(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}/diagnostic`, request);
  }

  getOpportunities(id: string) {
    return this.http.get<GovernanceOpportunity[]>(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}/opportunities`);
  }

  replaceOpportunities(id: string, request: GovernanceOpportunity[]) {
    return this.http.put<GovernanceOpportunity[]>(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}/opportunities`, request);
  }

  getPnmcAxes(id: string) {
    return this.http.get<GovernancePnmcAxis[]>(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}/pnmc-axes`);
  }

  replacePnmcAxes(id: string, request: GovernancePnmcAxis[]) {
    return this.http.put<GovernancePnmcAxis[]>(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}/pnmc-axes`, request);
  }

  getActors(id: string) {
    return this.http.get<GovernanceActor[]>(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}/actors`);
  }

  replaceActors(id: string, request: GovernanceActor[]) {
    return this.http.put<GovernanceActor[]>(`${this.appConfigService.apiBaseUrl}/governance/fichas/${id}/actors`, request);
  }
}
