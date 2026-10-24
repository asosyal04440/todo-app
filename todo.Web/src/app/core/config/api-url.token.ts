import { InjectionToken } from '@angular/core';

export const API_URL = new InjectionToken<string>('Configured Todo API base URL');

export function isApiRequest(requestUrl: string, apiUrl: string): boolean {
  const base =
    typeof document === 'undefined' ? 'https://url-comparison.invalid/' : document.baseURI;
  const request = new URL(requestUrl, base);
  const api = new URL(apiUrl, base);
  return (
    request.origin === api.origin &&
    (request.pathname === api.pathname ||
      request.pathname.startsWith(`${api.pathname.replace(/\/+$/, '')}/`))
  );
}
