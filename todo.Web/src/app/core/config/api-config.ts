export interface ApiConfig {
  apiUrl: string;
}

export function parseApiConfig(value: unknown): ApiConfig {
  if (!value || typeof value !== 'object' || Array.isArray(value))
    throw new Error('API config must be an object.');
  const raw = (value as Record<string, unknown>)['apiUrl'];
  if (typeof raw !== 'string' || !raw.trim()) throw new Error('API config apiUrl is required.');
  const apiUrl = raw.trim().replace(/\/+$/, '');
  if (/\s|["'<>\\]/.test(apiUrl)) throw new Error('API config apiUrl contains invalid characters.');
  const relative = apiUrl.startsWith('/') && !apiUrl.startsWith('//');
  const url = new URL(apiUrl, relative ? 'https://config-validation.invalid' : undefined);
  if (
    !['http:', 'https:'].includes(url.protocol) ||
    url.username ||
    url.password ||
    url.search ||
    url.hash
  )
    throw new Error(
      'API config apiUrl must be an HTTP(S) URL or an absolute path without credentials, query or fragment.',
    );
  if (!relative && !/^https?:\/\//i.test(apiUrl))
    throw new Error('API config apiUrl must be an absolute HTTP(S) URL.');
  return { apiUrl: relative ? url.pathname.replace(/\/+$/, '') : url.href.replace(/\/+$/, '') };
}

export function apiConnectSource(config: ApiConfig): string {
  return config.apiUrl.startsWith('/') ? "'self'" : `'self' ${new URL(config.apiUrl).origin}`;
}
