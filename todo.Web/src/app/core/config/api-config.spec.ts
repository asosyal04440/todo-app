import { parseApiConfig, apiConnectSource } from './api-config';
import { isApiRequest } from './api-url.token';

describe('Environment API configuration', () => {
  it.each(['/api', 'https://api.example.com/api', 'http://localhost:7070/api'])(
    'accepts %s and removes trailing slash',
    (apiUrl) => {
      expect(parseApiConfig({ apiUrl: `${apiUrl}/` })).toEqual({ apiUrl });
    },
  );
  it.each([
    null,
    [],
    {},
    { apiUrl: '' },
    { apiUrl: '//evil.example/api' },
    { apiUrl: 'javascript:alert(1)' },
    { apiUrl: 'https://user:password@api.example.com/api' },
    { apiUrl: 'https://api.example.com/api?key=1' },
    { apiUrl: 'https://api.example.com/api#part' },
    { apiUrl: "https://api.example.com/'unsafe" },
  ])('rejects invalid configuration %j before bootstrap', (value) => {
    expect(() => parseApiConfig(value)).toThrow();
  });
  it('allows only the configured origin in the deployment connect source', () => {
    expect(apiConnectSource(parseApiConfig({ apiUrl: 'https://api.example.com:8443/api' }))).toBe(
      "'self' https://api.example.com:8443",
    );
    expect(apiConnectSource(parseApiConfig({ apiUrl: '/api' }))).toBe("'self'");
  });
  it('matches API paths and origin without leaking headers to prefix lookalikes', () => {
    const api = 'https://api.example.com/api';
    expect(isApiRequest(`${api}/todos?x=1`, api)).toBe(true);
    expect(isApiRequest(`${api}-other/todos`, api)).toBe(false);
    expect(isApiRequest('https://api.example.com.evil.test/api/todos', api)).toBe(false);
    expect(isApiRequest('https://other.example.com/api/todos', api)).toBe(false);
  });
});
