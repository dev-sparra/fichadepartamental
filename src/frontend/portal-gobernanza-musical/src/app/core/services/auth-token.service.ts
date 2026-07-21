import { Injectable, signal } from '@angular/core';

const ACCESS_TOKEN_KEY = 'portal_gobernanza_musical_access_token';
const ACCESS_TOKEN_EXPIRES_KEY = 'portal_gobernanza_musical_access_token_expires';
const USER_SESSION_KEY = 'portal_gobernanza_musical_user_session';
const PERSIST_FLAG_KEY = 'portal_gobernanza_musical_persist_session';

export interface UserSession {
  email: string;
  displayName: string | null;
  roles: string[];
  mustChangePassword: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthTokenService {
  readonly userSession = signal<UserSession | null>(this.loadSession());
  readonly isAuthenticated = signal(this.hasToken());
  readonly mustChangePassword = signal(this.userSession()?.mustChangePassword ?? false);

  getAccessToken(): string | null {
    return this.getStorage().getItem(ACCESS_TOKEN_KEY);
  }

  hasToken(): boolean {
    const token = this.getAccessToken();
    const storage = this.getStorage();
    const expiresAt = storage.getItem(ACCESS_TOKEN_EXPIRES_KEY);
    return !!token && !!expiresAt && new Date(expiresAt) > new Date();
  }

  getUserSession(): UserSession | null {
    return this.userSession();
  }

  get userDisplayName(): string {
    return this.userSession()?.displayName ?? this.userSession()?.email ?? 'Usuario';
  }

  get userInitials(): string {
    const name = this.userDisplayName;
    if (!name || name === 'Usuario') {
      return 'U';
    }
    return name
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part.charAt(0).toUpperCase())
      .join('');
  }

  hasAnyRole(allowedRoles: readonly string[]): boolean {
    if (allowedRoles.length === 0) {
      return true;
    }
    const userRoles = this.userSession()?.roles ?? [];
    const normalizedUserRoles = userRoles.map((role) => this.normalizeRole(role));
    return allowedRoles.some((allowed) => normalizedUserRoles.includes(this.normalizeRole(allowed)));
  }

  setSession(token: string, expiresAtUtc?: string, session?: UserSession, persist = false): void {
    this.clear();
    const storage = persist ? localStorage : sessionStorage;
    storage.setItem(PERSIST_FLAG_KEY, persist ? '1' : '0');
    storage.setItem(ACCESS_TOKEN_KEY, token);
    if (expiresAtUtc) {
      storage.setItem(ACCESS_TOKEN_EXPIRES_KEY, expiresAtUtc);
    }
    if (session) {
      storage.setItem(USER_SESSION_KEY, JSON.stringify(session));
      this.userSession.set(session);
      this.mustChangePassword.set(session.mustChangePassword ?? false);
    }
    this.isAuthenticated.set(true);
  }

  clearMustChangePassword(): void {
    const current = this.userSession();
    if (current) {
      const updated = { ...current, mustChangePassword: false };
      this.getStorage().setItem(USER_SESSION_KEY, JSON.stringify(updated));
      this.userSession.set(updated);
    }
    this.mustChangePassword.set(false);
  }

  clear(): void {
    sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(ACCESS_TOKEN_EXPIRES_KEY);
    sessionStorage.removeItem(USER_SESSION_KEY);
    sessionStorage.removeItem(PERSIST_FLAG_KEY);
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(ACCESS_TOKEN_EXPIRES_KEY);
    localStorage.removeItem(USER_SESSION_KEY);
    localStorage.removeItem(PERSIST_FLAG_KEY);
    this.userSession.set(null);
    this.isAuthenticated.set(false);
    this.mustChangePassword.set(false);
  }

  private getStorage(): Storage {
    const persist = localStorage.getItem(PERSIST_FLAG_KEY) === '1'
      || sessionStorage.getItem(PERSIST_FLAG_KEY) === '1';

    if (persist && localStorage.getItem(ACCESS_TOKEN_KEY)) {
      return localStorage;
    }
    if (sessionStorage.getItem(ACCESS_TOKEN_KEY)) {
      return sessionStorage;
    }
    return localStorage;
  }

  private loadSession(): UserSession | null {
    try {
      const storage = this.getStorage();
      const raw = storage.getItem(USER_SESSION_KEY);
      return raw ? (JSON.parse(raw) as UserSession) : null;
    } catch {
      return null;
    }
  }

  private normalizeRole(role: string): string {
    return role
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '')
      .trim()
      .toUpperCase();
  }
}