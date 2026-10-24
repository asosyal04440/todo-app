import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API_URL, isApiRequest } from '../config/api-url.token';
import { SILENT_API_ERROR } from '../errors/api-error.context';
import { ApiErrorService } from '../errors/api-error.service';

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const apiUrl = inject(API_URL);
  if (!isApiRequest(request.url, apiUrl)) return next(request);
  const errors = inject(ApiErrorService);
  return next(request).pipe(
    catchError((error: unknown) => {
      const normalized = errors.normalize(error);
      if (
        !request.context.get(SILENT_API_ERROR) &&
        (normalized.status === 0 || normalized.status === 429 || normalized.status >= 500)
      ) {
        errors.report(normalized);
      }
      return throwError(() => normalized);
    }),
  );
};
