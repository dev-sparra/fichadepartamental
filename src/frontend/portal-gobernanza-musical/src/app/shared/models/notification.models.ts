export type NotificationTone = 'success' | 'warning' | 'error' | 'info';

export interface AppNotification {
  id: string;
  category: string;
  eventCode: string;
  title: string;
  message: string;
  tone: NotificationTone;
  /** Ruta del portal a la que lleva el aviso (p. ej. /governance). */
  actionRoute: string | null;
  relatedEntityId: string | null;
  /** Persona que originó el cambio de estado (líder que aprueba o devuelve). */
  triggeredByName: string | null;
  isRead: boolean;
  createdAtUtc: string;
}

export interface NotificationFeed {
  unreadCount: number;
  items: AppNotification[];
}
