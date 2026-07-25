import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { AppConfigService } from './app-config.service';
import { NotificationFeed } from '../../shared/models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfigService = inject(AppConfigService);

  getFeed(take = 15) {
    return this.http.get<NotificationFeed>(`${this.appConfigService.apiBaseUrl}/notifications`, {
      params: { take }
    });
  }

  markAsRead(notificationId: string) {
    return this.http.post<void>(
      `${this.appConfigService.apiBaseUrl}/notifications/${notificationId}/read`,
      {}
    );
  }

  markAllAsRead() {
    return this.http.post<void>(`${this.appConfigService.apiBaseUrl}/notifications/read-all`, {});
  }
}
