import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { AppConfigService } from './app-config.service';

@Injectable({ providedIn: 'root' })
export class ExportsApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  exportFichaExcel(fichaId: string) {
    return this.http.get(`${this.appConfigService.apiBaseUrl}/exports/fichas/${fichaId}/excel`, {
      responseType: 'blob'
    });
  }
}
