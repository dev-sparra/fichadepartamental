import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AppConfigService } from '../../../core/services/app-config.service';

export interface UserDto {
  id: string;
  email: string;
  displayName: string | null;
  isActive: boolean;
  roles: string[];
}

export interface CreateUserRequest {
  email: string;
  password: string;
  displayName: string | null;
  roleNames: string[];
}

export interface UpdateUserRequest {
  email: string;
  displayName: string | null;
  isActive: boolean;
  roleNames: string[];
}

export interface ResetPasswordResult {
  userId: string;
  email: string;
  temporaryPassword: string;
}

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  private get baseUrl(): string {
    return `${this.appConfigService.apiBaseUrl}/administration/users`;
  }

  getAll() {
    return this.http.get<UserDto[]>(this.baseUrl);
  }

  getById(id: string) {
    return this.http.get<UserDto>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateUserRequest) {
    return this.http.post<UserDto>(this.baseUrl, request);
  }

  update(id: string, request: UpdateUserRequest) {
    return this.http.put<UserDto>(`${this.baseUrl}/${id}`, request);
  }

  resetPassword(id: string) {
    return this.http.post<ResetPasswordResult>(`${this.baseUrl}/${id}/reset-password`, {});
  }
}
