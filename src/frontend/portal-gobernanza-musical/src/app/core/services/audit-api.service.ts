import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { AppConfigService } from './app-config.service';
import { AuditLog, AuditLogQuery } from '../../shared/models/audit.models';

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  getLogs(query: AuditLogQuery) {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.entityName) {
      params = params.set('entityName', query.entityName);
    }
    if (query.entityId) {
      params = params.set('entityId', query.entityId);
    }

    return this.http.get<AuditLog[]>(`${this.appConfigService.apiBaseUrl}/audit`, { params });
  }
}
