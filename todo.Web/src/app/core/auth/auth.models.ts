export type UserRole = 'User' | 'Admin';

export interface CurrentUser {
  id: number;
  email: string;
  displayName: string;
  role: UserRole;
  hasPassword: boolean;
  hasGoogleLogin: boolean;
  createdAtUtc: string;
}

export interface AuthResult {
  accessToken: string;
  expiresAtUtc: string;
  user: CurrentUser;
}

export interface AuthConfig {
  googleClientId: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  displayName: string;
  password: string;
}
