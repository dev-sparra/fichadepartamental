import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { AppConfigService } from './app-config.service';
import { AuthenticatedUser, ChangePasswordRequest, LoginRequest } from '../../shared/models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  login(request: LoginRequest) {
    return this.http.post<AuthenticatedUser>(`${this.appConfigService.apiBaseUrl}/auth/login`, request);
  }

  changePassword(request: ChangePasswordRequest) {
    return this.http.post<void>(`${this.appConfigService.apiBaseUrl}/auth/change-password`, request);
  }
}