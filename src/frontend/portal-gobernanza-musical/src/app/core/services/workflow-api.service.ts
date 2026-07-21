import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { AppConfigService } from './app-config.service';
import { ApprovalRecord, FichaApprovalStatus } from '../../shared/models/workflow.models';

@Injectable({ providedIn: 'root' })
export class WorkflowApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  getStatus(fichaId: string) {
    return this.http.get<FichaApprovalStatus>(
      `${this.appConfigService.apiBaseUrl}/governance/fichas/${fichaId}/workflow`
    );
  }

  approve(fichaId: string, comment: string | null) {
    return this.http.post<void>(
      `${this.appConfigService.apiBaseUrl}/governance/fichas/${fichaId}/workflow/approve`,
      comment
    );
  }

  reject(fichaId: string, comment: string | null) {
    return this.http.post<void>(
      `${this.appConfigService.apiBaseUrl}/governance/fichas/${fichaId}/workflow/reject`,
      comment
    );
  }

  getHistory(fichaId: string) {
    return this.http.get<ApprovalRecord[]>(
      `${this.appConfigService.apiBaseUrl}/governance/fichas/${fichaId}/workflow/history`
    );
  }
}
