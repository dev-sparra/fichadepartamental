import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { AppConfigService } from './app-config.service';
import { AuditFilterOptions, AuditLogPage, AuditLogQuery } from '../../shared/models/audit.models';

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  getLogs(query: AuditLogQuery) {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    // Solo viajan los filtros con valor: así la URL refleja lo que el usuario eligió.
    const optional: [string, string | null | undefined][] = [
      ['module', query.module],
      ['userEmail', query.userEmail],
      ['operation', query.operation],
      ['entityName', query.entityName],
      ['entityId', query.entityId],
      ['result', query.result],
      ['search', query.search],
      ['from', query.from],
      ['to', query.to]
    ];

    for (const [key, value] of optional) {
      if (value) {
        params = params.set(key, value);
      }
    }

    return this.http.get<AuditLogPage>(`${this.appConfigService.apiBaseUrl}/audit`, { params });
  }

  getFilterOptions() {
    return this.http.get<AuditFilterOptions>(`${this.appConfigService.apiBaseUrl}/audit/filters`);
  }
}
