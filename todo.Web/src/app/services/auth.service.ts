import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  catchError,
  finalize,
  firstValueFrom,
  map,
  of,
  shareReplay,
  tap,
} from 'rxjs';
import { resolveApiUrl } from '../core/api-url';
import {
  AuthConfig,
  AuthResult,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
} from '../models/auth.models';

interface Session {
  accessToken: string;
  expiresAtUtc: string;
  user: CurrentUser;
}

const EXPIRY_SAFETY_MARGIN_MS = 30_000;

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = resolveApiUrl();
  private readonly session = signal<Session | null>(null);
  private refreshInFlight: Observable<Session | null> | null = null;

  readonly currentUser = computed(() => this.session()?.user ?? null);
  readonly isAdmin = computed(() => this.currentUser()?.role === 'Admin');

  getAuthConfig(): Observable<AuthConfig> {
    return this.http.get<AuthConfig>(`${this.apiUrl}/auth/config`);
  }

  login(request: LoginRequest): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${this.apiUrl}/auth/login`, request)
      .pipe(tap((result) => this.startSession(result)));
  }

  register(request: RegisterRequest): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${this.apiUrl}/auth/register`, request)
      .pipe(tap((result) => this.startSession(result)));
  }

  loginWithGoogle(idToken: string): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${this.apiUrl}/auth/google`, { idToken })
      .pipe(tap((result) => this.startSession(result)));
  }

  async restoreSession(): Promise<void> {
    const restored = await firstValueFrom(this.refreshSession());

    if (restored === null) {
      await firstValueFrom(this.refreshSession());
    }
  }

  refreshSession(): Observable<Session | null> {
    if (this.refreshInFlight === null) {
      this.refreshInFlight = this.http.post<AuthResult>(`${this.apiUrl}/auth/refresh`, null).pipe(
        map((result) => this.startSession(result)),
        catchError(() => {
          this.session.set(null);
          return of(null);
        }),
        finalize(() => {
          this.refreshInFlight = null;
        }),
        shareReplay(1),
      );
    }

    return this.refreshInFlight;
  }

  isAuthenticated(): boolean {
    return this.session() !== null;
  }

  getAccessToken(): string | null {
    const session = this.session();

    if (session === null) {
      return null;
    }

    if (Date.parse(session.expiresAtUtc) - EXPIRY_SAFETY_MARGIN_MS <= Date.now()) {
      return null;
    }

    return session.accessToken;
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post<void>(`${this.apiUrl}/auth/logout`, null));
    } catch {
    }

    this.endSession();
  }

  endSession(): void {
    this.session.set(null);
    void this.router.navigate(['/login']);
  }

  private startSession(result: AuthResult): Session {
    const session: Session = {
      accessToken: result.accessToken,
      expiresAtUtc: result.expiresAtUtc,
      user: result.user,
    };

    this.session.set(session);

    return session;
  }
}
