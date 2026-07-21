import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { AppConfigService } from './app-config.service';
import { ImportBatchSummary, ImportValidationIssue, ImportWorkbookResult } from '../../shared/models/import.models';

@Injectable({ providedIn: 'root' })
export class ImportsApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  importExcel(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ImportWorkbookResult>(`${this.appConfigService.apiBaseUrl}/imports/excel`, formData);
  }

  getBatches() {
    return this.http.get<ImportBatchSummary[]>(`${this.appConfigService.apiBaseUrl}/imports/batches`);
  }

  getIssues(batchId: string) {
    return this.http.get<ImportValidationIssue[]>(`${this.appConfigService.apiBaseUrl}/imports/batches/${batchId}/issues`);
  }

  deleteBatch(batchId: string) {
    return this.http.delete(`${this.appConfigService.apiBaseUrl}/imports/batches/${batchId}`, { observe: 'response' });
  }

  downloadTemplate() {
    return this.http.get(`${this.appConfigService.apiBaseUrl}/imports/template`, { responseType: 'blob' });
  }
}
