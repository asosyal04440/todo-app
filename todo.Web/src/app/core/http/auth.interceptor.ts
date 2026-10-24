import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
import { API_URL, isApiRequest } from '../config/api-url.token';
import { AuthService } from '../auth/auth.service';

export const CLIENT_HEADER_NAME = 'X-Todo-Client';

const ENDPOINTS_WITHOUT_ACCESS_TOKEN = [
  '/auth/config',
  '/auth/login',
  '/auth/register',
  '/auth/google',
  '/auth/refresh',
  '/auth/logout',
];

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const apiUrl = inject(API_URL);

  if (!isApiRequest(request.url, apiUrl)) {
    return next(request);
  }

  const isAuthRequest = request.url.startsWith(`${apiUrl}/auth/`);
  const needsAccessToken = !ENDPOINTS_WITHOUT_ACCESS_TOKEN.some((endpoint) =>
    request.url.endsWith(endpoint),
  );

  const preparedRequest = isAuthRequest
    ? request.clone({ withCredentials: true, setHeaders: { [CLIENT_HEADER_NAME]: '1' } })
    : request;

  if (!needsAccessToken) {
    return next(preparedRequest);
  }

  const accessToken = auth.getAccessToken();
  const firstAttempt =
    accessToken !== null
      ? sendWithToken(preparedRequest, next, accessToken)
      : auth
          .refreshSession()
          .pipe(
            switchMap((session) =>
              sendWithToken(preparedRequest, next, session?.accessToken ?? null),
            ),
          );

  return firstAttempt.pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return auth.refreshSession().pipe(
        switchMap((session) => {
          if (session === null) {
            auth.endSession();
            return throwError(() => error);
          }

          return sendWithToken(preparedRequest, next, session.accessToken);
        }),
      );
    }),
  );
};

function sendWithToken(
  request: HttpRequest<unknown>,
  next: HttpHandlerFn,
  accessToken: string | null,
): Observable<HttpEvent<unknown>> {
  if (accessToken === null) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }));
}
