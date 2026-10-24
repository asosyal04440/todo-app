import { API_URL } from '../config/api-url.token';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { authInterceptor } from '../http/auth.interceptor';
import { AuthResult } from './auth.models';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpTestingController: HttpTestingController;
  const apiUrl = 'http://localhost:7070/api';

  function createAuthResult(
    expiresAtUtc = new Date(Date.now() + 15 * 60 * 1000).toISOString(),
    role: 'User' | 'Admin' = 'User',
  ): AuthResult {
    return {
      accessToken: 'access-token',
      expiresAtUtc,
      user: {
        id: 1,
        email: 'ali@example.com',
        displayName: 'Ali',
        role,
        hasPassword: true,
        hasGoogleLogin: false,
        createdAtUtc: '2026-10-06T10:00:00Z',
      },
    };
  }

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        { provide: API_URL, useValue: 'http://localhost:7070/api' },
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    service = TestBed.inject(AuthService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('keeps the access token in memory and never writes it to browser storage', () => {
    service.login({ email: 'ali@example.com', password: 'Sifre1234' }).subscribe();

    const request = httpTestingController.expectOne(`${apiUrl}/auth/login`);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('X-Todo-Client')).toBe('1');
    request.flush(createAuthResult());

    expect(service.isAuthenticated()).toBe(true);
    expect(service.getAccessToken()).toBe('access-token');
    expect(service.currentUser()?.displayName).toBe('Ali');
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  it('sends the Google credential to the API', () => {
    service.loginWithGoogle('google-id-token').subscribe();

    const request = httpTestingController.expectOne(`${apiUrl}/auth/google`);
    expect(request.request.body).toEqual({ idToken: 'google-id-token' });
    request.flush(createAuthResult(undefined, 'Admin'));

    expect(service.isAdmin()).toBe(true);
  });

  it('restores the session from the refresh cookie when the app starts', async () => {
    const restoring = service.restoreSession();

    const request = httpTestingController.expectOne(`${apiUrl}/auth/refresh`);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    request.flush(createAuthResult());
    await restoring;

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.email).toBe('ali@example.com');
  });

  it('stays signed out when there is no valid refresh cookie', async () => {
    const restoring = service.restoreSession();

    httpTestingController
      .expectOne(`${apiUrl}/auth/refresh`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    await Promise.resolve();
    httpTestingController
      .expectOne(`${apiUrl}/auth/refresh`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    await restoring;

    expect(service.isAuthenticated()).toBe(false);
    expect(service.getAccessToken()).toBeNull();
  });

  it('shares a single refresh request between simultaneous callers', () => {
    service.refreshSession().subscribe();
    service.refreshSession().subscribe();

    httpTestingController.expectOne(`${apiUrl}/auth/refresh`).flush(createAuthResult());
  });

  it('treats an access token that is about to expire as unusable', () => {
    const almostExpired = new Date(Date.now() + 10_000).toISOString();

    service.login({ email: 'ali@example.com', password: 'Sifre1234' }).subscribe();
    httpTestingController.expectOne(`${apiUrl}/auth/login`).flush(createAuthResult(almostExpired));

    expect(service.isAuthenticated()).toBe(true);
    expect(service.getAccessToken()).toBeNull();
  });

  it('revokes the session on the server and goes to the login page on logout', async () => {
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    service.login({ email: 'ali@example.com', password: 'Sifre1234' }).subscribe();
    httpTestingController.expectOne(`${apiUrl}/auth/login`).flush(createAuthResult());

    const loggingOut = service.logout();

    const request = httpTestingController.expectOne(`${apiUrl}/auth/logout`);
    expect(request.request.withCredentials).toBe(true);
    request.flush(null, { status: 204, statusText: 'No Content' });
    await loggingOut;

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
});
