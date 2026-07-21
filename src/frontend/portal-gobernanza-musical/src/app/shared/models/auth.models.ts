export interface LoginRequest {
  email: string;
  password: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface AuthenticatedUser {
  accessToken: string;
  expiresAtUtc: string;
  email: string;
  displayName: string | null;
  roles: string[];
  mustChangePassword: boolean;
}