import { API_URL } from '../config/api-url.token';
import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NzMessageService } from 'ng-zorro-antd/message';
import { of } from 'rxjs';
import { apiErrorInterceptor } from './api-error.interceptor';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../auth/auth.service';
import { ApiError } from '../errors/api-error';
import { ApiErrorService } from '../errors/api-error.service';
import { SILENT_API_ERROR } from '../errors/api-error.context';

describe('API error interceptor ordering', () => {
  const notify = vi.fn();
  const refresh = vi.fn();
  const url = 'http://localhost:7070/api/todo-lists';
  beforeEach(() => {
    notify.mockReset();
    refresh.mockReset();
    refresh.mockReturnValue(of({ accessToken: 'fresh' }));
    TestBed.configureTestingModule({
      providers: [
        { provide: API_URL, useValue: 'http://localhost:7070/api' },
        provideHttpClient(withInterceptors([apiErrorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
        { provide: NzMessageService, useValue: { error: notify } },
        {
          provide: AuthService,
          useValue: { getAccessToken: () => 'old', refreshSession: refresh, endSession: vi.fn() },
        },
      ],
    });
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());
  it('lets auth refresh retry a raw 401 before error normalization', () => {
    let result: unknown;
    TestBed.inject(HttpClient)
      .get(url)
      .subscribe((value) => (result = value));
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(url).flush(null, { status: 401, statusText: 'Unauthorized' });
    const retry = http.expectOne(url);
    expect(retry.request.headers.get('Authorization')).toBe('Bearer fresh');
    retry.flush({ items: [] });
    expect(result).toEqual({ items: [] });
    expect(refresh).toHaveBeenCalledOnce();
    expect(notify).not.toHaveBeenCalled();
  });
  it('reports a global server failure once even when a caller also reports it', () => {
    let caught: unknown;
    TestBed.inject(HttpClient)
      .get(url)
      .subscribe({ error: (error) => (caught = error) });
    TestBed.inject(HttpTestingController)
      .expectOne(url)
      .flush({}, { status: 503, statusText: 'Unavailable' });
    expect(caught).toBeInstanceOf(ApiError);
    TestBed.inject(ApiErrorService).report(caught);
    expect(notify).toHaveBeenCalledOnce();
  });
  it('keeps a silent request normalized without showing a toast', () => {
    let caught: unknown;
    TestBed.inject(HttpClient)
      .get(url, { context: new HttpContext().set(SILENT_API_ERROR, true) })
      .subscribe({ error: (error) => (caught = error) });
    TestBed.inject(HttpTestingController)
      .expectOne(url)
      .flush({}, { status: 503, statusText: 'Unavailable' });
    expect(caught).toBeInstanceOf(ApiError);
    expect(notify).not.toHaveBeenCalled();
  });
});
