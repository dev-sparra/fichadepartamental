import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AppConfigService } from './app-config.service';
import { IndicatorDetailRecord, IndicatorRecord, IndicatorWorksheet, UpdateIndicatorDetailRecordRequest, UpdateIndicatorRecordRequest } from '../../shared/models/indicator.models';

@Injectable({ providedIn: 'root' })
export class IndicatorsApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  getRecords() { return this.http.get<IndicatorRecord[]>(`${this.appConfigService.apiBaseUrl}/indicators/records`); }
  getRecord(id: string) { return this.http.get<IndicatorRecord>(`${this.appConfigService.apiBaseUrl}/indicators/records/${id}`); }
  updateRecord(id: string, request: UpdateIndicatorRecordRequest) { return this.http.put<IndicatorRecord>(`${this.appConfigService.apiBaseUrl}/indicators/records/${id}`, request); }

  provisionWorksheet(departmentId: number, year: number) {
    return this.http.post<IndicatorWorksheet>(`${this.appConfigService.apiBaseUrl}/indicators/worksheet`, { departmentId, year });
  }

  getDetails() { return this.http.get<IndicatorDetailRecord[]>(`${this.appConfigService.apiBaseUrl}/indicators/details`); }
  getDetail(id: string) { return this.http.get<IndicatorDetailRecord>(`${this.appConfigService.apiBaseUrl}/indicators/details/${id}`); }
  updateDetail(id: string, request: UpdateIndicatorDetailRecordRequest) { return this.http.put<IndicatorDetailRecord>(`${this.appConfigService.apiBaseUrl}/indicators/details/${id}`, request); }
}
