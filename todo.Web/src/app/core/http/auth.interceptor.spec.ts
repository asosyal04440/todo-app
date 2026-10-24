import { API_URL } from '../config/api-url.token';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  const apiUrl = 'http://localhost:7070/api';
  let http: HttpClient;
  let httpTestingController: HttpTestingController;
  let accessToken: string | null;
  let refreshedToken: string | null;
  const endSession = vi.fn();
  const refreshSession = vi.fn((): Observable<{ accessToken: string } | null> =>
    of(refreshedToken === null ? null : { accessToken: refreshedToken }),
  );

  beforeEach(() => {
    accessToken = 'access-token';
    refreshedToken = 'refreshed-token';
    endSession.mockClear();
    refreshSession.mockClear();

    TestBed.configureTestingModule({
      providers: [
        { provide: API_URL, useValue: 'http://localhost:7070/api' },
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            getAccessToken: () => accessToken,
            refreshSession,
            endSession,
          },
        },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('adds the bearer token to API requests without sending cookies', () => {
    http.get(`${apiUrl}/todo-lists`).subscribe();

    const request = httpTestingController.expectOne(`${apiUrl}/todo-lists`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer access-token');
    expect(request.request.withCredentials).toBe(false);
    request.flush([]);
  });

  it('never sends the token to another website', () => {
    http.get('https://example.com/data').subscribe();

    const request = httpTestingController.expectOne('https://example.com/data');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('sends cookies and the anti-CSRF header only to the auth endpoints', () => {
    http.post(`${apiUrl}/auth/refresh`, null).subscribe();

    const request = httpTestingController.expectOne(`${apiUrl}/auth/refresh`);
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('X-Todo-Client')).toBe('1');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('refreshes an expired access token before sending the request', () => {
    accessToken = null;

    http.get(`${apiUrl}/todo-lists`).subscribe();

    const request = httpTestingController.expectOne(`${apiUrl}/todo-lists`);
    expect(refreshSession).toHaveBeenCalledOnce();
    expect(request.request.headers.get('Authorization')).toBe('Bearer refreshed-token');
    request.flush([]);
  });

  it('refreshes once and retries when the API rejects the token', () => {
    let received: unknown;
    http.get(`${apiUrl}/todo-lists`).subscribe((value) => (received = value));

    httpTestingController
      .expectOne(`${apiUrl}/todo-lists`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    const retried = httpTestingController.expectOne(`${apiUrl}/todo-lists`);
    expect(retried.request.headers.get('Authorization')).toBe('Bearer refreshed-token');
    retried.flush(['liste']);

    expect(received).toEqual(['liste']);
    expect(endSession).not.toHaveBeenCalled();
  });

  it('ends the session when the token cannot be refreshed', () => {
    refreshedToken = null;
    http.get(`${apiUrl}/todo-lists`).subscribe({ error: () => undefined });

    httpTestingController
      .expectOne(`${apiUrl}/todo-lists`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(endSession).toHaveBeenCalledOnce();
  });

  it('loads the public sign-in settings without trying to refresh a session', () => {
    accessToken = null;

    http.get(`${apiUrl}/auth/config`).subscribe();

    const request = httpTestingController.expectOne(`${apiUrl}/auth/config`);
    expect(refreshSession).not.toHaveBeenCalled();
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({ googleClientId: null });
  });

  it('does not try to refresh when a sign-in attempt fails', () => {
    http
      .post(`${apiUrl}/auth/login`, { email: 'ali@example.com', password: 'x' })
      .subscribe({ error: () => undefined });

    httpTestingController
      .expectOne(`${apiUrl}/auth/login`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(refreshSession).not.toHaveBeenCalled();
    expect(endSession).not.toHaveBeenCalled();
  });
});
